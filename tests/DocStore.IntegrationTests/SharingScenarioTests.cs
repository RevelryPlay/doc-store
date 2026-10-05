using System.Net;
using DocStore.Core.Auditing;
using Microsoft.EntityFrameworkCore;

namespace DocStore.IntegrationTests;

/// <summary>
/// The seeded scenario, read-only: A (Avery -> Blake), B (Blake, link only), C (Casey -> Avery and Blake).
/// </summary>
public class SharingScenarioTests(DocStoreFactory factory)
{
    private const string DocA = "Onboarding checklist.pdf";
    private const string DocB = "Benefits overview.pdf";
    private const string DocC = "Team handbook.pdf";

    [Theory]
    [InlineData("avery", new[] { DocA }, new[] { DocC })]
    [InlineData("blake", new[] { DocB }, new[] { DocA, DocC })]
    [InlineData("casey", new[] { DocC }, new string[0])]
    public async Task Library_ShowsOwnedAndExplicitlySharedOnly(string user, string[] owned, string[] shared)
    {
        using var client = await factory.SignInAsync(user);

        var library = await client.GetAsync<Library>("/api/documents");

        Assert.Equivalent(owned, SeededNames(library.Owned), strict: true);
        Assert.Equivalent(shared, SeededNames(library.SharedWithMe), strict: true);
    }

    [Fact]
    public async Task DocA_IsInvisibleToCasey()
    {
        using var avery = await factory.SignInAsync("avery");
        using var casey = await factory.SignInAsync("casey");
        var docA = await FindOwnedAsync(avery, DocA);

        var details = await casey.SendAsync(HttpMethod.Get, $"/api/documents/{docA.Id}");
        var content = await casey.SendAsync(HttpMethod.Get, $"/api/documents/{docA.Id}/content");

        Assert.Equal(HttpStatusCode.NotFound, details.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, content.StatusCode);
    }

    [Theory]
    [InlineData("avery")]
    [InlineData("casey")]
    public async Task DocB_OpensByLinkForAnySignedInUserAndIsAudited(string user)
    {
        using var blake = await factory.SignInAsync("blake");
        using var reader = await factory.SignInAsync(user);
        var docB = await FindOwnedAsync(blake, DocB);
        var token = (await blake.GetAsync<Details>($"/api/documents/{docB.Id}")).ShareLinkToken!;

        var details = await reader.GetAsync<Details>($"/api/links/{token}");
        var content = await reader.SendAsync(HttpMethod.Get, $"/api/documents/{docB.Id}/content?link={token}");

        Assert.Equal("LinkHolder", details.Access);
        Assert.Null(details.ShareLinkToken);
        Assert.Equal(HttpStatusCode.OK, content.StatusCode);
        Assert.Equal("application/pdf", content.Content.Headers.ContentType?.MediaType);

        var me = await reader.GetAsync<Me>("/api/auth/me");
        var audited = await factory.QueryDbAsync(db => db.AuditEntries.AnyAsync(e =>
            e.DocumentId == docB.Id && e.UserId == me.Id && e.Action == AuditAction.Downloaded));
        Assert.True(audited);
    }

    [Fact]
    public async Task DocB_WithoutLink_IsNotFoundForOthers()
    {
        using var blake = await factory.SignInAsync("blake");
        using var avery = await factory.SignInAsync("avery");
        var docB = await FindOwnedAsync(blake, DocB);

        var response = await avery.SendAsync(HttpMethod.Get, $"/api/documents/{docB.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SharedUser_CannotDeleteOrReshare()
    {
        using var casey = await factory.SignInAsync("casey");
        using var avery = await factory.SignInAsync("avery");
        var docC = await FindOwnedAsync(casey, DocC);
        var users = await avery.GetAsync<List<UserSummary>>("/api/users");
        var blakeId = users.Single(u => u.DisplayName == "Blake Rivera").Id;

        var delete = await avery.SendAsync(HttpMethod.Delete, $"/api/documents/{docC.Id}");
        var share = await avery.SendAsync(HttpMethod.Put, $"/api/documents/{docC.Id}/shares/{blakeId}");

        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, share.StatusCode);
    }

    [Fact]
    public async Task UnauthenticatedRequests_AreRejected()
    {
        using var anonymous = new ApiClient(factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") }));

        var response = await anonymous.SendAsync(HttpMethod.Get, "/api/documents");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UnsafeRequestWithoutXsrfHeader_IsRejected()
    {
        using var avery = await factory.SignInAsync("avery");
        avery.RemoveXsrfHeader();
        var docA = await FindOwnedAsync(avery, DocA);

        var response = await avery.SendAsync(HttpMethod.Put, $"/api/documents/{docA.Id}/link");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Other tests upload their own documents; the scenario assertions only concern the seeded ones.
    private static IEnumerable<string> SeededNames(IEnumerable<ListItem> items) =>
        items.Select(i => i.FileName).Where(n => n is DocA or DocB or DocC);

    private static async Task<ListItem> FindOwnedAsync(ApiClient owner, string fileName) =>
        (await owner.GetAsync<Library>("/api/documents")).Owned.Single(d => d.FileName == fileName);

    private sealed record Me(Guid Id, string DisplayName);
}

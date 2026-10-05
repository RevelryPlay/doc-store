using System.Net;
using DocStore.Api.Seeding;

namespace DocStore.IntegrationTests;

public class DocumentLifecycleTests(DocStoreFactory factory)
{
    [Fact]
    public async Task UploadedPdf_IsProcessedInTheBackground()
    {
        using var avery = await factory.SignInAsync("avery");

        var upload = await avery.UploadAsync("review.pdf", SamplePdf.Create("Annual review", "Avery Chen", "Exceeds expectations"));
        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);
        var id = (await ApiClient.ReadAsync<Created>(upload)).Id;

        var details = await WaitForProcessingAsync(avery, id);

        Assert.Equal("Ready", details.Status);
        Assert.Equal(1, details.PageCount);
        Assert.Equal("Annual review", details.Title);
    }

    [Fact]
    public async Task RenamedNonPdf_IsRejected()
    {
        using var avery = await factory.SignInAsync("avery");

        var upload = await avery.UploadAsync("totally-a.pdf", "MZ\u0090\u0000 executable"u8.ToArray());

        Assert.Equal(HttpStatusCode.BadRequest, upload.StatusCode);
    }

    [Fact]
    public async Task SharingThenDeleting_RemovesDocumentForRecipients()
    {
        using var avery = await factory.SignInAsync("avery");
        using var casey = await factory.SignInAsync("casey");
        var upload = await avery.UploadAsync("temp.pdf", SamplePdf.Create("Temp", "Avery Chen"));
        var id = (await ApiClient.ReadAsync<Created>(upload)).Id;
        var caseyId = (await avery.GetAsync<List<UserSummary>>("/api/users")).Single(u => u.DisplayName == "Casey Morgan").Id;

        var share = await avery.SendAsync(HttpMethod.Put, $"/api/documents/{id}/shares/{caseyId}");
        Assert.Equal(HttpStatusCode.NoContent, share.StatusCode);
        Assert.Contains((await casey.GetAsync<Library>("/api/documents")).SharedWithMe, d => d.Id == id);

        var delete = await avery.SendAsync(HttpMethod.Delete, $"/api/documents/{id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.DoesNotContain((await casey.GetAsync<Library>("/api/documents")).SharedWithMe, d => d.Id == id);
    }

    private static async Task<Details> WaitForProcessingAsync(ApiClient client, Guid id)
    {
        var timeout = DateTime.UtcNow.AddSeconds(15);
        while (true)
        {
            var details = await client.GetAsync<Details>($"/api/documents/{id}");
            if (details.Status != "Pending" || DateTime.UtcNow > timeout)
            {
                return details;
            }

            await Task.Delay(100, TestContext.Current.CancellationToken);
        }
    }

    private sealed record Created(Guid Id);
}

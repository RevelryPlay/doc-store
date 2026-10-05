using DocStore.Api.Infrastructure.Persistence;
using DocStore.Core.Documents;
using DocStore.Core.Storage;
using DocStore.Core.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DocStore.Api.Seeding;

public sealed class DemoOptions
{
    public bool Seed { get; init; }
    public string Password { get; init; } = "";
}

/// <summary>
/// Seeds the three-user sharing scenario:
/// A is shared by Avery with Blake, B is Blake's link-only document, C is shared by Casey with Avery and Blake.
/// Avery also has an unshared multi-page handbook.
/// </summary>
public sealed class DemoSeeder(
    DocStoreDbContext db,
    IDocumentStorage storage,
    IPasswordHasher<User> hasher,
    TimeProvider time)
{
    public async Task SeedAsync(string password, CancellationToken cancellationToken)
    {
        if (await db.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        var avery = NewUser("avery", "Avery Chen", password);
        var blake = NewUser("blake", "Blake Rivera", password);
        var casey = NewUser("casey", "Casey Morgan", password);
        db.Users.AddRange(avery, blake, casey);

        var docA = await NewDocumentAsync(avery, "Onboarding checklist.pdf", "Onboarding checklist",
            ["Complete I-9 verification", "Enroll in benefits within 30 days", "Schedule equipment pickup"], cancellationToken);
        docA.ShareWith(blake.Id);

        var docB = await NewDocumentAsync(blake, "Benefits overview.pdf", "Benefits overview",
            ["Medical and dental are fully covered", "401(k) contribution details", "Open enrollment runs each November"], cancellationToken);
        docB.EnableLinkSharing();

        var docC = await NewDocumentAsync(casey, "Team handbook.pdf", "Team handbook",
            ["Core collaboration days are Tuesday and Thursday", "Code review expectations", "On-call rotation"], cancellationToken);
        docC.ShareWith(avery.Id);
        docC.ShareWith(blake.Id);

        var handbook = await NewDocumentAsync(avery, "Employee handbook.pdf", "Employee handbook",
            HandbookContent.Paragraphs, cancellationToken);

        db.Documents.AddRange(docA, docB, docC, handbook);
        await db.SaveChangesAsync(cancellationToken);
    }

    private User NewUser(string userName, string displayName, string password)
    {
        var user = new User(userName, displayName);
        user.SetPasswordHash(hasher.HashPassword(user, password));
        return user;
    }

    private async Task<Document> NewDocumentAsync(
        User owner, string fileName, string title, string[] lines, CancellationToken cancellationToken)
    {
        var bytes = SamplePdf.Create(title, owner.DisplayName, lines);
        using var content = new MemoryStream(bytes);
        var storageKey = await storage.SaveAsync(content, cancellationToken);
        return new Document(owner.Id, fileName, "application/pdf", bytes.Length, storageKey, time.GetUtcNow());
    }
}

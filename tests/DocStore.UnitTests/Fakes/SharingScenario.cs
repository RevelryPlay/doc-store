using DocStore.Core.Documents;
using DocStore.Core.Users;

namespace DocStore.UnitTests.Fakes;

/// <summary>
/// A: user 1 shares with user 2. B: user 2 shares by link only. C: user 3 shares with users 1 and 2.
/// </summary>
internal sealed class SharingScenario
{
    public SharingScenario()
    {
        DocA = NewDocument(User1, "a.pdf");
        DocA.ShareWith(User2.Id);

        DocB = NewDocument(User2, "b.pdf");
        LinkToken = DocB.EnableLinkSharing();

        DocC = NewDocument(User3, "c.pdf");
        DocC.ShareWith(User1.Id);
        DocC.ShareWith(User2.Id);

        Documents.Items.AddRange([DocA, DocB, DocC]);
    }

    public User User1 { get; } = new("user1", "User One");
    public User User2 { get; } = new("user2", "User Two");
    public User User3 { get; } = new("user3", "User Three");
    public Document DocA { get; }
    public Document DocB { get; }
    public Document DocC { get; }
    public string LinkToken { get; }

    public InMemoryDocuments Documents { get; } = new();
    public InMemoryStorage Storage { get; } = new();
    public RecordingAuditLog Audit { get; } = new();
    public InMemoryUsers Users => new(User1, User2, User3);

    public Document this[string name] => name switch
    {
        "A" => DocA,
        "B" => DocB,
        "C" => DocC,
        _ => throw new ArgumentOutOfRangeException(nameof(name)),
    };

    public User User(int number) => number switch
    {
        1 => User1,
        2 => User2,
        3 => User3,
        _ => throw new ArgumentOutOfRangeException(nameof(number)),
    };

    private Document NewDocument(User owner, string fileName) =>
        new(owner.Id, fileName, "application/pdf", 3, Storage.Put([1, 2, 3]), DateTimeOffset.UnixEpoch);
}

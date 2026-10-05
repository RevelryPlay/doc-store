namespace DocStore.Core.Users;

public sealed class User
{
    public User(string userName, string displayName)
    {
        Id = Guid.CreateVersion7();
        UserName = userName;
        DisplayName = displayName;
        PasswordHash = "";
    }

    public Guid Id { get; private set; }
    public string UserName { get; private set; }
    public string DisplayName { get; private set; }
    public string PasswordHash { get; private set; }

    public void SetPasswordHash(string passwordHash) => PasswordHash = passwordHash;
}

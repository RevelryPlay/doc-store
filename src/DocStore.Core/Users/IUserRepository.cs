namespace DocStore.Core.Users;

public interface IUserRepository
{
    Task<User?> FindByUserNameAsync(string userName, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<User>> ListAsync(CancellationToken cancellationToken);
}

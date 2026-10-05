using DocStore.Core.Users;
using Microsoft.EntityFrameworkCore;

namespace DocStore.Api.Infrastructure.Persistence;

public sealed class UserRepository(DocStoreDbContext db) : IUserRepository
{
    public Task<User?> FindByUserNameAsync(string userName, CancellationToken cancellationToken) =>
        db.Users.FirstOrDefaultAsync(u => u.UserName == userName, cancellationToken);

    public Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken) =>
        db.Users.AnyAsync(u => u.Id == userId, cancellationToken);

    public async Task<IReadOnlyList<User>> ListAsync(CancellationToken cancellationToken) =>
        await db.Users.AsNoTracking().OrderBy(u => u.DisplayName).ToListAsync(cancellationToken);
}

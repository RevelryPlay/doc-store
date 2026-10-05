using DocStore.Core.Users;

namespace DocStore.Api.Endpoints;

public static class UserEndpoints
{
    public sealed record UserSummary(Guid Id, string DisplayName);

    public static void MapUserEndpoints(this RouteGroupBuilder api) =>
        api.MapGet("/users", async (IUserRepository users, CancellationToken cancellationToken) =>
            (await users.ListAsync(cancellationToken)).Select(u => new UserSummary(u.Id, u.DisplayName)));
}

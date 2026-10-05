using System.Security.Claims;
using DocStore.Core.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;

namespace DocStore.Api.Auth;

public static class AuthEndpoints
{
    public sealed record LoginRequest(string UserName, string Password);

    public sealed record SessionUser(Guid Id, string DisplayName);

    public static void MapAuthEndpoints(this RouteGroupBuilder api)
    {
        var auth = api.MapGroup("/auth").AllowAnonymous();

        // Also the SPA's bootstrap call: it always issues a fresh antiforgery token.
        auth.MapGet("/me", (HttpContext context) =>
        {
            Xsrf.IssueToken(context);
            return context.User.Identity?.IsAuthenticated == true
                ? Results.Ok(ToSessionUser(context.User))
                : Results.Unauthorized();
        });

        auth.MapPost("/login", async (
            LoginRequest request,
            IUserRepository users,
            IPasswordHasher<User> hasher,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            var user = await users.FindByUserNameAsync(request.UserName, cancellationToken);
            if (user is null || hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
            {
                return Results.Problem(detail: "Invalid user name or password.", statusCode: StatusCodes.Status401Unauthorized);
            }

            var principal = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(ClaimTypes.Name, user.DisplayName)],
                CookieAuthenticationDefaults.AuthenticationScheme));

            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            context.User = principal;
            Xsrf.IssueToken(context);
            return Results.Ok(ToSessionUser(principal));
        });

        auth.MapPost("/logout", async (HttpContext context) =>
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            context.User = new ClaimsPrincipal(new ClaimsIdentity());
            Xsrf.IssueToken(context);
            return Results.NoContent();
        });
    }

    private static SessionUser ToSessionUser(ClaimsPrincipal principal) =>
        new(principal.GetUserId(), principal.Identity!.Name!);
}

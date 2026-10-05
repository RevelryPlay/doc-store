using Microsoft.AspNetCore.Antiforgery;

namespace DocStore.Api.Auth;

/// <summary>
/// Double-submit antiforgery for the SPA: the request token goes in a readable XSRF-TOKEN cookie,
/// which Angular's HttpClient echoes back as the X-XSRF-TOKEN header on unsafe requests.
/// </summary>
public static class Xsrf
{
    public const string CookieName = "XSRF-TOKEN";
    public const string HeaderName = "X-XSRF-TOKEN";

    // Tokens are bound to the signed-in user, so this must run again whenever the identity changes.
    public static void IssueToken(HttpContext context)
    {
        var tokens = context.RequestServices.GetRequiredService<IAntiforgery>().GetAndStoreTokens(context);
        context.Response.Cookies.Append(CookieName, tokens.RequestToken!, new CookieOptions
        {
            HttpOnly = false,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/",
        });
    }

    public static IApplicationBuilder UseApiAntiforgery(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            if (IsUnsafe(context.Request.Method) && context.Request.Path.StartsWithSegments("/api"))
            {
                var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
                if (!await antiforgery.IsRequestValidAsync(context))
                {
                    await Results.Problem(detail: "Missing or invalid antiforgery token.", statusCode: StatusCodes.Status400BadRequest)
                        .ExecuteAsync(context);
                    return;
                }
            }

            await next(context);
        });

    private static bool IsUnsafe(string method) =>
        !(HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method));
}

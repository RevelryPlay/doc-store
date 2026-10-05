using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DocStore.IntegrationTests;

/// <summary>Signed-in HTTP client that echoes the XSRF cookie as a header, the same way Angular does.</summary>
public sealed class ApiClient(HttpClient http) : IDisposable
{
    private const string XsrfCookie = "XSRF-TOKEN";
    private const string XsrfHeader = "X-XSRF-TOKEN";

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public async Task SignInAsync(string userName, string password)
    {
        CaptureXsrf(await SendAsync(HttpMethod.Get, "/api/auth/me"));
        var login = await http.PostAsJsonAsync("/api/auth/login", new { userName, password }, CancellationToken);
        login.EnsureSuccessStatusCode();
        CaptureXsrf(login);
    }

    public Task<HttpResponseMessage> SendAsync(HttpMethod method, string url) =>
        http.SendAsync(new HttpRequestMessage(method, new Uri(url, UriKind.Relative)), CancellationToken);

    public async Task<T> GetAsync<T>(string url)
    {
        var response = await SendAsync(HttpMethod.Get, url);
        response.EnsureSuccessStatusCode();
        return await ReadAsync<T>(response);
    }

    public static async Task<T> ReadAsync<T>(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(Json, CancellationToken))!;

    public void RemoveXsrfHeader() => http.DefaultRequestHeaders.Remove(XsrfHeader);

    public Task<HttpResponseMessage> UploadAsync(string fileName, byte[] content)
    {
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        var form = new MultipartFormDataContent { { file, "file", fileName } };
        return http.PostAsync(new Uri("/api/documents", UriKind.Relative), form, CancellationToken);
    }

    public void Dispose() => http.Dispose();

    private void CaptureXsrf(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            return;
        }

        foreach (var cookie in cookies)
        {
            if (cookie.StartsWith(XsrfCookie + "=", StringComparison.Ordinal))
            {
                var value = cookie[(XsrfCookie.Length + 1)..].Split(';')[0];
                http.DefaultRequestHeaders.Remove(XsrfHeader);
                http.DefaultRequestHeaders.Add(XsrfHeader, Uri.UnescapeDataString(value));
            }
        }
    }
}

public sealed record Library(List<ListItem> Owned, List<ListItem> SharedWithMe);

public sealed record ListItem(Guid Id, string FileName, string OwnerName, string Status, int ShareCount, bool IsLinkShared);

public sealed record Details(Guid Id, string FileName, string Status, int? PageCount, string? Title, string Access, string? ShareLinkToken, List<Guid> SharedWith);

public sealed record UserSummary(Guid Id, string DisplayName);

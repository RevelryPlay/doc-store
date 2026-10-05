using System.Security.Claims;
using DocStore.Api.Auth;
using DocStore.Core.Documents;
using Microsoft.AspNetCore.Mvc;

namespace DocStore.Api.Endpoints;

public static class DocumentEndpoints
{
    // Room for multipart boundaries and headers on top of the file itself.
    private const long MultipartOverheadBytes = 64 * 1024;

    public static void MapDocumentEndpoints(this RouteGroupBuilder api, UploadOptions uploadOptions)
    {
        var documents = api.MapGroup("/documents");

        documents.MapGet("/", (ClaimsPrincipal user, IDocumentQueries queries, CancellationToken cancellationToken) =>
            queries.GetLibraryAsync(user.GetUserId(), cancellationToken));

        // Limits are enforced by the server before buffering, not just by the use case after.
        documents.MapPost("/", UploadAsync)
            .DisableAntiforgery()
            .WithMetadata(new RequestSizeLimitAttribute(uploadOptions.MaxSizeBytes + MultipartOverheadBytes))
            .WithFormOptions(multipartBodyLengthLimit: uploadOptions.MaxSizeBytes + MultipartOverheadBytes);

        documents.MapGet("/{id:guid}", async (
            Guid id, string? link, ClaimsPrincipal user, DocumentReader reader, CancellationToken cancellationToken) =>
            (await reader.GetAsync(id, user.GetUserId(), link, cancellationToken)).ToHttp(Results.Ok));

        documents.MapGet("/{id:guid}/content", async (
            Guid id, string? link, ClaimsPrincipal user, DocumentReader reader, CancellationToken cancellationToken) =>
            (await reader.OpenAsync(id, user.GetUserId(), link, cancellationToken))
                .ToHttp(file => Results.File(file.Content, file.ContentType, file.FileName)));

        documents.MapGet("/{id:guid}/activity", async (
            Guid id, ClaimsPrincipal user, DocumentReader reader, CancellationToken cancellationToken) =>
            (await reader.GetActivityAsync(id, user.GetUserId(), cancellationToken)).ToHttp(Results.Ok));

        documents.MapDelete("/{id:guid}", async (
            Guid id, ClaimsPrincipal user, DeleteDocument delete, CancellationToken cancellationToken) =>
            (await delete.ExecuteAsync(id, user.GetUserId(), cancellationToken)).ToHttp());

        documents.MapPut("/{id:guid}/shares/{userId:guid}", async (
            Guid id, Guid userId, ClaimsPrincipal user, DocumentSharing sharing, CancellationToken cancellationToken) =>
            (await sharing.ShareAsync(id, user.GetUserId(), userId, cancellationToken)).ToHttp());

        documents.MapDelete("/{id:guid}/shares/{userId:guid}", async (
            Guid id, Guid userId, ClaimsPrincipal user, DocumentSharing sharing, CancellationToken cancellationToken) =>
            (await sharing.UnshareAsync(id, user.GetUserId(), userId, cancellationToken)).ToHttp());

        documents.MapPut("/{id:guid}/link", async (
            Guid id, ClaimsPrincipal user, DocumentSharing sharing, CancellationToken cancellationToken) =>
            (await sharing.EnableLinkAsync(id, user.GetUserId(), cancellationToken)).ToHttp(token => Results.Ok(new { token })));

        documents.MapDelete("/{id:guid}/link", async (
            Guid id, ClaimsPrincipal user, DocumentSharing sharing, CancellationToken cancellationToken) =>
            (await sharing.DisableLinkAsync(id, user.GetUserId(), cancellationToken)).ToHttp());

        api.MapGet("/links/{token}", async (
            string token, ClaimsPrincipal user, DocumentReader reader, CancellationToken cancellationToken) =>
            (await reader.GetByLinkAsync(token, user.GetUserId(), cancellationToken)).ToHttp(Results.Ok));
    }

    private static async Task<IResult> UploadAsync(
        IFormFile file, ClaimsPrincipal user, UploadDocument upload, CancellationToken cancellationToken)
    {
        await using var content = file.OpenReadStream();
        var result = await upload.ExecuteAsync(user.GetUserId(), file.FileName, content, cancellationToken);
        return result.ToHttp(id => Results.Created($"/api/documents/{id}", new { id }));
    }
}

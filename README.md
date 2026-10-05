# Doc Store

An example employee document store: an Angular client over an ASP.NET Core (.NET 10) API backed by SQL Server. Users upload PDFs, see their own and shared documents, delete them, and share them with specific people or by link. Owners can see who has viewed or downloaded each document. Uploaded PDFs are processed in the background to extract page count, metadata, and a text preview.

The scope is deliberately small. Each concept (layering, sharing rules, background processing, security, accessibility, testing) is implemented once, properly, with the extension points described below rather than built out.

## Prerequisites

- .NET 10 SDK (with ASP.NET Core runtime)
- Docker with Compose
- Node 24 and npm

## Setup

```sh
cp env.example .env          # then set a strong MSSQL_SA_PASSWORD
docker compose up -d --wait
dotnet tool restore
dotnet dev-certs https --trust

# The API reads its connection string from user-secrets, never from committed config.
set -a; . ./.env; set +a
dotnet user-secrets --project src/DocStore.Api set ConnectionStrings:DocStore \
  "Server=localhost,1433;Database=DocStore;User Id=sa;Password=$MSSQL_SA_PASSWORD;TrustServerCertificate=True"
```

## Run

```sh
dotnet run --project src/DocStore.Api   # API on https://localhost:7075
cd client && npm ci && npm start        # UI on http://localhost:4200, proxies /api to the API
```

In Development the API applies migrations and seeds demo users and documents.

### Demo accounts

All demo users share the password `demo-password`. This is seed data for local use only.

| User name | Display name | Seeded documents |
| --- | --- | --- |
| `avery` | Avery Chen | Owns *Onboarding checklist* (shared with Blake) and *Employee handbook* (private, multi-page) |
| `blake` | Blake Rivera | Owns *Benefits overview* (link sharing only) |
| `casey` | Casey Morgan | Owns *Team handbook* (shared with Avery and Blake) |

That gives each user a different view: Casey cannot see the onboarding checklist at all, and the benefits overview appears only in Blake's list but opens for anyone signed in who has its link.

### Reset demo data

Seeding only runs against an empty database. To start over (this deletes all local documents and users):

```sh
docker compose down -v                 # drop the SQL Server volume
rm -rf src/DocStore.Api/App_Data       # remove stored document files
docker compose up -d --wait
dotnet run --project src/DocStore.Api  # migrates and reseeds
```

## Test

```sh
dotnet test
cd client && npm test && npm run lint && npm run format:check
```

Integration tests start their own SQL Server container, so Docker must be running. They run the seeded three-user scenario end to end over HTTP, including the antiforgery handshake the browser performs.

## Architecture

```
src/
  DocStore.Core/     Domain model, access rules, ports (interfaces), use cases. No infrastructure packages.
  DocStore.Api/      Endpoints, auth, composition root, and Infrastructure/ (EF Core, Dapper, storage, PDF, worker).
tests/
  DocStore.UnitTests/         Domain and use cases against in-memory fakes; the PDF processor.
  DocStore.IntegrationTests/  The real API against SQL Server in Testcontainers.
client/              Angular 21 (standalone components, signals, RxJS, zoneless).
```

Two backend projects is a deliberate choice. The dependency rule that matters (business logic never references infrastructure) is enforced by the project reference from Api to Core. Splitting further into Domain, Application, and Infrastructure projects would add ceremony without adding protection at this size.

An upload flows through it like this:

1. `DocumentEndpoints` binds the multipart file. Kestrel enforces the size limit before the body is buffered.
2. `UploadDocument` sniffs the leading bytes, asks the registered processors which one recognizes the file, stores the content under a random key, and commits the row and an audit entry together.
3. The document ID goes onto an in-process `Channel<Guid>`. The request returns `201` immediately.
4. `ProcessingWorker` (a `BackgroundService`) reads the channel, opens a DI scope per document, and runs `ProcessDocument`, which marks the document `Ready` or `Failed`.
5. The client polls the document with `timer` + `switchMap` + `takeWhile` until it leaves `Pending`, then announces the result.

## Design notes

### How the principles show up

- **Single responsibility.** Each use case (`UploadDocument`, `DocumentReader`, `DocumentSharing`, `DeleteDocument`, `ProcessDocument`) does one job. Authorization lives only in `DocumentAccess`, a pure function that every read and write goes through. The `Document` entity guards its own invariants: no public setters, sharing with the owner is rejected, and processing results can only be applied once.
- **Open/closed.** Supporting a new file type means adding one `IDocumentProcessor` and registering it. Upload detection and background processing both discover formats through the registered processors, so no existing code changes. See [Adding a file type](#adding-a-file-type).
- **Liskov substitution.** `IDocumentStorage` has a file system implementation in the API and an in-memory implementation in the unit tests. The use cases behave identically against either, which is what lets them be tested without a disk or database.
- **Interface segregation.** Ports are small and shaped by their callers. Writes go through `IDocumentRepository`. The list view reads through `IDocumentQueries`. `IAuditLog` and `IProcessingQueue` each have one method.
- **Dependency inversion.** Core defines the ports. Infrastructure implements them, and `ServiceCollectionExtensions` is the only place that knows which implementation backs which port.
- **DRY, applied where there are real repeats.** `DocumentAccess.RequireOwner` backs five owner-only operations. `DocumentSharing` runs its four mutations through a single load, check, audit, and save path. `ResultHttpExtensions` maps every `Result` to HTTP in one place. On the client, one `DocumentTable` component renders both the owned and shared lists.

### What was intentionally left out

- No generic repository or mediator library. The use cases are plain classes injected into endpoints.
- No custom clock abstraction. `TimeProvider` is built into .NET.
- Use cases return `Result<T>` instead of throwing for expected outcomes like "not found" or "invalid file". Exceptions are reserved for genuinely unexpected failures.

### Security

- **No existence leaks.** Users without access to a document get `404`, not `403`. `403` is only returned to people who can already see the document but are not its owner.
- **Link tokens** are 256 bits from a cryptographic RNG, compared in constant time, and stored under a unique filtered index. Link access still requires sign-in, so every download is attributable in the audit log.
- **Uploads.** File type is decided by magic bytes, never by the client's file name or content type. Content is stored under a random key, so user-supplied names never reach the file system. Size limits are enforced by the server before buffering.
- **Session.** Cookie auth is `HttpOnly`, `Secure`, and `SameSite=Strict`. Unsafe requests require a double-submit antiforgery token, which Angular's `HttpClient` sends automatically. The token is re-issued whenever the identity changes.
- **Smaller details.** `X-Content-Type-Options: nosniff` is set on all responses. The login page only follows same-site return URLs. Audit entries record who granted or revoked access and to whom, plus every view and download by someone other than the owner, and they survive document deletion. Only the owner can read a document's activity.

### Data access

EF Core handles writes and migrations. The library list, which is the hottest read, is one hand-written Dapper query (`DocumentQueries`) that shares the request's EF connection. Indexes are shaped to the queries: a filtered index on pending documents for worker recovery, a unique filtered index on link tokens, and indexes on owner and share recipient.

**GUID keys and clustering.** Entities get `Guid.CreateVersion7()` IDs in the domain, so objects have identity before they are saved. SQL Server orders `uniqueidentifier` by its last six bytes first, which in a v7 GUID are random, so the clustered primary keys here will fragment under heavy insert load. At this scale that is irrelevant. At production scale the fix is either a nonclustered primary key with the clustered index on `UploadedAt`, or SQL Server-ordered sequential GUIDs generated at insert.

### Background processing

The queue is an in-process channel. The database is the source of truth: on startup the worker re-queues anything still `Pending`, so a restart loses nothing. Each document gets its own DI scope and `DbContext`, and a failure in one document is logged and marks that document `Failed` without stopping the worker.

### Accessibility

The client targets WCAG 2.2 AA:

- Semantic landmarks and native controls, including a native modal `<dialog>` for sharing. It provides the focus trap and Escape handling, and returns focus to the button that opened it.
- Focus moves to the page heading on every route change.
- A single `role="status"` region announces upload, processing, sharing, and delete outcomes.
- AA contrast for text and for control borders in both light and dark themes.
- A skip link, and respect for `prefers-reduced-motion`.
- angular-eslint's template accessibility rules run as part of lint.

## Adding a file type

1. Implement `IDocumentProcessor` in `src/DocStore.Api/Infrastructure/Processing/`:

   ```csharp
   public sealed class PngProcessor : IDocumentProcessor
   {
       public string ContentType => "image/png";

       private static ReadOnlySpan<byte> Signature => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

       public bool IsMatch(ReadOnlySpan<byte> header) => header.StartsWith(Signature);

       public Task<ProcessingResult> ProcessAsync(Stream content, CancellationToken cancellationToken) =>
           Task.FromResult(new ProcessingResult(PageCount: 1, Title: null, Author: null, TextPreview: null));
   }
   ```

2. Register it next to the PDF processor in `ServiceCollectionExtensions`:

   ```csharp
   services.AddSingleton<IDocumentProcessor, PngProcessor>();
   ```

Uploads of that type are now accepted and processed. Nothing else changes.

## Where this goes next

- **Durable processing.** In production, the in-memory channel would be replaced by a durable workflow engine such as Temporal, for retries, visibility, and processing across multiple API instances.
- **Ingestion sources.** A watched folder (for example, a scanner drop folder) would feed the same processing queue, as would a desktop bulk-import utility.
- **Cloud storage.** A Google Cloud Storage `IDocumentStorage`, with downloads served through short-lived signed URLs instead of streaming through the API.
- **HRIS integration.** Employee records synced through adapters (CSV import, REST providers) behind an `IHrisSource` port, so documents can be attached to employees rather than only to users.
- **More processors.** Images, Office documents, and OCR for scanned PDFs.
- **Audit and access.** A cross-document audit view for administrators, roles (for example, HR administrators), and tenant isolation.
- **Login hardening.** Rate limiting and lockout, and equal response timing for unknown user names.
- **Delivery.** A CI pipeline running both test suites, and migrations applied as a release step instead of at startup.

## How I used AI tools

I built this with Claude Code as a pair, and treated its output the way I would treat a pull request from a colleague: nothing was committed until I had reviewed and verified it.

- **Exploring and planning.** I started from the job description and worked through scope with the agent before any code existed: what to build, what to describe instead, and which abstractions would earn their place. I cut the scope twice during planning to keep the demo focused.
- **Implementing.** The agent wrote code in small stages (scaffold, core, API, client, docs). I reviewed each stage and verified it before approving the commit.
- **Verifying.** Builds treat warnings as errors with the .NET analyzers enabled. Every stage ran the unit tests, the integration tests against a real SQL Server, lint, and formatting checks. I also ran the app myself: signed in as each demo user to confirm their document lists, worked through the sharing dialog and the rest of the UI by keyboard, uploaded a 53-page, 11 MB PDF, opened a share link as a different user in a separate browser session, and deleted a shared document to confirm it disappeared for the recipient.
- **Reviewing.** Each stage got a separate review pass before it came back to me. Those reviews caught real issues, such as authorization ordering in sharing, a startup deadlock in the processing queue, and form controls whose borders did not meet contrast requirements.
- **Protecting sensitive information.** Secrets stay in a git-ignored `.env` and in user-secrets, and the agent verified the `.env` format without printing the password. The only data in the project is generated seed data.

## License

MIT

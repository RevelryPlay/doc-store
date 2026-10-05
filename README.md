# Doc Store

An example employee document store: an Angular client over an ASP.NET Core (.NET 10) API backed by SQL Server. Users upload PDFs, see their own and shared documents, delete, and share with specific people or by link.

## Prerequisites

- .NET 10 SDK (with ASP.NET Core runtime)
- Docker with Compose
- Node 24 and npm

## Setup

```sh
cp env.example .env          # then set a strong MSSQL_SA_PASSWORD
docker compose up -d
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
docker compose up -d
dotnet run --project src/DocStore.Api  # migrates and reseeds
```

## Test

```sh
dotnet test
cd client && npm test && npm run lint && npm run format:check
```

Integration tests start their own SQL Server container, so Docker must be running.

## Design notes

**GUID keys and clustering.** Entities get `Guid.CreateVersion7()` IDs in the domain, so objects have identity before they are saved. SQL Server orders `uniqueidentifier` by its last six bytes first, which in a v7 GUID are random, so the clustered primary keys here will fragment under heavy insert load. At this scale that is irrelevant. At production scale the fix is either a nonclustered primary key with the clustered index on `UploadedAt`, or SQL Server-ordered sequential GUIDs generated at insert.

## License

MIT

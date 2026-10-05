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
dotnet run --project src/DocStore.Api
```

In Development the API applies migrations and seeds three demo users (`avery`, `blake`, `casey`, password `demo-password`) with the sample sharing scenario.

## Test

```sh
dotnet test
```

Integration tests start their own SQL Server container, so Docker must be running.

## Design notes

**GUID keys and clustering.** Entities get `Guid.CreateVersion7()` IDs in the domain, so objects have identity before they are saved. SQL Server orders `uniqueidentifier` by its last six bytes first, which in a v7 GUID are random, so the clustered primary keys here will fragment under heavy insert load. At this scale that is irrelevant. At production scale the fix is either a nonclustered primary key with the clustered index on `UploadedAt`, or SQL Server-ordered sequential GUIDs generated at insert.

## License

MIT

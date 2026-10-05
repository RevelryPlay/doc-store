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
```

## Run

```sh
dotnet run --project src/DocStore.Api
```

## Test

```sh
dotnet test
```

Integration tests start their own SQL Server container, so Docker must be running.

## License

MIT

using DocStore.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;

[assembly: AssemblyFixture(typeof(DocStore.IntegrationTests.DocStoreFactory))]

namespace DocStore.IntegrationTests;

/// <summary>
/// Runs the real API against a throwaway SQL Server container, seeded with the demo scenario.
/// </summary>
public sealed class DocStoreFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string DemoPassword = "demo-password";

    private readonly MsSqlContainer _sql = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();
    private readonly string _storageRoot = Path.Combine(Path.GetTempPath(), "docstore-tests-" + Guid.NewGuid().ToString("N"));

    public async ValueTask InitializeAsync()
    {
        await _sql.StartAsync();

        // Start the host here: lazy startup is not thread-safe when test classes run in parallel.
        StartServer();
    }

    public async Task<ApiClient> SignInAsync(string userName)
    {
        var client = new ApiClient(CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") }));
        await client.SignInAsync(userName, DemoPassword);
        return client;
    }

    public async Task<T> QueryDbAsync<T>(Func<DocStoreDbContext, Task<T>> query)
    {
        await using var scope = Services.CreateAsyncScope();
        return await query(scope.ServiceProvider.GetRequiredService<DocStoreDbContext>());
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var connectionString = new SqlConnectionStringBuilder(_sql.GetConnectionString()) { InitialCatalog = "DocStore" }.ConnectionString;

        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:DocStore", connectionString);
        builder.UseSetting("Storage:RootPath", _storageRoot);
        builder.UseSetting("Demo:Password", DemoPassword);
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _sql.DisposeAsync();
        if (Directory.Exists(_storageRoot))
        {
            Directory.Delete(_storageRoot, recursive: true);
        }
    }
}

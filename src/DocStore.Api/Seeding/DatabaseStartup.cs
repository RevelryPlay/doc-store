using DocStore.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DocStore.Api.Seeding;

public static class DatabaseStartup
{
    // Development convenience; a production deployment would apply migrations as a release step.
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();

        if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
        {
            await scope.ServiceProvider.GetRequiredService<DocStoreDbContext>().Database.MigrateAsync();
        }

        var demo = app.Configuration.GetSection("Demo").Get<DemoOptions>();
        if (demo is { Seed: true })
        {
            await scope.ServiceProvider.GetRequiredService<DemoSeeder>().SeedAsync(demo.Password, CancellationToken.None);
        }
    }
}

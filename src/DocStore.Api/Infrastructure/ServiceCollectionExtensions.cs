using DocStore.Api.Infrastructure.Persistence;
using DocStore.Api.Infrastructure.Processing;
using DocStore.Api.Infrastructure.Storage;
using DocStore.Api.Seeding;
using DocStore.Core.Auditing;
using DocStore.Core.Common;
using DocStore.Core.Documents;
using DocStore.Core.Processing;
using DocStore.Core.Storage;
using DocStore.Core.Users;
using Microsoft.EntityFrameworkCore;

namespace DocStore.Api.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDocStore(this IServiceCollection services, IConfiguration configuration, UploadOptions uploadOptions)
    {
        var connectionString = configuration.GetConnectionString("DocStore")
            ?? throw new InvalidOperationException("ConnectionStrings:DocStore is not configured. See README setup.");

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(uploadOptions);

        services.AddDbContext<DocStoreDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<DocStoreDbContext>());
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAuditLog, AuditLog>();
        services.AddScoped<IDocumentQueries, DocumentQueries>();

        services.AddSingleton(configuration.GetSection("Storage").Get<StorageOptions>() ?? new StorageOptions());
        services.AddSingleton<IDocumentStorage, FileSystemDocumentStorage>();

        // New formats: register another IDocumentProcessor here.
        services.AddSingleton<IDocumentProcessor, PdfProcessor>();
        services.AddSingleton<DocumentProcessors>();

        services.AddSingleton<ChannelProcessingQueue>();
        services.AddSingleton<IProcessingQueue>(sp => sp.GetRequiredService<ChannelProcessingQueue>());
        services.AddHostedService<ProcessingWorker>();

        services.AddScoped<UploadDocument>();
        services.AddScoped<DocumentReader>();
        services.AddScoped<DocumentSharing>();
        services.AddScoped<DeleteDocument>();
        services.AddScoped<ProcessDocument>();
        services.AddScoped<DemoSeeder>();

        return services;
    }
}

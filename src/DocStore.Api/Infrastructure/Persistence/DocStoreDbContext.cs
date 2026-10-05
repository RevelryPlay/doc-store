using DocStore.Core.Auditing;
using DocStore.Core.Common;
using DocStore.Core.Documents;
using DocStore.Core.Users;
using Microsoft.EntityFrameworkCore;

namespace DocStore.Api.Infrastructure.Persistence;

public sealed class DocStoreDbContext(DbContextOptions<DocStoreDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentShare> DocumentShares => Set<DocumentShare>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    Task IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken) => SaveChangesAsync(cancellationToken);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(user =>
        {
            user.Property(u => u.Id).ValueGeneratedNever();
            user.Property(u => u.UserName).HasMaxLength(64);
            user.Property(u => u.DisplayName).HasMaxLength(128);
            user.Property(u => u.PasswordHash).HasMaxLength(256);
            user.HasIndex(u => u.UserName).IsUnique();
        });

        modelBuilder.Entity<Document>(document =>
        {
            document.Property(d => d.Id).ValueGeneratedNever();
            document.Property(d => d.FileName).HasMaxLength(Document.MaxFileNameLength);
            document.Property(d => d.ContentType).HasMaxLength(100).IsUnicode(false);
            document.Property(d => d.StorageKey).HasMaxLength(64).IsUnicode(false);
            document.Property(d => d.Status).HasConversion<string>().HasMaxLength(16).IsUnicode(false);
            document.Property(d => d.Title).HasMaxLength(Document.MaxMetadataLength);
            document.Property(d => d.Author).HasMaxLength(Document.MaxMetadataLength);
            document.Property(d => d.TextPreview).HasMaxLength(Document.MaxPreviewLength);
            document.Property(d => d.ShareLinkToken).HasMaxLength(64).IsUnicode(false);

            document.HasOne<User>().WithMany().HasForeignKey(d => d.OwnerId).OnDelete(DeleteBehavior.Restrict);
            document.HasMany(d => d.Shares).WithOne().HasForeignKey(s => s.DocumentId).OnDelete(DeleteBehavior.Cascade);

            document.HasIndex(d => d.OwnerId);
            document.HasIndex(d => d.ShareLinkToken).IsUnique();
            document.HasIndex(d => d.Status).HasFilter("[Status] = 'Pending'");
        });

        modelBuilder.Entity<DocumentShare>(share =>
        {
            share.HasKey(s => new { s.DocumentId, s.UserId });
            share.HasOne<User>().WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Restrict);
            share.HasIndex(s => s.UserId);
        });

        modelBuilder.Entity<AuditEntry>(entry =>
        {
            entry.Property(e => e.Action).HasConversion<string>().HasMaxLength(32).IsUnicode(false);
            entry.HasIndex(e => e.DocumentId);
        });
    }
}

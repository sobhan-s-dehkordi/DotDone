using Microsoft.EntityFrameworkCore;
using DotDone.Core.Models;
using DotDone.Core.Enums;

namespace DotDone.Data.Context;

public class DotDoneDbContext : DbContext
{
    public DbSet<Repository> Repositories => Set<Repository>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<HeadTask> HeadTasks => Set<HeadTask>();
    public DbSet<SubTask> SubTasks => Set<SubTask>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<DailyActivityLog> ActivityLogs => Set<DailyActivityLog>();

    public DotDoneDbContext(DbContextOptions<DotDoneDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Repository
        modelBuilder.Entity<Repository>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Title).IsRequired().HasMaxLength(150);
            entity.Property(r => r.Description).HasMaxLength(500);

            entity.HasMany(r => r.Projects)
                  .WithOne(p => p.Repository)
                  .HasForeignKey(p => p.RepositoryId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(r => r.ActivityLogs)
                  .WithOne(l => l.Repository)
                  .HasForeignKey(l => l.RepositoryId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Project
        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Title).IsRequired().HasMaxLength(200);
            entity.Property(p => p.Description).HasMaxLength(500);
            entity.Property(p => p.ReviewNotes).HasMaxLength(500);

            entity.HasMany(p => p.HeadTasks)
                  .WithOne(t => t.Project)
                  .HasForeignKey(t => t.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(p => p.Attachments)
                  .WithOne(a => a.Project)
                  .HasForeignKey(a => a.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // HeadTask
        modelBuilder.Entity<HeadTask>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Title).IsRequired().HasMaxLength(200);
            entity.Property(t => t.Description).HasMaxLength(500);
            entity.Property(t => t.ReviewNotes).HasMaxLength(500);

            entity.HasMany(t => t.SubTasks)
                  .WithOne(s => s.HeadTask)
                  .HasForeignKey(s => s.HeadTaskId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(t => t.Attachments)
                  .WithOne(a => a.HeadTask)
                  .HasForeignKey(a => a.HeadTaskId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // SubTask
        modelBuilder.Entity<SubTask>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Title).IsRequired().HasMaxLength(200);
            entity.Property(s => s.Description).HasMaxLength(500);
            entity.Property(s => s.ReviewNotes).HasMaxLength(500);

            entity.HasMany(s => s.Attachments)
                  .WithOne(a => a.SubTask)
                  .HasForeignKey(a => a.SubTaskId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Attachment
        modelBuilder.Entity<Attachment>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.OriginalFileName).IsRequired().HasMaxLength(260);
            entity.Property(a => a.StoredFileName).IsRequired().HasMaxLength(260);
            entity.Property(a => a.RelativePath).IsRequired().HasMaxLength(500);
        });

        // DailyActivityLog
        modelBuilder.Entity<DailyActivityLog>(entity =>
        {
            entity.HasKey(l => l.Id);
            entity.HasIndex(l => new { l.RepositoryId, l.ActivityDate });
            entity.HasIndex(l => new { l.ProjectId, l.ActivityDate });
        });
    }
}

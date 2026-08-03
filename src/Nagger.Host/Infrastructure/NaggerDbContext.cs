using Microsoft.EntityFrameworkCore;

namespace Nagger.Host.Infrastructure;

public sealed class NaggerDbContext(DbContextOptions<NaggerDbContext> options) : DbContext(options)
{
    public DbSet<TaskEntity> Tasks => Set<TaskEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var task = modelBuilder.Entity<TaskEntity>();
        task.ToTable("one_shot_tasks");
        task.HasKey(x => x.Id);
        task.Property(x => x.Title).IsRequired();
        task.Property(x => x.DueAt).IsRequired();
        task.Property(x => x.ReminderPolicy).IsRequired();
        task.Property(x => x.Status).IsRequired();
        task.Property(x => x.CreatedAt).IsRequired();
        task.Property(x => x.UpdatedAt).IsRequired();
    }
}

public sealed class TaskEntity
{
    public long Id { get; set; }
    public required string Title { get; set; }
    public DateTimeOffset DueAt { get; set; }
    public required string ReminderPolicy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? LastReminderAt { get; set; }
    public required string Status { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
}

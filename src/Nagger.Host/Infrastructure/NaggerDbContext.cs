using Microsoft.EntityFrameworkCore;

namespace Nagger.Host.Infrastructure;

public sealed class NaggerDbContext(DbContextOptions<NaggerDbContext> options) : DbContext(options)
{
    public DbSet<TaskEntity> Tasks => Set<TaskEntity>();
    public DbSet<RecurringTaskTemplateEntity> RecurringTaskTemplates => Set<RecurringTaskTemplateEntity>();
    public DbSet<RecurringTaskInstanceEntity> RecurringTaskInstances => Set<RecurringTaskInstanceEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var task = modelBuilder.Entity<TaskEntity>();
        task.ToTable("one_shot_tasks");
        task.HasKey(x => x.Id);
        task.Property(x => x.Title).IsRequired();
        task.Property(x => x.DueAt).IsRequired();
        task.Property(x => x.Status).IsRequired();
        task.Property(x => x.CreatedAt).IsRequired();
        task.Property(x => x.UpdatedAt).IsRequired();
        task.HasIndex(x => x.Status);

        var template = modelBuilder.Entity<RecurringTaskTemplateEntity>();
        template.ToTable("recurring_task_templates");
        template.HasKey(x => x.Id);
        template.Property(x => x.Title).IsRequired();
        template.Property(x => x.StartDate).IsRequired();
        template.Property(x => x.RecurrenceEvery).IsRequired();
        template.Property(x => x.RecurrenceUnit).IsRequired();
        template.Property(x => x.Status).IsRequired();
        template.Property(x => x.CreatedAt).IsRequired();
        template.Property(x => x.UpdatedAt).IsRequired();

        var instance = modelBuilder.Entity<RecurringTaskInstanceEntity>();
        instance.ToTable("recurring_task_instances");
        instance.HasKey(x => x.Id);
        instance.Property(x => x.RecurringTaskId).IsRequired();
        instance.Property(x => x.Title).IsRequired();
        instance.Property(x => x.DueAt).IsRequired();
        instance.Property(x => x.Status).IsRequired();
        instance.Property(x => x.CreatedAt).IsRequired();
        instance.Property(x => x.UpdatedAt).IsRequired();
        instance
            .HasOne<RecurringTaskTemplateEntity>()
            .WithMany()
            .HasForeignKey(x => x.RecurringTaskId)
            .OnDelete(DeleteBehavior.Restrict);
        instance.HasIndex(x => x.RecurringTaskId);
        instance.HasIndex(x => x.Status);
    }
}

public sealed class TaskEntity
{
    public long Id { get; set; }
    public required string Title { get; set; }
    public DateTimeOffset DueAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public required string Status { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
}

public sealed class RecurringTaskInstanceEntity
{
    public long Id { get; set; }
    public long RecurringTaskId { get; set; }
    public required string Title { get; set; }
    public DateTimeOffset DueAt { get; set; }
    public required string Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
}

public sealed class RecurringTaskTemplateEntity
{
    public long Id { get; set; }
    public required string Title { get; set; }
    public DateOnly StartDate { get; set; }
    public int RecurrenceEvery { get; set; }
    public required string RecurrenceUnit { get; set; }
    public required string Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
}

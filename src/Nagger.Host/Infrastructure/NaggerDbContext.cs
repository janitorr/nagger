using Microsoft.EntityFrameworkCore;

namespace Nagger.Host.Infrastructure;

public sealed class NaggerDbContext(DbContextOptions<NaggerDbContext> options) : DbContext(options)
{
    public DbSet<TaskEntity> Tasks => Set<TaskEntity>();
    public DbSet<RecurringTaskTemplateEntity> RecurringTaskTemplates => Set<RecurringTaskTemplateEntity>();

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

        var template = modelBuilder.Entity<RecurringTaskTemplateEntity>();
        template.ToTable("recurring_task_templates");
        template.HasKey(x => x.Id);
        template.Property(x => x.Title).IsRequired();
        template.Property(x => x.StartDate).IsRequired();
        template.Property(x => x.RecurrenceEvery).IsRequired();
        template.Property(x => x.RecurrenceUnit).IsRequired();
        template.Property(x => x.ReminderPolicy).IsRequired();
        template.Property(x => x.Status).IsRequired();
        template.Property(x => x.CreatedAt).IsRequired();
        template.Property(x => x.UpdatedAt).IsRequired();
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
    public long? RecurringTaskId { get; set; }
}

public sealed class RecurringTaskTemplateEntity
{
    public long Id { get; set; }
    public required string Title { get; set; }
    public DateOnly StartDate { get; set; }
    public int RecurrenceEvery { get; set; }
    public required string RecurrenceUnit { get; set; }
    public required string ReminderPolicy { get; set; }
    public required string Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
}

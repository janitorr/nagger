using Microsoft.EntityFrameworkCore;
using Nagger.Core.Tasks.Domain;
using Nagger.Host.Infrastructure;
using Shouldly;

namespace Nagger.Host.Tests;

public sealed class MigrationTests
{
    [Theory]
    [InlineData("Days", "days", RecurrenceUnit.Days)]
    [InlineData("Weeks", "weeks", RecurrenceUnit.Weeks)]
    [InlineData("Months", "months", RecurrenceUnit.Months)]
    public async Task RecurrenceUnitMigration_GivenLegacyUpperCaseRow_WhenMigrationApplied_ThenStoresLowercaseAndParses(
        string legacyValue,
        string expectedStored,
        RecurrenceUnit expectedUnit
    )
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"nagger-{Guid.NewGuid():N}.db");
        try
        {
            using (var context = CreateContext(databasePath))
            {
                await context.Database.MigrateAsync("20260823190839_RemoveReminderPolicy");
                context.RecurringTaskTemplates.Add(
                    new RecurringTaskTemplateEntity
                    {
                        Title = "Team sync",
                        StartDate = new DateOnly(2026, 8, 4),
                        RecurrenceEvery = 1,
                        RecurrenceUnit = legacyValue,
                        Status = "active",
                        CreatedAt = default,
                        UpdatedAt = default,
                    }
                );
                context.RecurringTaskInstances.Add(
                    new RecurringTaskInstanceEntity
                    {
                        RecurringTaskId = 1,
                        Title = "Team sync",
                        DueAt = new DateTimeOffset(2026, 8, 4, 0, 0, 0, TimeSpan.Zero),
                        Status = "active",
                        CreatedAt = default,
                        UpdatedAt = default,
                    }
                );
                await context.SaveChangesAsync();
            }

            using (var context = CreateContext(databasePath))
            {
                await context.Database.MigrateAsync();
                var entity = await context.RecurringTaskTemplates.SingleAsync();
                entity.RecurrenceUnit.ShouldBe(expectedStored);
                RecurrenceUnits.FromContractValue(entity.RecurrenceUnit).ShouldBe(expectedUnit);
                (await context.RecurringTaskInstances.SingleAsync()).RecurringTaskId.ShouldBe(entity.Id);
            }
        }
        finally
        {
            if (File.Exists(databasePath))
                File.Delete(databasePath);
        }
    }

    private static NaggerDbContext CreateContext(string databasePath)
    {
        var options = new DbContextOptionsBuilder<NaggerDbContext>().UseSqlite($"Data Source={databasePath}").Options;
        return new NaggerDbContext(options);
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nagger.Host.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRecurringTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "RecurringTaskId",
                table: "one_shot_tasks",
                type: "INTEGER",
                nullable: true
            );

            migrationBuilder.CreateTable(
                name: "recurring_task_templates",
                columns: table => new
                {
                    Id = table
                        .Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    RecurrenceEvery = table.Column<int>(type: "INTEGER", nullable: false),
                    RecurrenceUnit = table.Column<string>(type: "TEXT", nullable: false),
                    ReminderPolicy = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CancelledAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recurring_task_templates", x => x.Id);
                }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "recurring_task_templates");

            migrationBuilder.DropColumn(name: "RecurringTaskId", table: "one_shot_tasks");
        }
    }
}

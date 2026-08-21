using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nagger.Host.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeparateRecurringTaskInstances : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "RecurringTaskId", table: "one_shot_tasks");

            migrationBuilder.CreateTable(
                name: "recurring_task_instances",
                columns: table => new
                {
                    Id = table
                        .Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RecurringTaskId = table.Column<long>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    DueAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ReminderPolicy = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CancelledAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recurring_task_instances", x => x.Id);
                }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "recurring_task_instances");

            migrationBuilder.AddColumn<long>(
                name: "RecurringTaskId",
                table: "one_shot_tasks",
                type: "INTEGER",
                nullable: true
            );
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nagger.Host.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveReminderPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ReminderPolicy", table: "recurring_task_templates");

            migrationBuilder.DropColumn(name: "ReminderPolicy", table: "recurring_task_instances");

            migrationBuilder.DropColumn(name: "LastReminderAt", table: "one_shot_tasks");

            migrationBuilder.DropColumn(name: "ReminderPolicy", table: "one_shot_tasks");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReminderPolicy",
                table: "recurring_task_templates",
                type: "TEXT",
                nullable: false,
                defaultValue: ""
            );

            migrationBuilder.AddColumn<string>(
                name: "ReminderPolicy",
                table: "recurring_task_instances",
                type: "TEXT",
                nullable: false,
                defaultValue: ""
            );

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReminderAt",
                table: "one_shot_tasks",
                type: "TEXT",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "ReminderPolicy",
                table: "one_shot_tasks",
                type: "TEXT",
                nullable: false,
                defaultValue: ""
            );
        }
    }
}

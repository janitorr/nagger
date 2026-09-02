using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nagger.Host.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRecurringInstanceForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_recurring_task_instances_RecurringTaskId",
                table: "recurring_task_instances",
                column: "RecurringTaskId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_recurring_task_instances_Status",
                table: "recurring_task_instances",
                column: "Status"
            );

            migrationBuilder.CreateIndex(name: "IX_one_shot_tasks_Status", table: "one_shot_tasks", column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_recurring_task_instances_recurring_task_templates_RecurringTaskId",
                table: "recurring_task_instances",
                column: "RecurringTaskId",
                principalTable: "recurring_task_templates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_recurring_task_instances_recurring_task_templates_RecurringTaskId",
                table: "recurring_task_instances"
            );

            migrationBuilder.DropIndex(
                name: "IX_recurring_task_instances_RecurringTaskId",
                table: "recurring_task_instances"
            );

            migrationBuilder.DropIndex(name: "IX_recurring_task_instances_Status", table: "recurring_task_instances");

            migrationBuilder.DropIndex(name: "IX_one_shot_tasks_Status", table: "one_shot_tasks");
        }
    }
}

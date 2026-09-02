using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nagger.Host.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeRecurrenceUnitCase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE recurring_task_templates
                SET RecurrenceUnit = lower(RecurrenceUnit)
                WHERE RecurrenceUnit IN ('Days', 'Weeks', 'Months');
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE recurring_task_templates
                SET RecurrenceUnit = upper(RecurrenceUnit)
                WHERE RecurrenceUnit IN ('days', 'weeks', 'months');
                """
            );
        }
    }
}

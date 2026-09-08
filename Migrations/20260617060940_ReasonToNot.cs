using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MeetingProject.Migrations
{
    /// <inheritdoc />
    public partial class ReasonToNot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "Notifitications",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Reason",
                table: "Notifitications");
        }
    }
}

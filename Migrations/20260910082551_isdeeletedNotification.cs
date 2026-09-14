using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MeetingProject.Migrations
{
    /// <inheritdoc />
    public partial class isdeeletedNotification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "isDeleted",
                table: "Notifitications",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "isDeleted",
                table: "Notifitications");
        }
    }
}

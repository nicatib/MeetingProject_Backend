using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MeetingProject.Migrations
{
    /// <inheritdoc />
    public partial class someChangeInNotificaiton : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Notifitications_MeetingId",
                table: "Notifitications",
                column: "MeetingId");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifitications_Meetings_MeetingId",
                table: "Notifitications",
                column: "MeetingId",
                principalTable: "Meetings",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifitications_Meetings_MeetingId",
                table: "Notifitications");

            migrationBuilder.DropIndex(
                name: "IX_Notifitications_MeetingId",
                table: "Notifitications");
        }
    }
}

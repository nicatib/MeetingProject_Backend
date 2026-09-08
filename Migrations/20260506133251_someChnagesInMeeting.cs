using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MeetingProject.Migrations
{
    /// <inheritdoc />
    public partial class someChnagesInMeeting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Countrys_AspNetUsers_UserId",
                table: "Countrys");

            migrationBuilder.DropColumn(
                name: "CountryId",
                table: "MeetingParticipant");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Meetings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Meetings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingParticipant_GovernmentId",
                table: "MeetingParticipant",
                column: "GovernmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Countrys_AspNetUsers_UserId",
                table: "Countrys",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MeetingParticipant_StateGovs_GovernmentId",
                table: "MeetingParticipant",
                column: "GovernmentId",
                principalTable: "StateGovs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Countrys_AspNetUsers_UserId",
                table: "Countrys");

            migrationBuilder.DropForeignKey(
                name: "FK_MeetingParticipant_StateGovs_GovernmentId",
                table: "MeetingParticipant");

            migrationBuilder.DropIndex(
                name: "IX_MeetingParticipant_GovernmentId",
                table: "MeetingParticipant");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Meetings");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Meetings");

            migrationBuilder.AddColumn<int>(
                name: "CountryId",
                table: "MeetingParticipant",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_Countrys_AspNetUsers_UserId",
                table: "Countrys",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}

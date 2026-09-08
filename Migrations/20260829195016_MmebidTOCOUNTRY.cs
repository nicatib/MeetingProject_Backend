using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MeetingProject.Migrations
{
    /// <inheritdoc />
    public partial class MmebidTOCOUNTRY : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MemberId",
                table: "Countrys",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MemberId1",
                table: "Countrys",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Countrys_MemberId1",
                table: "Countrys",
                column: "MemberId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Countrys_AspNetUsers_MemberId1",
                table: "Countrys",
                column: "MemberId1",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Countrys_AspNetUsers_MemberId1",
                table: "Countrys");

            migrationBuilder.DropIndex(
                name: "IX_Countrys_MemberId1",
                table: "Countrys");

            migrationBuilder.DropColumn(
                name: "MemberId",
                table: "Countrys");

            migrationBuilder.DropColumn(
                name: "MemberId1",
                table: "Countrys");
        }
    }
}

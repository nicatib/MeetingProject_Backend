using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MeetingProject.Migrations
{
    /// <inheritdoc />
    public partial class MeetingRoomidToCountry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Countrys_AspNetUsers_MemberId1",
                table: "Countrys");

            migrationBuilder.DropForeignKey(
                name: "FK_Countrys_AspNetUsers_UserId",
                table: "Countrys");

            migrationBuilder.DropIndex(
                name: "IX_Countrys_MemberId1",
                table: "Countrys");

            migrationBuilder.DropColumn(
                name: "MemberId1",
                table: "Countrys");

            migrationBuilder.AlterColumn<string>(
                name: "MemberId",
                table: "Countrys",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MeetingRoomId",
                table: "Countrys",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Countrys_MeetingRoomId",
                table: "Countrys",
                column: "MeetingRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_Countrys_MemberId",
                table: "Countrys",
                column: "MemberId");

            migrationBuilder.AddForeignKey(
                name: "FK_Countrys_AspNetUsers_MemberId",
                table: "Countrys",
                column: "MemberId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Countrys_AspNetUsers_UserId",
                table: "Countrys",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Countrys_HotelRooms_MeetingRoomId",
                table: "Countrys",
                column: "MeetingRoomId",
                principalTable: "HotelRooms",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Countrys_AspNetUsers_MemberId",
                table: "Countrys");

            migrationBuilder.DropForeignKey(
                name: "FK_Countrys_AspNetUsers_UserId",
                table: "Countrys");

            migrationBuilder.DropForeignKey(
                name: "FK_Countrys_HotelRooms_MeetingRoomId",
                table: "Countrys");

            migrationBuilder.DropIndex(
                name: "IX_Countrys_MeetingRoomId",
                table: "Countrys");

            migrationBuilder.DropIndex(
                name: "IX_Countrys_MemberId",
                table: "Countrys");

            migrationBuilder.DropColumn(
                name: "MeetingRoomId",
                table: "Countrys");

            migrationBuilder.AlterColumn<string>(
                name: "MemberId",
                table: "Countrys",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

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

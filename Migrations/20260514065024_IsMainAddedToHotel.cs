using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MeetingProject.Migrations
{
    /// <inheritdoc />
    public partial class IsMainAddedToHotel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Countrys_AspNetUsers_UserId",
                table: "Countrys");

            migrationBuilder.AddColumn<bool>(
                name: "IsMain",
                table: "Hotels",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "HotelId",
                table: "Countrys",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Countrys_HotelId",
                table: "Countrys",
                column: "HotelId");

            migrationBuilder.AddForeignKey(
                name: "FK_Countrys_AspNetUsers_UserId",
                table: "Countrys",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Countrys_Hotels_HotelId",
                table: "Countrys",
                column: "HotelId",
                principalTable: "Hotels",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Countrys_AspNetUsers_UserId",
                table: "Countrys");

            migrationBuilder.DropForeignKey(
                name: "FK_Countrys_Hotels_HotelId",
                table: "Countrys");

            migrationBuilder.DropIndex(
                name: "IX_Countrys_HotelId",
                table: "Countrys");

            migrationBuilder.DropColumn(
                name: "IsMain",
                table: "Hotels");

            migrationBuilder.DropColumn(
                name: "HotelId",
                table: "Countrys");

            migrationBuilder.AddForeignKey(
                name: "FK_Countrys_AspNetUsers_UserId",
                table: "Countrys",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}

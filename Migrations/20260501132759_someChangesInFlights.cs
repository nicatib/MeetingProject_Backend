using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MeetingProject.Migrations
{
    /// <inheritdoc />
    public partial class someChangesInFlights : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Countrys_AspNetUsers_UserId",
                table: "Countrys");

            migrationBuilder.DropIndex(
                name: "IX_Flights_StateGovId",
                table: "Flights");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Countrys",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_Flights_StateGovId",
                table: "Flights",
                column: "StateGovId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Countrys_AspNetUsers_UserId",
                table: "Countrys",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Countrys_AspNetUsers_UserId",
                table: "Countrys");

            migrationBuilder.DropIndex(
                name: "IX_Flights_StateGovId",
                table: "Flights");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Countrys",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Flights_StateGovId",
                table: "Flights",
                column: "StateGovId");

            migrationBuilder.AddForeignKey(
                name: "FK_Countrys_AspNetUsers_UserId",
                table: "Countrys",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

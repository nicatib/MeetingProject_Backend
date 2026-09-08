using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MeetingProject.Migrations
{
    /// <inheritdoc />
    public partial class Nulablestategovid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Seats_StateGovs_StateGovId",
                table: "Seats");

            migrationBuilder.AlterColumn<int>(
                name: "StateGovId",
                table: "Seats",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_Seats_StateGovs_StateGovId",
                table: "Seats",
                column: "StateGovId",
                principalTable: "StateGovs",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Seats_StateGovs_StateGovId",
                table: "Seats");

            migrationBuilder.AlterColumn<int>(
                name: "StateGovId",
                table: "Seats",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Seats_StateGovs_StateGovId",
                table: "Seats",
                column: "StateGovId",
                principalTable: "StateGovs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

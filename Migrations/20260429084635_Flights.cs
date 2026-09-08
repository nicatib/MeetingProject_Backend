using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MeetingProject.Migrations
{
    /// <inheritdoc />
    public partial class Flights : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FlyingTime",
                table: "StateGovs");

            migrationBuilder.DropColumn(
                name: "IsArrived",
                table: "StateGovs");

            migrationBuilder.DropColumn(
                name: "PlannedArrivedTime",
                table: "StateGovs");

            migrationBuilder.DropColumn(
                name: "RealArrivedTime",
                table: "StateGovs");

            migrationBuilder.CreateTable(
                name: "Flights",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FlightNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StateGovId = table.Column<int>(type: "int", nullable: false),
                    PlannedArrivedTime = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RealArrivedTime = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsArrived = table.Column<bool>(type: "bit", nullable: false),
                    Airline = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedTime = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    isDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedTime = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedTime = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Flights", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Flights_StateGovs_StateGovId",
                        column: x => x.StateGovId,
                        principalTable: "StateGovs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Flights_StateGovId",
                table: "Flights",
                column: "StateGovId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Flights");

            migrationBuilder.AddColumn<string>(
                name: "FlyingTime",
                table: "StateGovs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArrived",
                table: "StateGovs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PlannedArrivedTime",
                table: "StateGovs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RealArrivedTime",
                table: "StateGovs",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}

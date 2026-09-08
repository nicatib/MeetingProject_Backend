using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MeetingProject.Migrations
{
    /// <inheritdoc />
    public partial class MeetingTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Meetings_HotelRooms_HotelRoomId",
                table: "Meetings");

            migrationBuilder.DropIndex(
                name: "IX_Meetings_HotelRoomId",
                table: "Meetings");

            migrationBuilder.DropColumn(
                name: "CreatedTime",
                table: "Meetings");

            migrationBuilder.DropColumn(
                name: "DeletedTime",
                table: "Meetings");

            migrationBuilder.DropColumn(
                name: "HotelRoomId",
                table: "Meetings");

            migrationBuilder.DropColumn(
                name: "UpdatedTime",
                table: "Meetings");

            migrationBuilder.DropColumn(
                name: "isDeleted",
                table: "Meetings");

            migrationBuilder.AddColumn<DateTime>(
                name: "ActualEndTime",
                table: "Meetings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ActualStartTime",
                table: "Meetings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Meetings",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "DurationMinutes",
                table: "Meetings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "PlannedEndTime",
                table: "Meetings",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "PlannedStartTime",
                table: "Meetings",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "RoomId",
                table: "Meetings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Meetings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "MeetingParticipant",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MeetingId = table.Column<int>(type: "int", nullable: false),
                    CountryId = table.Column<int>(type: "int", nullable: false),
                    GovernmentId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingParticipant", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeetingParticipant_Meetings_MeetingId",
                        column: x => x.MeetingId,
                        principalTable: "Meetings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Meetings_RoomId",
                table: "Meetings",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingParticipant_MeetingId",
                table: "MeetingParticipant",
                column: "MeetingId");

            migrationBuilder.AddForeignKey(
                name: "FK_Meetings_HotelRooms_RoomId",
                table: "Meetings",
                column: "RoomId",
                principalTable: "HotelRooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Meetings_HotelRooms_RoomId",
                table: "Meetings");

            migrationBuilder.DropTable(
                name: "MeetingParticipant");

            migrationBuilder.DropIndex(
                name: "IX_Meetings_RoomId",
                table: "Meetings");

            migrationBuilder.DropColumn(
                name: "ActualEndTime",
                table: "Meetings");

            migrationBuilder.DropColumn(
                name: "ActualStartTime",
                table: "Meetings");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Meetings");

            migrationBuilder.DropColumn(
                name: "DurationMinutes",
                table: "Meetings");

            migrationBuilder.DropColumn(
                name: "PlannedEndTime",
                table: "Meetings");

            migrationBuilder.DropColumn(
                name: "PlannedStartTime",
                table: "Meetings");

            migrationBuilder.DropColumn(
                name: "RoomId",
                table: "Meetings");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Meetings");

            migrationBuilder.AddColumn<string>(
                name: "CreatedTime",
                table: "Meetings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DeletedTime",
                table: "Meetings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HotelRoomId",
                table: "Meetings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedTime",
                table: "Meetings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "isDeleted",
                table: "Meetings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Meetings_HotelRoomId",
                table: "Meetings",
                column: "HotelRoomId");

            migrationBuilder.AddForeignKey(
                name: "FK_Meetings_HotelRooms_HotelRoomId",
                table: "Meetings",
                column: "HotelRoomId",
                principalTable: "HotelRooms",
                principalColumn: "Id");
        }
    }
}

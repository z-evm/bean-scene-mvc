using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BeanScene.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixTypoInModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_ReservationStatuses_ReservationStatusId",
                table: "Reservations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ReservationStatuses",
                table: "ReservationStatuses");

            migrationBuilder.RenameTable(
                name: "ReservationStatuses",
                newName: "ReservationStatus");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ReservationStatus",
                table: "ReservationStatus",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_ReservationStatus_ReservationStatusId",
                table: "Reservations",
                column: "ReservationStatusId",
                principalTable: "ReservationStatus",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_ReservationStatus_ReservationStatusId",
                table: "Reservations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ReservationStatus",
                table: "ReservationStatus");

            migrationBuilder.RenameTable(
                name: "ReservationStatus",
                newName: "ReservationStatuses");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ReservationStatuses",
                table: "ReservationStatuses",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_ReservationStatuses_ReservationStatusId",
                table: "Reservations",
                column: "ReservationStatusId",
                principalTable: "ReservationStatuses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

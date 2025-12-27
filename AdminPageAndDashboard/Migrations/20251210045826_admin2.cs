using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdminPageAndDashboard.Migrations
{
    /// <inheritdoc />
    public partial class admin2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "created_at",
                value: new DateTime(2025, 12, 10, 4, 58, 26, 206, DateTimeKind.Utc).AddTicks(2592));

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "created_at",
                value: new DateTime(2025, 12, 10, 4, 58, 26, 206, DateTimeKind.Utc).AddTicks(2598));

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: 3,
                column: "created_at",
                value: new DateTime(2025, 12, 10, 4, 58, 26, 206, DateTimeKind.Utc).AddTicks(2600));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "created_at",
                value: new DateTime(2025, 12, 10, 4, 53, 27, 255, DateTimeKind.Utc).AddTicks(8902));

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "created_at",
                value: new DateTime(2025, 12, 10, 4, 53, 27, 255, DateTimeKind.Utc).AddTicks(8909));

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: 3,
                column: "created_at",
                value: new DateTime(2025, 12, 10, 4, 53, 27, 255, DateTimeKind.Utc).AddTicks(8910));
        }
    }
}

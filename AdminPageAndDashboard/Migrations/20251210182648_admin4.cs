using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdminPageAndDashboard.Migrations
{
    /// <inheritdoc />
    public partial class admin4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActivityLogs_users_UserId",
                table: "ActivityLogs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ActivityLogs",
                table: "ActivityLogs");

            migrationBuilder.RenameTable(
                name: "ActivityLogs",
                newName: "activity_logs");

            migrationBuilder.RenameColumn(
                name: "Timestamp",
                table: "activity_logs",
                newName: "timestamp");

            migrationBuilder.RenameColumn(
                name: "Details",
                table: "activity_logs",
                newName: "details");

            migrationBuilder.RenameColumn(
                name: "Action",
                table: "activity_logs",
                newName: "action");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "activity_logs",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "IpAddress",
                table: "activity_logs",
                newName: "ip_address");

            migrationBuilder.RenameColumn(
                name: "EntityType",
                table: "activity_logs",
                newName: "entity_type");

            migrationBuilder.RenameColumn(
                name: "EntityId",
                table: "activity_logs",
                newName: "entity_id");

            migrationBuilder.RenameIndex(
                name: "IX_ActivityLogs_UserId",
                table: "activity_logs",
                newName: "IX_activity_logs_user_id");

            migrationBuilder.AlterColumn<string>(
                name: "ip_address",
                table: "activity_logs",
                type: "varchar(45)",
                maxLength: 45,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(45)",
                oldMaxLength: 45)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "entity_type",
                table: "activity_logs",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "entity_id",
                table: "activity_logs",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddPrimaryKey(
                name: "PK_activity_logs",
                table: "activity_logs",
                column: "Id");

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "created_at",
                value: new DateTime(2025, 12, 10, 18, 26, 48, 496, DateTimeKind.Utc).AddTicks(8025));

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "created_at",
                value: new DateTime(2025, 12, 10, 18, 26, 48, 496, DateTimeKind.Utc).AddTicks(8028));

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: 3,
                column: "created_at",
                value: new DateTime(2025, 12, 10, 18, 26, 48, 496, DateTimeKind.Utc).AddTicks(8029));

            migrationBuilder.AddForeignKey(
                name: "FK_activity_logs_users_user_id",
                table: "activity_logs",
                column: "user_id",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_activity_logs_users_user_id",
                table: "activity_logs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_activity_logs",
                table: "activity_logs");

            migrationBuilder.RenameTable(
                name: "activity_logs",
                newName: "ActivityLogs");

            migrationBuilder.RenameColumn(
                name: "timestamp",
                table: "ActivityLogs",
                newName: "Timestamp");

            migrationBuilder.RenameColumn(
                name: "details",
                table: "ActivityLogs",
                newName: "Details");

            migrationBuilder.RenameColumn(
                name: "action",
                table: "ActivityLogs",
                newName: "Action");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "ActivityLogs",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "ip_address",
                table: "ActivityLogs",
                newName: "IpAddress");

            migrationBuilder.RenameColumn(
                name: "entity_type",
                table: "ActivityLogs",
                newName: "EntityType");

            migrationBuilder.RenameColumn(
                name: "entity_id",
                table: "ActivityLogs",
                newName: "EntityId");

            migrationBuilder.RenameIndex(
                name: "IX_activity_logs_user_id",
                table: "ActivityLogs",
                newName: "IX_ActivityLogs_UserId");

            migrationBuilder.UpdateData(
                table: "ActivityLogs",
                keyColumn: "IpAddress",
                keyValue: null,
                column: "IpAddress",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "IpAddress",
                table: "ActivityLogs",
                type: "varchar(45)",
                maxLength: 45,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(45)",
                oldMaxLength: 45,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "ActivityLogs",
                keyColumn: "EntityType",
                keyValue: null,
                column: "EntityType",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "EntityType",
                table: "ActivityLogs",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "ActivityLogs",
                keyColumn: "EntityId",
                keyValue: null,
                column: "EntityId",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "EntityId",
                table: "ActivityLogs",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ActivityLogs",
                table: "ActivityLogs",
                column: "Id");

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "created_at",
                value: new DateTime(2025, 12, 10, 18, 23, 4, 932, DateTimeKind.Utc).AddTicks(4605));

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "created_at",
                value: new DateTime(2025, 12, 10, 18, 23, 4, 932, DateTimeKind.Utc).AddTicks(4608));

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: 3,
                column: "created_at",
                value: new DateTime(2025, 12, 10, 18, 23, 4, 932, DateTimeKind.Utc).AddTicks(4609));

            migrationBuilder.AddForeignKey(
                name: "FK_ActivityLogs_users_UserId",
                table: "ActivityLogs",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

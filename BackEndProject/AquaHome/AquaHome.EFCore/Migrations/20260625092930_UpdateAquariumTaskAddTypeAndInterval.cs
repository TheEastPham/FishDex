using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AquaHome.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAquariumTaskAddTypeAndInterval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AquariumTasks_Aquariums_AquariumId",
                table: "AquariumTasks");

            migrationBuilder.DropIndex(
                name: "IX_AquariumTasks_AquariumId",
                table: "AquariumTasks");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "AquariumTasks");

            migrationBuilder.AlterColumn<Guid>(
                name: "AquariumId",
                table: "AquariumTasks",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "AquariumTasks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IntervalDays",
                table: "AquariumTasks",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AquaTaskType",
                table: "AquariumTasks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_AquariumTasks_AquariumId_IsCompleted",
                table: "AquariumTasks",
                columns: new[] { "AquariumId", "IsCompleted" });

            migrationBuilder.AddForeignKey(
                name: "FK_AquariumTasks_Aquariums_AquariumId",
                table: "AquariumTasks",
                column: "AquariumId",
                principalTable: "Aquariums",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AquariumTasks_Aquariums_AquariumId",
                table: "AquariumTasks");

            migrationBuilder.DropIndex(
                name: "IX_AquariumTasks_AquariumId_IsCompleted",
                table: "AquariumTasks");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "AquariumTasks");

            migrationBuilder.DropColumn(
                name: "IntervalDays",
                table: "AquariumTasks");

            migrationBuilder.DropColumn(
                name: "AquaTaskType",
                table: "AquariumTasks");

            migrationBuilder.AlterColumn<Guid>(
                name: "AquariumId",
                table: "AquariumTasks",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "AquariumTasks",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_AquariumTasks_AquariumId",
                table: "AquariumTasks",
                column: "AquariumId");

            migrationBuilder.AddForeignKey(
                name: "FK_AquariumTasks_Aquariums_AquariumId",
                table: "AquariumTasks",
                column: "AquariumId",
                principalTable: "Aquariums",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}

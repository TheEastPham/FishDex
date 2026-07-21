using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FishDex.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddCommonNameCommunityModeration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ContributedBy",
                table: "CommonNames",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsVerified",
                table: "CommonNames",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "CommonNames",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewedBy",
                table: "CommonNames",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommonNames_ContributedBy_IsVerified",
                table: "CommonNames",
                columns: new[] { "ContributedBy", "IsVerified" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CommonNames_ContributedBy_IsVerified",
                table: "CommonNames");

            migrationBuilder.DropColumn(
                name: "ContributedBy",
                table: "CommonNames");

            migrationBuilder.DropColumn(
                name: "IsVerified",
                table: "CommonNames");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "CommonNames");

            migrationBuilder.DropColumn(
                name: "ReviewedBy",
                table: "CommonNames");
        }
    }
}

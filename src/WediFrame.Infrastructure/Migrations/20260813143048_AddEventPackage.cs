using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WediFrame.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEventPackage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "ExpiresAt",
                schema: "events",
                table: "events",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PackageId",
                schema: "events",
                table: "events",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "UploadEndsAt",
                schema: "events",
                table: "events",
                type: "date",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_events_ExpiresAt",
                schema: "events",
                table: "events",
                column: "ExpiresAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_events_ExpiresAt",
                schema: "events",
                table: "events");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                schema: "events",
                table: "events");

            migrationBuilder.DropColumn(
                name: "PackageId",
                schema: "events",
                table: "events");

            migrationBuilder.DropColumn(
                name: "UploadEndsAt",
                schema: "events",
                table: "events");
        }
    }
}

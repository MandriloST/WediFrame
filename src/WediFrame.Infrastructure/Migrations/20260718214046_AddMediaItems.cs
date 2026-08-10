using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WediFrame.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "media");

            migrationBuilder.CreateTable(
                name: "media_items",
                schema: "media",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ObjectKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    GuestName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UploadStatus = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Visibility = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ThumbnailKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ConfirmedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SoftDeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_media_items", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_media_items_EventId_CreatedAt",
                schema: "media",
                table: "media_items",
                columns: new[] { "EventId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_media_items_EventId_UploadStatus",
                schema: "media",
                table: "media_items",
                columns: new[] { "EventId", "UploadStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_media_items_ObjectKey",
                schema: "media",
                table: "media_items",
                column: "ObjectKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "media_items",
                schema: "media");
        }
    }
}

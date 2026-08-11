using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WediFrame.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddThumbnailStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ThumbnailStatus",
                schema: "media",
                table: "media_items",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.CreateIndex(
                name: "IX_media_items_Type_UploadStatus_ThumbnailStatus",
                schema: "media",
                table: "media_items",
                columns: new[] { "Type", "UploadStatus", "ThumbnailStatus" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_media_items_Type_UploadStatus_ThumbnailStatus",
                schema: "media",
                table: "media_items");

            migrationBuilder.DropColumn(
                name: "ThumbnailStatus",
                schema: "media",
                table: "media_items");
        }
    }
}

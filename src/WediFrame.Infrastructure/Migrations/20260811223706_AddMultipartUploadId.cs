using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WediFrame.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMultipartUploadId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MultipartUploadId",
                schema: "media",
                table: "media_items",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MultipartUploadId",
                schema: "media",
                table: "media_items");
        }
    }
}

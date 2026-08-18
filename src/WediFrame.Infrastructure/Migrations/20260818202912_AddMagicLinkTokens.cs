using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WediFrame.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMagicLinkTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "magic_link_tokens",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Purpose = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    PreferredLanguage = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ConsumedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_magic_link_tokens", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_magic_link_tokens_Email",
                schema: "identity",
                table: "magic_link_tokens",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_magic_link_tokens_TokenHash",
                schema: "identity",
                table: "magic_link_tokens",
                column: "TokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "magic_link_tokens",
                schema: "identity");
        }
    }
}

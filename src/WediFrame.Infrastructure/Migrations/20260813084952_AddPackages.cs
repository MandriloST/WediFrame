using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WediFrame.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPackages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "billing");

            migrationBuilder.CreateTable(
                name: "packages",
                schema: "billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    PriceCents = table.Column<int>(type: "integer", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    MaxPhotoCount = table.Column<int>(type: "integer", nullable: false),
                    MaxVideoTotalBytes = table.Column<long>(type: "bigint", nullable: false),
                    MaxTotalBytes = table.Column<long>(type: "bigint", nullable: false),
                    MaxFileBytes = table.Column<long>(type: "bigint", nullable: false),
                    UploadPeriodDays = table.Column<int>(type: "integer", nullable: false),
                    RetentionDays = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_packages", x => x.Id);
                });

            migrationBuilder.InsertData(
                schema: "billing",
                table: "packages",
                columns: new[] { "Id", "CreatedAt", "Currency", "IsActive", "MaxFileBytes", "MaxPhotoCount", "MaxTotalBytes", "MaxVideoTotalBytes", "Name", "PriceCents", "RetentionDays", "Slug", "SortOrder", "UploadPeriodDays" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-000000000001"), new DateTimeOffset(new DateTime(2026, 8, 13, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "EUR", true, 2147483648L, 50, 262144000L, 52428800L, "Free", 0, 7, "free", 0, 2 },
                    { new Guid("11111111-1111-1111-1111-000000000002"), new DateTimeOffset(new DateTime(2026, 8, 13, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "EUR", true, 2147483648L, 500, 10737418240L, 5368709120L, "Essential", 2500, 90, "essential", 1, 30 },
                    { new Guid("11111111-1111-1111-1111-000000000003"), new DateTimeOffset(new DateTime(2026, 8, 13, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "EUR", true, 2147483648L, 1500, 21474836480L, 16106127360L, "Classic", 4000, 365, "classic", 2, 60 },
                    { new Guid("11111111-1111-1111-1111-000000000004"), new DateTimeOffset(new DateTime(2026, 8, 13, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "EUR", true, 2147483648L, 5000, 53687091200L, 42949672960L, "Premium", 8000, 365, "premium", 3, 120 },
                    { new Guid("11111111-1111-1111-1111-000000000005"), new DateTimeOffset(new DateTime(2026, 8, 13, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "EUR", true, 2147483648L, 5000, 53687091200L, 42949672960L, "Brzi i žestoki", 5000, 60, "brzi-i-zestoki", 4, 14 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_packages_Slug",
                schema: "billing",
                table: "packages",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "packages",
                schema: "billing");
        }
    }
}

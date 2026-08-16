using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WediFrame.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "purchases",
                schema: "billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    PackageId = table.Column<Guid>(type: "uuid", nullable: false),
                    AmountCents = table.Column<int>(type: "integer", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PaymentProvider = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    PaymentReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    NeedsR1 = table.Column<bool>(type: "boolean", nullable: false),
                    CompanyName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CompanyOib = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CompanyAddress = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    FiscalProvider = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    FiscalInvoiceNumber = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    FiscalJir = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    FiscalZki = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    FiscalizedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FiscalStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchases", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_purchases_EventId",
                schema: "billing",
                table: "purchases",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_purchases_PaymentReference",
                schema: "billing",
                table: "purchases",
                column: "PaymentReference");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "purchases",
                schema: "billing");
        }
    }
}

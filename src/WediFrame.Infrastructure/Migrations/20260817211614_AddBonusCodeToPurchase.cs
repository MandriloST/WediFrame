using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WediFrame.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBonusCodeToPurchase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BonusCodeId",
                schema: "billing",
                table: "purchases",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DiscountCents",
                schema: "billing",
                table: "purchases",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BonusCodeId",
                schema: "billing",
                table: "purchases");

            migrationBuilder.DropColumn(
                name: "DiscountCents",
                schema: "billing",
                table: "purchases");
        }
    }
}

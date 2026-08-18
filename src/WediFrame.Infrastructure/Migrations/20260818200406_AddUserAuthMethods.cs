using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WediFrame.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAuthMethods : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                schema: "identity",
                table: "users",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512);

            migrationBuilder.AddColumn<bool>(
                name: "EmailVerified",
                schema: "identity",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("UPDATE identity.users SET \"EmailVerified\" = true;");

            migrationBuilder.AddColumn<string>(
                name: "GoogleSubjectId",
                schema: "identity",
                table: "users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_GoogleSubjectId",
                schema: "identity",
                table: "users",
                column: "GoogleSubjectId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_GoogleSubjectId",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "EmailVerified",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "GoogleSubjectId",
                schema: "identity",
                table: "users");

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                schema: "identity",
                table: "users",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512,
                oldNullable: true);
        }
    }
}

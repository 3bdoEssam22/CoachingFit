using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachingFit.Identity.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddingUserRoleColumnToUsersTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserRole",
                schema: "identity",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserRole",
                schema: "identity",
                table: "Users");
        }
    }
}

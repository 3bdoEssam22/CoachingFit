using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachingFit.Identity.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class DropUserRoleColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserRole",
                schema: "identity",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserRole",
                schema: "identity",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}

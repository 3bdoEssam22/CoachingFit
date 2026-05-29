using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachingFit.Identity.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCoachRejection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedAt",
                schema: "identity",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                schema: "identity",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RejectedAt",
                schema: "identity",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                schema: "identity",
                table: "Users");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DryCleaning.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "ServiceProviders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "Drivers",
                type: "datetime2",
                nullable: true);
            migrationBuilder.Sql("UPDATE ServiceProviders SET ApprovedAt = CreatedAt WHERE IsActive = 1");
            migrationBuilder.Sql("UPDATE Drivers SET ApprovedAt = CreatedAt WHERE IsActive = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "ServiceProviders");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "Drivers");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DryCleaning.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddConfigurablePricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "VatRate",
                table: "Orders",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 15m);

            migrationBuilder.AddColumn<string>(
                name: "Basis",
                table: "OrderExtraCharges",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsTaxable",
                table: "OrderExtraCharges",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "Percent",
                table: "OrderExtraCharges",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsTaxable",
                table: "ExtraCharges",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "Percent",
                table: "ExtraCharges",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PercentBase",
                table: "ExtraCharges",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "ItemsSubtotal");

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "ExtraCharges",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Fixed");

            migrationBuilder.CreateTable(
                name: "VatSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AppliesToDelivery = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VatSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VatSettings");

            migrationBuilder.DropColumn(
                name: "VatRate",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Basis",
                table: "OrderExtraCharges");

            migrationBuilder.DropColumn(
                name: "IsTaxable",
                table: "OrderExtraCharges");

            migrationBuilder.DropColumn(
                name: "Percent",
                table: "OrderExtraCharges");

            migrationBuilder.DropColumn(
                name: "IsTaxable",
                table: "ExtraCharges");

            migrationBuilder.DropColumn(
                name: "Percent",
                table: "ExtraCharges");

            migrationBuilder.DropColumn(
                name: "PercentBase",
                table: "ExtraCharges");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "ExtraCharges");
        }
    }
}

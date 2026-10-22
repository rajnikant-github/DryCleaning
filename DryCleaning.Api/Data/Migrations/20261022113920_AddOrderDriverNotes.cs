using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DryCleaning.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderDriverNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DriverNotes",
                table: "Orders",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DriverNotes",
                table: "Orders");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LarvaX.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDesignGaps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "RiskZones",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "RiskZones",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "RadiusMetres",
                table: "RiskZones",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<bool>(
                name: "IsRejected",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "RiskZones");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "RiskZones");

            migrationBuilder.DropColumn(
                name: "RadiusMetres",
                table: "RiskZones");

            migrationBuilder.DropColumn(
                name: "IsRejected",
                table: "AspNetUsers");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LarvaX.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDoctorSpecialty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Specialty",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Specialty",
                table: "AspNetUsers");
        }
    }
}

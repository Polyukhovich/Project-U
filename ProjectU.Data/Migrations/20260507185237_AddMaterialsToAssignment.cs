using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectU.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialsToAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowDownload",
                table: "Assignments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MaterialFileName",
                table: "Assignments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MaterialFilePath",
                table: "Assignments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MaterialType",
                table: "Assignments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MaterialUrl",
                table: "Assignments",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowDownload",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "MaterialFileName",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "MaterialFilePath",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "MaterialType",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "MaterialUrl",
                table: "Assignments");
        }
    }
}

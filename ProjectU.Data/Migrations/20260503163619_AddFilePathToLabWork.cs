using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectU.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFilePathToLabWork : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
        name: "FilePath",
        table: "LabWorks",
        type: "nvarchar(max)",
        nullable: false,
        defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "LabWorks",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
          name: "FilePath",
          table: "LabWorks");

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "LabWorks");
        }
    }
}

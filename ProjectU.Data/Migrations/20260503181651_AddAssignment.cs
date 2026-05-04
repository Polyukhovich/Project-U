using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectU.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AssignmentId",
                table: "LabWorks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsGraded",
                table: "LabWorks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Assignment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Deadline = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CourseId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assignment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Assignment_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LabWorks_AssignmentId",
                table: "LabWorks",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Assignment_CourseId",
                table: "Assignment",
                column: "CourseId");
            // Спочатку видаляємо залежні записи
            migrationBuilder.Sql("DELETE FROM PlagiarismResults");
            migrationBuilder.Sql("DELETE FROM Grades WHERE LabWorkId IS NOT NULL");
            migrationBuilder.Sql("DELETE FROM LabWorks");

            migrationBuilder.AddForeignKey(
                name: "FK_LabWorks_Assignment_AssignmentId",
                table: "LabWorks",
                column: "AssignmentId",
                principalTable: "Assignment",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LabWorks_Assignment_AssignmentId",
                table: "LabWorks");

            migrationBuilder.DropTable(
                name: "Assignment");

            migrationBuilder.DropIndex(
                name: "IX_LabWorks_AssignmentId",
                table: "LabWorks");

            migrationBuilder.DropColumn(
                name: "AssignmentId",
                table: "LabWorks");

            migrationBuilder.DropColumn(
                name: "IsGraded",
                table: "LabWorks");
        }
    }
}

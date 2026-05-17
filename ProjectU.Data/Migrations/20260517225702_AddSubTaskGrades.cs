using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectU.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSubTaskGrades : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SubTaskGrades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubTaskId = table.Column<int>(type: "int", nullable: false),
                    LabWorkId = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubTaskGrades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubTaskGrades_LabWorks_LabWorkId",
                        column: x => x.LabWorkId,
                        principalTable: "LabWorks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SubTaskGrades_SubTasks_SubTaskId",
                        column: x => x.SubTaskId,
                        principalTable: "SubTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });
            migrationBuilder.CreateIndex(
                name: "IX_SubTaskGrades_LabWorkId",
                table: "SubTaskGrades",
                column: "LabWorkId");

            migrationBuilder.CreateIndex(
                name: "IX_SubTaskGrades_SubTaskId",
                table: "SubTaskGrades",
                column: "SubTaskId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubTaskGrades");
        }
    }
}

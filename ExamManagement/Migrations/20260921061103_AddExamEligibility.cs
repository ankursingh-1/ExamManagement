using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddExamEligibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExamEligibilities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExamId = table.Column<int>(type: "int", nullable: false),
                    Requires10th = table.Column<bool>(type: "bit", nullable: false),
                    Requires12th = table.Column<bool>(type: "bit", nullable: false),
                    RequiresGraduation = table.Column<bool>(type: "bit", nullable: false),
                    Allow12thPassed = table.Column<bool>(type: "bit", nullable: false),
                    Allow12thAppearing = table.Column<bool>(type: "bit", nullable: false),
                    Allow12thResultAwaited = table.Column<bool>(type: "bit", nullable: false),
                    MinimumPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    RequiresPhysics = table.Column<bool>(type: "bit", nullable: false),
                    RequiresChemistry = table.Column<bool>(type: "bit", nullable: false),
                    RequiresBiology = table.Column<bool>(type: "bit", nullable: false),
                    RequiresMathematics = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamEligibilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamEligibilities_Exams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "Exams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExamEligibilities_ExamId",
                table: "ExamEligibilities",
                column: "ExamId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExamEligibilities");
        }
    }
}

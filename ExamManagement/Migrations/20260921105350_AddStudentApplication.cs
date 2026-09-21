using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentApplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "MinimumPercentage",
                table: "ExamEligibilities",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "StudentApplications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExamId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    FatherName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MotherName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Gender = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    State = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Pincode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Has10thQualification = table.Column<bool>(type: "bit", nullable: false),
                    TenthPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TwelfthStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    TwelfthPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TwelfthBoard = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TwelfthPassingYear = table.Column<int>(type: "int", nullable: true),
                    HasPhysics = table.Column<bool>(type: "bit", nullable: false),
                    HasChemistry = table.Column<bool>(type: "bit", nullable: false),
                    HasBiology = table.Column<bool>(type: "bit", nullable: false),
                    HasMathematics = table.Column<bool>(type: "bit", nullable: false),
                    HasGraduation = table.Column<bool>(type: "bit", nullable: false),
                    GraduationCourse = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    GraduationPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    GraduationPassingYear = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentApplications", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudentApplications");

            migrationBuilder.AlterColumn<decimal>(
                name: "MinimumPercentage",
                table: "ExamEligibilities",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldPrecision: 5,
                oldScale: 2,
                oldNullable: true);
        }
    }
}

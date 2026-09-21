using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailAndMobileToStudentApplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "StudentApplications",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Mobile",
                table: "StudentApplications",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "StudentApplications");

            migrationBuilder.DropColumn(
                name: "Mobile",
                table: "StudentApplications");
        }
    }
}

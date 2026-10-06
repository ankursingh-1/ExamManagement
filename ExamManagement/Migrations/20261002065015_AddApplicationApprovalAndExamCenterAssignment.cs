using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationApprovalAndExamCenterAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "StudentApplications",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExamCenterId",
                table: "StudentApplications",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExamReportingTime",
                table: "StudentApplications",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HallTicketNumber",
                table: "StudentApplications",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "StudentApplications",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "StudentApplications");

            migrationBuilder.DropColumn(
                name: "ExamCenterId",
                table: "StudentApplications");

            migrationBuilder.DropColumn(
                name: "ExamReportingTime",
                table: "StudentApplications");

            migrationBuilder.DropColumn(
                name: "HallTicketNumber",
                table: "StudentApplications");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "StudentApplications");
        }
    }
}

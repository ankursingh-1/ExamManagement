using ExamManagement.Models;

namespace ExamManagement.ViewModels
{
    public class ApplicationManagementViewModel
    {
        public List<StudentApplication> Applications { get; set; }
            = new List<StudentApplication>();

        public List<Exam> Exams { get; set; }
            = new List<Exam>();

        public List<ExamCenter> ExamCenters { get; set; }
            = new List<ExamCenter>();

        public int? ExamId { get; set; }

        public string? Status { get; set; }

        public int? SelectedCenterId { get; set; }

        public string? HallTicketNumber { get; set; }

        public string? ExamReportingTime { get; set; }
    }
}
using ExamManagement.Models;

namespace ExamManagement.ViewModels
{
    public class ResultReportViewModel
    {
        public List<Exam> Exams { get; set; } = new();

        public List<ExamResult> Results { get; set; } = new();

        public int? SelectedExamId { get; set; }

        public string? SelectedCategory { get; set; }

        public string? SelectedStatus { get; set; }

        // Statistics
        public int TotalResults { get; set; }

        public int QualifiedCount { get; set; }

        public int NotQualifiedCount { get; set; }

        public int PendingCount { get; set; }

        public int PublishedCount { get; set; }

        public int UnpublishedCount { get; set; }
    }
}
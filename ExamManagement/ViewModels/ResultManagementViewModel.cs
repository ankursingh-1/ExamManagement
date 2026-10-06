using ExamManagement.Models;

namespace ExamManagement.ViewModels
{
    public class ResultManagementViewModel
    {
        public List<Exam> Exams { get; set; } = new();

        public int? SelectedExamId { get; set; }

        public Exam? SelectedExam { get; set; }

        public List<ExamCutoff> Cutoffs { get; set; } = new();

        public List<StudentApplication> Applications { get; set; } = new();

        public List<ExamResult> Results { get; set; } = new();

        public List<ExamResultSubject> ResultSubjects { get; set; } = new();

        public string? SelectedCategory { get; set; }
    }
}
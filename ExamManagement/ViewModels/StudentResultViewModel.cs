using ExamManagement.Models;

namespace ExamManagement.ViewModels
{
    public class StudentResultViewModel
    {
        public StudentApplication Application { get; set; } = null!;
        public Exam Exam { get; set; } = null!;
        public ExamResult Result { get; set; } = null!;
        public List<ExamResultSubject> Subjects { get; set; } = new();
        public ExamCutoff? Cutoff { get; set; }
    }
}
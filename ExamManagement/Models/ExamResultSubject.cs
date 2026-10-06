using System.ComponentModel.DataAnnotations;

namespace ExamManagement.Models
{
    public class ExamResultSubject
    {
        public int Id { get; set; }

        [Required]
        public int ExamResultId { get; set; }

        public ExamResult? ExamResult { get; set; }

        [Required]
        [StringLength(100)]
        public string SubjectName { get; set; } = string.Empty;

        [Range(0, 100000)]
        public decimal TotalMarks { get; set; }

        [Range(0, 100000)]
        public decimal ObtainedMarks { get; set; }

        public decimal Percentage { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
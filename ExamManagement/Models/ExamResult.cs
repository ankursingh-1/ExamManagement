using System.ComponentModel.DataAnnotations;

namespace ExamManagement.Models
{
    public class ExamResult
    {
        public int Id { get; set; }

        [Required]
        public int StudentApplicationId { get; set; }
        public StudentApplication? StudentApplication { get; set; }

        [Required]
        public int ExamId { get; set; }

        public Exam? Exam { get; set; }

        [Range(0, 100000)]
        public decimal TotalMarks { get; set; }

        [Range(0, 100000)]
        public decimal ObtainedMarks { get; set; }

        [Range(0, 100)]
        public decimal Percentage { get; set; }

        [StringLength(30)]
        public string ResultStatus { get; set; } = "Pending";

        [StringLength(30)]
        public string CutoffStatus { get; set; } = "Pending";

        public bool IsPublished { get; set; } = false;

        public DateTime? PublishedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
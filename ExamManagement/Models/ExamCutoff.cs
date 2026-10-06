using System.ComponentModel.DataAnnotations;

namespace ExamManagement.Models
{
    public class ExamCutoff
    {
        public int Id { get; set; }

        [Required]
        public int ExamId { get; set; }

        public Exam? Exam { get; set; }

        [Required]
        [StringLength(50)]
        public string Category { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string CutoffType { get; set; } = "Percentage";

        [Range(0, 100)]
        public decimal CutoffValue { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
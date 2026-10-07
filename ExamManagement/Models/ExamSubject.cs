using System.ComponentModel.DataAnnotations;

namespace ExamManagement.Models
{
    public class ExamSubject
    {
        public int Id { get; set; }

        [Required]
        public int ExamId { get; set; }

        public Exam? Exam { get; set; }

        [Required]
        [StringLength(100)]
        public string SubjectName { get; set; } = string.Empty;

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
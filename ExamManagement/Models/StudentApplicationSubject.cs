using System.ComponentModel.DataAnnotations;

namespace ExamManagement.Models
{
    public class StudentApplicationSubject
    {
        public int Id { get; set; }

        [Required]
        public int StudentApplicationId { get; set; }

        public StudentApplication? StudentApplication { get; set; }

        [Required]
        public int ExamSubjectId { get; set; }

        public ExamSubject? ExamSubject { get; set; }

        [Required]
        [StringLength(100)]
        public string SubjectName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
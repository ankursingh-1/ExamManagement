using System.ComponentModel.DataAnnotations;

namespace ExamManagement.Models
{
    public class StudentApplicationDocument
    {
        public int Id { get; set; }

        [Required]
        public int StudentApplicationId { get; set; }

        [Required]
        [StringLength(50)]
        public string DocumentType { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string FilePath { get; set; } = string.Empty;

        [StringLength(100)]
        public string? ContentType { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public StudentApplication? StudentApplication { get; set; }
    }
}
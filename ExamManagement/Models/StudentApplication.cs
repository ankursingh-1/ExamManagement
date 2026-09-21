using System.ComponentModel.DataAnnotations;

namespace ExamManagement.Models
{
    public class StudentApplication
    {
        public int Id { get; set; }

        // Unique application number
        [Required]
        [StringLength(50)]
        public string ApplicationNumber { get; set; } = string.Empty;

        // Identity User
        [Required]
        public string UserId { get; set; } = string.Empty;

        // Applied Exam
        [Required]
        public int ExamId { get; set; }

        // Application status
        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Draft";

        // Personal Information
        [Required]
        [StringLength(150)]
        public string FullName { get; set; } = string.Empty;

        [EmailAddress]
        [StringLength(150)]
        public string? Email { get; set; }

        [Phone]
        [StringLength(20)]
        public string? Mobile { get; set; }

        [StringLength(100)]
        public string? FatherName { get; set; }

        [StringLength(100)]
        public string? MotherName { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [StringLength(20)]
        public string? Gender { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(100)]
        public string? State { get; set; }

        [StringLength(10)]
        public string? Pincode { get; set; }

        // 10th Education
        public bool Has10thQualification { get; set; }

        [Range(0, 100)]
        public decimal? TenthPercentage { get; set; }

        // 12th Education
        [StringLength(30)]
        public string? TwelfthStatus { get; set; }

        [Range(0, 100)]
        public decimal? TwelfthPercentage { get; set; }

        [StringLength(100)]
        public string? TwelfthBoard { get; set; }

        public int? TwelfthPassingYear { get; set; }

        // Subjects
        public bool HasPhysics { get; set; }

        public bool HasChemistry { get; set; }

        public bool HasBiology { get; set; }

        public bool HasMathematics { get; set; }

        // Graduation
        public bool HasGraduation { get; set; }

        [StringLength(150)]
        public string? GraduationCourse { get; set; }

        [Range(0, 100)]
        public decimal? GraduationPercentage { get; set; }

        public int? GraduationPassingYear { get; set; }

        // Application timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
using System.ComponentModel.DataAnnotations;

namespace ExamManagement.Models
{
    public class Exam
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Exam name is required.")]
        [StringLength(200)]
        [Display(Name = "Exam Name")]
        public string ExamName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Exam code is required.")]
        [StringLength(50)]
        [Display(Name = "Exam Code")]
        public string ExamCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Exam type is required.")]
        [StringLength(100)]
        [Display(Name = "Exam Type")]
        public string ExamType { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "Application Start Date")]
        public DateTime ApplicationStartDate { get; set; }

        [Required]
        [Display(Name = "Application End Date")]
        public DateTime ApplicationEndDate { get; set; }

        [Required]
        [Display(Name = "Exam Date")]
        public DateTime ExamDate { get; set; }

        [Required]
        [Display(Name = "Start Time")]
        public TimeSpan StartTime { get; set; }

        [Required]
        [Display(Name = "End Time")]
        public TimeSpan EndTime { get; set; }

        [Range(0, 1000000,
            ErrorMessage = "Fee must be between 0 and 1,000,000.")]
        [Display(Name = "Application Fee")]
        public decimal ApplicationFee { get; set; }

        [StringLength(2000)]
        public string? Eligibility { get; set; }

        [StringLength(5000)]
        public string? Instructions { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        // Eligibility Configuration
        public ExamEligibility? EligibilityConfiguration { get; set; }
    }
}
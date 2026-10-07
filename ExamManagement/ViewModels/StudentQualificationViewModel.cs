using System.ComponentModel.DataAnnotations;

namespace ExamManagement.ViewModels
{
    public class StudentQualificationViewModel : IValidatableObject
    {
        public int ApplicationId { get; set; }
        public int ExamId { get; set; }

        // 10th
        [Required(ErrorMessage = "Please select 10th qualification.")]
        [Display(Name = "10th Qualification")]
        public bool? Has10thQualification { get; set; }

        [Required(ErrorMessage = "Please enter 10th percentage.")]
        [Range(0.01, 100, ErrorMessage = "10th percentage must be between 0 and 100.")]
        [Display(Name = "10th Percentage")]
        public decimal? TenthPercentage { get; set; }

        // 12th
        [Required(ErrorMessage = "Please select 12th status.")]
        [StringLength(30)]
        [Display(Name = "12th Status")]
        public string? TwelfthStatus { get; set; }

        // IMPORTANT:
        // No [Required] here.
        // Required only when status = Passed.
        [Range(0, 100, ErrorMessage = "12th percentage must be between 0 and 100.")]
        [Display(Name = "12th Percentage")]
        public decimal? TwelfthPercentage { get; set; }

        [Required(ErrorMessage = "Please enter 12th board.")]
        [StringLength(100)]
        [Display(Name = "12th Board")]
        public string? TwelfthBoard { get; set; }

        // IMPORTANT:
        // No [Required] here.
        // Required only when status = Passed.
        [Range(1900, 2100, ErrorMessage = "Please enter a valid passing year.")]
        [Display(Name = "12th Passing Year")]
        public int? TwelfthPassingYear { get; set; }

        // Subjects
        // Dynamic Exam Subjects
        public List<int> SelectedSubjectIds { get; set; } = new();

        [Display(Name = "Physics")]
        public bool HasPhysics { get; set; }
        [Display(Name = "Chemistry")]
        public bool HasChemistry { get; set; }
        [Display(Name = "Biology")]
        public bool HasBiology { get; set; }
        [Display(Name = "Mathematics")]
        public bool HasMathematics { get; set; }

        // Graduation
        [Display(Name = "Graduation")]
        public bool HasGraduation { get; set; }
        [StringLength(150)]
        [Display(Name = "Graduation Course")]
        public string? GraduationCourse { get; set; }
        [Range(0, 100, ErrorMessage = "Graduation percentage must be between 0 and 100.")]
        [Display(Name = "Graduation Percentage")]
        public decimal? GraduationPercentage { get; set; }

        [Range(1900, 2100, ErrorMessage = "Please enter a valid passing year.")]
        [Display(Name = "Graduation Passing Year")]
        public int? GraduationPassingYear { get; set; }

        // CUSTOM VALIDATION
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Graduation validation
            if (HasGraduation)
            {
                if (string.IsNullOrWhiteSpace(GraduationCourse))
                {
                    yield return new ValidationResult("Please enter graduation course.",
                        new[]
                        {nameof(GraduationCourse)});
                }

                if (!GraduationPercentage.HasValue)
                {
                    yield return new ValidationResult("Please enter graduation percentage.",
                        new[]
                        {nameof(GraduationPercentage)});
                }

                if (!GraduationPassingYear.HasValue)
                {
                    yield return new ValidationResult("Please enter graduation passing year.",
                        new[]
                        {nameof(GraduationPassingYear)});
                }
            }
        }
    }
}
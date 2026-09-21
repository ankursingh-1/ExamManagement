using System.ComponentModel.DataAnnotations;

namespace ExamManagement.Models
{
    public class ExamEligibility
    {
        public int Id { get; set; }

        // Which exam this eligibility belongs to
        public int ExamId { get; set; }

        public Exam? Exam { get; set; }

        // Qualification
        public bool Requires10th { get; set; }

        public bool Requires12th { get; set; }

        public bool RequiresGraduation { get; set; }

        // 12th qualification status
        public bool Allow12thPassed { get; set; }

        public bool Allow12thAppearing { get; set; }

        public bool Allow12thResultAwaited { get; set; }

        // Minimum percentage
        [Range(0, 100)]
        public decimal? MinimumPercentage { get; set; }

        // Subjects
        public bool RequiresPhysics { get; set; }

        public bool RequiresChemistry { get; set; }

        public bool RequiresBiology { get; set; }

        public bool RequiresMathematics { get; set; }
    }
}
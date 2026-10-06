using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExamManagement.Models
{
    public class StudentApplicationPayment
    {
        public int Id { get; set; }

        [Required]
        public int StudentApplicationId { get; set; }

        [Required]
        [StringLength(100)]
        public string OrderId { get; set; } = string.Empty;

        [StringLength(100)]
        public string? PaymentId { get; set; }

        [StringLength(500)]
        public string? Signature { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Pending";

        [StringLength(50)]
        public string? PaymentMethod { get; set; }

        public DateTime? PaidAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public StudentApplication? StudentApplication { get; set; }
    }
}
using System.ComponentModel.DataAnnotations;

namespace ExamManagement.Models
{
    public class ExamCenter
    {
        public int Id { get; set; }

        [Required]
        public int ExamId { get; set; }

        [Required]
        [StringLength(200)]
        public string CenterName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Address { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string State { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string Pincode { get; set; } = string.Empty;

        [Range(1, 100000)]
        public int Capacity { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
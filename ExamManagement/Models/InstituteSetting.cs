using System.ComponentModel.DataAnnotations;

namespace ExamManagement.Models
{
    public class InstituteSetting
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string InstituteName { get; set; } = string.Empty;

        [StringLength(50)]
        public string? InstituteCode { get; set; }

        [StringLength(500)]
        public string? LogoPath { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(100)]
        public string? State { get; set; }

        [StringLength(10)]
        public string? PinCode { get; set; }

        [Phone]
        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [EmailAddress]
        [StringLength(150)]
        public string? EmailAddress { get; set; }

        [Url]
        [StringLength(250)]
        public string? Website { get; set; }

        [EmailAddress]
        [StringLength(150)]
        public string? SupportEmail { get; set; }

        [Phone]
        [StringLength(20)]
        public string? SupportPhone { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}
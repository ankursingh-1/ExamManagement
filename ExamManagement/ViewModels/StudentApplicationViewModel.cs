using System.ComponentModel.DataAnnotations;

namespace ExamManagement.ViewModels
{
    public class StudentApplicationViewModel
    {
        public int ExamId { get; set; }

        [Required]
        [StringLength(150)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(150)]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Phone]
        [StringLength(20)]
        [Display(Name = "Mobile")]
        public string Mobile { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Gender")]
        public string Gender { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Father's Name")]
        public string FatherName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Mother's Name")]
        public string MotherName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        [Display(Name = "Address")]
        public string Address { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "State")]
        public string State { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "City")]
        public string City { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        [RegularExpression(@"^[0-9]{6}$", ErrorMessage = "PIN Code must be 6 digits.")]
        [Display(Name = "PIN Code")]
        public string Pincode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Aadhaar number is required.")]
        [RegularExpression(@"^\d{12}$", ErrorMessage = "Aadhaar number must be exactly 12 digits.")]
        [Display(Name = "Aadhaar Number")]
        public string AadhaarNumber { get; set; } = string.Empty;
    }
}
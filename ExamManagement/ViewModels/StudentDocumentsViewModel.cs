using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ExamManagement.ViewModels
{
    public class StudentDocumentsViewModel
    {

        public int ApplicationId { get; set; }

        public int ExamId { get; set; }

        [Required(ErrorMessage = "Please upload passport size photo.")]
        [Display(Name = "Passport Size Photo")]
        public IFormFile? Photo { get; set; }


        [Required(ErrorMessage = "Please upload signature.")]
        [Display(Name = "Signature")]
        public IFormFile? Signature { get; set; }

        [Required(ErrorMessage = "Please upload 10th marksheet.")]
        [Display(Name = "10th Marksheet")]
        public IFormFile? TenthMarksheet { get; set; }

        [Display(Name = "12th Marksheet")]
        public IFormFile? TwelfthMarksheet { get; set; }

        // OPTIONAL
        [Display(Name = "Graduation Certificate / Marksheet")]
        public IFormFile? GraduationDocument { get; set; }

        [Required(ErrorMessage = "Please upload Aadhaar Card.")]
        [Display(Name = "Aadhaar Card")]
        public IFormFile? AadhaarCard { get; set; }
    }
}
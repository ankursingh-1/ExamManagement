using Razorpay.Api;
using ExamManagement.Data;
using ExamManagement.Models;
using ExamManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamManagement.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentApplicationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IConfiguration _configuration;

        public StudentApplicationController(ApplicationDbContext context,
        UserManager<IdentityUser> userManager, IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _configuration = configuration;
        }

        // GET: /StudentApplication/Create?examId=5
        [HttpGet]
        public async Task<IActionResult> Create(int examId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }
            var exam = await _context.Exams
                .FirstOrDefaultAsync(x =>
                    x.Id == examId &&
                    x.IsActive);

            if (exam == null)
            {
                return NotFound("The selected examination is not available.");
            }

            // Check application deadline only for new applications
            if (exam.ApplicationEndDate.Date < DateTime.Today)
            {
                return BadRequest("The application deadline for this examination has passed.");
            }

            var existingApplication = await _context.StudentApplications
                .FirstOrDefaultAsync(x =>
                    x.UserId == user.Id &&
                    x.ExamId == examId);

            // NEW APPLICATION
            if (existingApplication == null)
            {
                var studentProfile = await _context.StudentProfiles
                    .FirstOrDefaultAsync(x => x.UserId == user.Id);

                var model = new StudentApplicationViewModel
                {
                    ExamId = examId,
                    FullName = studentProfile?.FullName ?? string.Empty,
                    Mobile = studentProfile?.Mobile
                             ?? user.PhoneNumber
                             ?? string.Empty,
                    Email = user.Email ?? string.Empty
                };

                ViewBag.ExamName = exam.ExamName;
                return View(model);
            }

            // APPROVED APPLICATION
            if (existingApplication.Status == "Approved")
            {
                return RedirectToAction(nameof(HallTicket),new
                    {
                        applicationId = existingApplication.Id
                    });
            }

            // SUBMITTED APPLICATION
            if (existingApplication.Status == "Submitted")
            {
                return RedirectToAction(nameof(ApplicationDetails),new
                    {
                        applicationId = existingApplication.Id
                    });
            }

            // PAYMENT CHECK
            var paidPayment = await _context.StudentApplicationPayments
                .FirstOrDefaultAsync(x =>
                    x.StudentApplicationId == existingApplication.Id &&
                    x.Status == "Paid");

            if (paidPayment != null)
            {
                return RedirectToAction(nameof(Review),new
                    {
                        applicationId = existingApplication.Id
                    });
            }

            // DOCUMENT CHECK
            var documents = await _context.StudentApplicationDocuments
                .Where(x =>
                    x.StudentApplicationId == existingApplication.Id)
                .ToListAsync();
            var requiredDocumentTypes = new List<string>
            {
                "Photo",
                "Signature",
                "10thMarksheet",
                "AadhaarCard"
            };

            if (string.Equals(existingApplication.TwelfthStatus,"Passed",StringComparison.OrdinalIgnoreCase))
            {
                requiredDocumentTypes.Add("12thMarksheet");
            }

            if (existingApplication.HasGraduation)
            {
                requiredDocumentTypes.Add("GraduationDocument");
            }          

            var hasAllRequiredDocuments = requiredDocumentTypes
                .All(type => documents.Any(x => x.DocumentType == type));

            if (hasAllRequiredDocuments)
            {
                return RedirectToAction(nameof(Payment),new
                    {
                        applicationId = existingApplication.Id
                    });
            }

            // CONTINUE DOCUMENTS
            return RedirectToAction(nameof(Documents),new
                {
                    applicationId = existingApplication.Id
                });
        }

        // POST: /StudentApplication/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StudentApplicationViewModel model)
        {
            var exam = await _context.Exams
                .FirstOrDefaultAsync(x =>
                    x.Id == model.ExamId &&
                    x.IsActive);

            if (exam == null)
            {
                return NotFound("The selected examination is not available.");
            }

            if (exam.ApplicationEndDate.Date < DateTime.Today)
            {
                return BadRequest("The application deadline for this examination has passed.");
            }

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            if (!ModelState.IsValid)
            {
                ViewBag.ExamName = exam.ExamName;
                return View(model);
            }

            // Prevent duplicate application for the same exam.
            var existingApplication = await _context.StudentApplications
                .FirstOrDefaultAsync(x =>
                    x.UserId == user.Id &&
                    x.ExamId == model.ExamId);

            if (existingApplication != null)
            {
                return RedirectToAction("Create", "StudentApplication", new { examId = model.ExamId });
            }

            var application = new StudentApplication
            {
                ApplicationNumber = string.Empty,
                UserId = user.Id,
                ExamId = model.ExamId,
                Status = "Draft",
                FullName = model.FullName,
                Email = model.Email,
                Mobile = model.Mobile,
                FatherName = model.FatherName,
                MotherName = model.MotherName,
                DateOfBirth = model.DateOfBirth,
                Gender = model.Gender,
                Address = model.Address,
                State = model.State,
                City = model.City,
                Pincode = model.Pincode,
                AadhaarNumber = model.AadhaarNumber,
                CreatedAt = DateTime.UtcNow
            };

            _context.StudentApplications.Add(application);
            // First save - database generates Id
            await _context.SaveChangesAsync();
            // Generate permanent application number
            application.ApplicationNumber = $"APP-{DateTime.UtcNow:yyyy}-{application.Id:D6}";
            // Save application number
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Basic details saved successfully.";
            return RedirectToAction(nameof(Qualification),new
                {
                    applicationId = application.Id
                });
        }

        // EDIT APPLICATION - GET
        [HttpGet]
        public async Task<IActionResult> Edit(int applicationId)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            var application = await _context.StudentApplications
                .FirstOrDefaultAsync(x =>
                    x.Id == applicationId &&
                    x.UserId == user.Id);

            if (application == null)
            {
                return NotFound("Application not found.");
            }

            // Only Draft applications can be edited.
            if (application.Status != "Draft")
            {
                TempData["ErrorMessage"] = "This application can no longer be edited.";

                return RedirectToAction(nameof(Review),new { applicationId = application.Id });
            }

            var exam = await _context.Exams
                .FirstOrDefaultAsync(x =>
                    x.Id == application.ExamId &&
                    x.IsActive);

            if (exam == null)
            {
                return NotFound("The selected examination is not available.");
            }

            var model = new StudentApplicationViewModel
            {
                ExamId = application.ExamId,
                FullName = application.FullName ?? string.Empty,
                Email = application.Email ?? string.Empty,
                Mobile = application.Mobile ?? string.Empty,
                FatherName = application.FatherName ?? string.Empty,
                MotherName = application.MotherName ?? string.Empty,
                DateOfBirth = application.DateOfBirth,
                Gender = application.Gender ?? string.Empty,
                Address = application.Address ?? string.Empty,
                State = application.State ?? string.Empty,
                City = application.City ?? string.Empty,
                Pincode = application.Pincode ?? string.Empty,
                AadhaarNumber = application.AadhaarNumber ?? string.Empty
            };
            ViewBag.ExamName = exam.ExamName;
            return View(model);
        }

        // EDIT APPLICATION - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int applicationId,StudentApplicationViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var application = await _context.StudentApplications
                .FirstOrDefaultAsync(x =>
                    x.Id == applicationId &&
                    x.UserId == user.Id);

            if (application == null)
            {
                return NotFound("Application not found.");
            }

            // Only Draft applications can be edited.
            if (application.Status != "Draft")
            {
                TempData["ErrorMessage"] = "This application can no longer be edited.";
                return RedirectToAction(nameof(Review),new { applicationId = application.Id });
            }

            var exam = await _context.Exams
                .FirstOrDefaultAsync(x =>
                    x.Id == application.ExamId &&
                    x.IsActive);

            if (exam == null)
            {
                return NotFound("The selected examination is not available.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.ExamName = exam.ExamName;
                return View(model);
            }
            application.FullName = model.FullName;
            application.Email = model.Email;
            application.Mobile = model.Mobile;
            application.FatherName = model.FatherName;
            application.MotherName = model.MotherName;
            application.DateOfBirth = model.DateOfBirth;
            application.Gender = model.Gender;
            application.Address = model.Address;
            application.State = model.State;
            application.City = model.City;
            application.Pincode = model.Pincode;
            application.AadhaarNumber = model.AadhaarNumber;
            application.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Basic details updated successfully.";
            return RedirectToAction(nameof(Review), new { applicationId = application.Id });
        }

        // QUALIFICATION - GET
        [HttpGet]
        public async Task<IActionResult> Qualification(int applicationId)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            var application = await _context.StudentApplications
                .FirstOrDefaultAsync(x =>
                    x.Id == applicationId &&
                    x.UserId == user.Id);

            if (application == null)
            {
                return NotFound("Application not found.");
            }

            var exam = await _context.Exams
                .Include(x => x.Subjects)
                .FirstOrDefaultAsync(x =>
                    x.Id == application.ExamId &&
                    x.IsActive);

            if (exam == null)
            {
                return NotFound("The selected examination is not available.");
            }

            // LOAD STUDENT SELECTED SUBJECTS
            var selectedSubjectIds = await _context.StudentApplicationSubjects
                .Where(x => x.StudentApplicationId == application.Id)
                .Select(x => x.ExamSubjectId)
                .ToListAsync();

            // FALLBACK FOR OLD APPLICATIONS
            // If dynamic subject records do not exist yet,
            // use the old fixed subject fields.
            if (!selectedSubjectIds.Any())
            {
                selectedSubjectIds = exam.Subjects
                    .Where(x => x.IsActive)
                    .Where(x =>
                        (x.SubjectName.Equals(
                            "Physics",
                            StringComparison.OrdinalIgnoreCase)
                            && application.HasPhysics)
                        ||
                        (x.SubjectName.Equals(
                            "Chemistry",
                            StringComparison.OrdinalIgnoreCase)
                            && application.HasChemistry)
                        ||
                        (x.SubjectName.Equals(
                            "Biology",
                            StringComparison.OrdinalIgnoreCase)
                            && application.HasBiology)
                        ||
                        (x.SubjectName.Equals(
                            "Mathematics",
                            StringComparison.OrdinalIgnoreCase)
                            && application.HasMathematics))
                    .Select(x => x.Id)
                    .ToList();
            }

            var model = new StudentQualificationViewModel
            {
                ApplicationId = application.Id,
                ExamId = application.ExamId,

                Has10thQualification = application.Has10thQualification,
                TenthPercentage = application.TenthPercentage,

                TwelfthStatus = application.TwelfthStatus,
                TwelfthPercentage = application.TwelfthPercentage,
                TwelfthBoard = application.TwelfthBoard,
                TwelfthPassingYear = application.TwelfthPassingYear,

                // Dynamic subjects
                SelectedSubjectIds = selectedSubjectIds,

                // Legacy fields
                HasPhysics = application.HasPhysics,
                HasChemistry = application.HasChemistry,
                HasBiology = application.HasBiology,
                HasMathematics = application.HasMathematics,

                HasGraduation = application.HasGraduation,
                GraduationCourse = application.GraduationCourse,
                GraduationPercentage = application.GraduationPercentage,
                GraduationPassingYear = application.GraduationPassingYear
            };

            ViewBag.ExamSubjects = exam.Subjects
                .Where(x => x.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .ToList();

            ViewBag.ExamName = exam.ExamName;

            return View(model);
        }

        // QUALIFICATION - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Qualification(StudentQualificationViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            // GET APPLICATION - USER SPECIFIC
            var application = await _context.StudentApplications
                .FirstOrDefaultAsync(x => x.Id == model.ApplicationId && x.UserId == user.Id);

            if (application == null)
            {
                return NotFound("Application not found.");
            }

            // GET EXAM
            var exam = await _context.Exams .FirstOrDefaultAsync(x => x.Id == application.ExamId && x.IsActive);
            if (exam == null)
            {
                return NotFound("The selected examination is not available.");
            }

            // LOAD ACTIVE EXAM SUBJECTS
            var examSubjects = await _context.ExamSubjects
                .Where(x => x.ExamId == application.ExamId && x.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();

            // 12th CONDITIONAL VALIDATION
            // Remove automatic validation errors for
            // 12th percentage and passing year.
            ModelState.Remove(nameof(model.TwelfthPercentage));
            ModelState.Remove(nameof(model.TwelfthPassingYear));

            // Required only when 12th status is Passed.
            if (string.Equals(model.TwelfthStatus,"Passed",StringComparison.OrdinalIgnoreCase))
            {
                if (!model.TwelfthPercentage.HasValue)
                {
                    ModelState.AddModelError(nameof(model.TwelfthPercentage),
                        "Please enter 12th percentage.");
                }

                if (!model.TwelfthPassingYear.HasValue)
                {
                    ModelState.AddModelError(nameof(model.TwelfthPassingYear),
                        "Please enter 12th passing year.");
                }
            }

            // DYNAMIC SUBJECT IDs
            var selectedSubjectIds = model.SelectedSubjectIds
              .Where(x => x > 0)
              .Distinct()
              .ToList();

            // VALIDATE SELECTED SUBJECTS
            var validSubjectIds = examSubjects
                .Select(x => x.Id)
                .ToHashSet();

            var invalidSubjectIds = selectedSubjectIds
                .Where(x => !validSubjectIds.Contains(x))
                .ToList();

            if (invalidSubjectIds.Any())
            {
                ModelState.AddModelError("","One or more selected subjects are invalid.");
            }

            // MODEL VALIDATION
            if (!ModelState.IsValid)
            {
                ViewBag.ExamName = exam.ExamName;
                ViewBag.ExamSubjects = examSubjects;

                return View(model);
            }

            // 10th
            application.Has10thQualification = model.Has10thQualification ?? false;
            application.TenthPercentage = model.TenthPercentage;

            // 12th
            application.TwelfthStatus = model.TwelfthStatus;
            application.TwelfthPercentage = model.TwelfthPercentage;
            application.TwelfthBoard = model.TwelfthBoard;
            application.TwelfthPassingYear = model.TwelfthPassingYear;

            // DYNAMIC SUBJECTS

            // Remove previous subject selections
            var existingStudentSubjects = await _context.StudentApplicationSubjects
                .Where(x => x.StudentApplicationId == application.Id)
                .ToListAsync();

            if (existingStudentSubjects.Any())
            {
                _context.StudentApplicationSubjects.RemoveRange(existingStudentSubjects);
            }

            // Save selected subjects
            foreach (var subject in examSubjects
                .Where(x => selectedSubjectIds.Contains(x.Id)))
            {
                _context.StudentApplicationSubjects.Add(
                    new StudentApplicationSubject
                    {
                        StudentApplicationId = application.Id,
                        ExamSubjectId = subject.Id,
                        SubjectName = subject.SubjectName,
                        CreatedAt = DateTime.UtcNow
                    });
            }

            // SYNC OLD FIXED SUBJECT FIELDS
            application.HasPhysics = examSubjects.Any(x =>
                selectedSubjectIds.Contains(x.Id) &&
                x.SubjectName.Equals(
                    "Physics",
                    StringComparison.OrdinalIgnoreCase));

            application.HasChemistry = examSubjects.Any(x =>
                selectedSubjectIds.Contains(x.Id) &&
                x.SubjectName.Equals(
                    "Chemistry",
                    StringComparison.OrdinalIgnoreCase));

            application.HasBiology = examSubjects.Any(x =>
                selectedSubjectIds.Contains(x.Id) &&
                x.SubjectName.Equals(
                    "Biology",
                    StringComparison.OrdinalIgnoreCase));

            application.HasMathematics = examSubjects.Any(x =>
                selectedSubjectIds.Contains(x.Id) &&
                x.SubjectName.Equals(
                    "Mathematics",
                    StringComparison.OrdinalIgnoreCase));

            // GRADUATION
            application.HasGraduation = model.HasGraduation;
            application.GraduationCourse = model.GraduationCourse;
            application.GraduationPercentage = model.GraduationPercentage;
            application.GraduationPassingYear = model.GraduationPassingYear;

            // UPDATE TIMESTAMP
            application.UpdatedAt = DateTime.UtcNow;

            // SAVE
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Qualification details saved successfully.";

            return RedirectToAction(
                nameof(Documents),
                new
                {
                    applicationId = application.Id
                });
        }

        // DOCUMENTS - GET
        [HttpGet]
        public async Task<IActionResult> Documents(int applicationId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var application = await _context.StudentApplications
                .FirstOrDefaultAsync(x =>
                    x.Id == applicationId &&
                    x.UserId == user.Id);

            if (application == null)
            {
                return NotFound("Application not found.");
            }

            // Already approved → Hall Ticket
            if (application.Status == "Approved")
            {
                return RedirectToAction(nameof(HallTicket),new
                    {
                        applicationId = application.Id
                    });
            }

            // Already submitted → Confirmation
            if (application.Status == "Submitted")
            {
                return RedirectToAction(nameof(Confirmation),new
                    {
                        applicationId = application.Id
                    });
            }

            var exam = await _context.Exams
                .Include(x => x.Subjects)
                .FirstOrDefaultAsync(x => x.Id == application.ExamId && x.IsActive);

            if (exam == null)
            {
                return NotFound("The selected examination is not available.");
            }

            // CHECK EXISTING PAYMENT
            //var paidPayment = await _context.StudentApplicationPayments
            //    .FirstOrDefaultAsync(x =>
            //        x.StudentApplicationId == application.Id &&
            //        x.Status == "Paid");

            //if (paidPayment != null)
            //{
            //    return RedirectToAction(nameof(Review),new
            //        {
            //            applicationId = application.Id
            //        });
            //}

            // CHECK EXISTING DOCUMENTS
            var existingDocuments = await _context.StudentApplicationDocuments
                .Where(x =>
                    x.StudentApplicationId == application.Id)
                .ToListAsync();

            var requiredDocumentTypes = new List<string>
            {
                "Photo",
                "Signature",
                "10thMarksheet",
                "AadhaarCard"
            };

            if (string.Equals(application.TwelfthStatus,"Passed",StringComparison.OrdinalIgnoreCase))
            {
                requiredDocumentTypes.Add("12thMarksheet");
            }

            if (application.HasGraduation)
            {
                requiredDocumentTypes.Add("GraduationDocument");
            }

            var hasAllRequiredDocuments = requiredDocumentTypes
                .All(type => existingDocuments.Any(x =>  x.DocumentType == type));

            // Documents already complete → Payment
            //if (hasAllRequiredDocuments)
            //{
            //    return RedirectToAction(nameof(Payment),new
            //        {
            //            applicationId = application.Id
            //        });
            //}

            // SHOW DOCUMENT PAGE
            var model = new StudentDocumentsViewModel
            {
                ApplicationId = application.Id,
                ExamId = application.ExamId
            };
            ViewBag.ExamSubjects = exam.Subjects
             .Where(x => x.IsActive)
             .OrderBy(x => x.DisplayOrder)
             .ToList();
            ViewBag.ExamName = exam.ExamName;
            ViewBag.ExistingDocuments = existingDocuments;
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Document(int documentId)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            var document = await _context.StudentApplicationDocuments
                .Join(_context.StudentApplications,document => document.StudentApplicationId,
                    application => application.Id,(document, application) => new
                    {
                        Document = document,
                        Application = application
                    })
                .FirstOrDefaultAsync(x =>
                    x.Document.Id == documentId &&
                    x.Application.UserId == user.Id);

            if (document == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(document.Document.FilePath))
            {
                return NotFound();
            }

            var relativePath = document.Document.FilePath
                .TrimStart('/')
                .Replace('/', Path.DirectorySeparatorChar);
            var webRootPath = Path.Combine(Directory.GetCurrentDirectory(),"wwwroot");
            var filePath = Path.GetFullPath(Path.Combine(webRootPath, relativePath));
            var uploadRoot = Path.GetFullPath(Path.Combine(webRootPath,"uploads","applications"));

            if (!filePath.StartsWith(uploadRoot + Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))
            {
                return NotFound();
            }

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            var contentType = string.IsNullOrWhiteSpace(document.Document.ContentType)
                ? "application/octet-stream"
                : document.Document.ContentType;
            return PhysicalFile(filePath,contentType,enableRangeProcessing: true);
        }

        // DOCUMENTS - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Documents(StudentDocumentsViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            var application = await _context.StudentApplications
                .FirstOrDefaultAsync(x =>
                    x.Id == model.ApplicationId &&
                    x.UserId == user.Id);

            if (application == null)
            {
                return NotFound("Application not found.");
            }

            var exam = await _context.Exams
                .FirstOrDefaultAsync(x =>
                    x.Id == application.ExamId &&
                    x.IsActive);

            if (exam == null)
            {
                return NotFound("The selected examination is not available.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.ExamName = exam.ExamName;
                ViewBag.ExistingDocuments = await _context.StudentApplicationDocuments
                        .Where(x => x.StudentApplicationId == application.Id)
                        .ToListAsync();
                return View(model);
            }

            var uploadFolder = Path.Combine(
            Directory.GetCurrentDirectory(), "wwwroot", "uploads", "applications", application.Id.ToString());
            Directory.CreateDirectory(uploadFolder);
            await SaveDocument(model.Photo, "Photo", application.Id, uploadFolder);
            await SaveDocument(model.Signature, "Signature", application.Id, uploadFolder);
            await SaveDocument(model.TenthMarksheet, "10thMarksheet", application.Id, uploadFolder);
            await SaveDocument(model.TwelfthMarksheet, "12thMarksheet", application.Id, uploadFolder);
            await SaveDocument(model.GraduationDocument, "GraduationDocument", application.Id, uploadFolder);
            await SaveDocument(model.AadhaarCard, "AadhaarCard", application.Id, uploadFolder);
            application.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Documents saved successfully.";
            return RedirectToAction(nameof(Payment), new { applicationId = application.Id });
        }

        private async Task SaveDocument(IFormFile? file, string documentType, int applicationId, string uploadFolder)
        {
            if (file == null || file.Length == 0)
            {
                return;
            }

            var allowedExtensions = new[]
            {
             ".jpg",
             ".jpeg",
             ".png",
             ".pdf"
            };

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                throw new InvalidOperationException("Only JPG, JPEG, PNG and PDF files are allowed.");
            }
            var fileName = $"{documentType}_{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(uploadFolder, fileName);
            await using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);
            var document = new StudentApplicationDocument
            {
                StudentApplicationId = applicationId,
                DocumentType = documentType,
                FileName = file.FileName,
                FilePath = $"/uploads/applications/{applicationId}/{fileName}",
                ContentType = file.ContentType,
                UploadedAt = DateTime.UtcNow
            };
            _context.StudentApplicationDocuments.Add(document);
        }

        // PAYMENT - GET
        [HttpGet]
        public async Task<IActionResult> Payment(int applicationId)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            var application = await _context.StudentApplications
                .FirstOrDefaultAsync(x =>
                    x.Id == applicationId &&
                    x.UserId == user.Id);

            if (application == null)
            {
                return NotFound("Application not found.");
            }

            // Already approved → Hall Ticket
            if (application.Status == "Approved")
            {
                return RedirectToAction(nameof(HallTicket),new
                    {
                        applicationId = application.Id
                    });
            }

            // Already submitted → Confirmation
            if (application.Status == "Submitted")
            {
                return RedirectToAction(nameof(Confirmation),new
                    {
                        applicationId = application.Id
                    });
            }

            // Check already successful payment
            var paidPayment = await _context.StudentApplicationPayments
                .FirstOrDefaultAsync(x =>
                    x.StudentApplicationId == application.Id &&
                    x.Status == "Paid");

            if (paidPayment != null)
            {
                return RedirectToAction(nameof(Review),new
                    {
                        applicationId = application.Id
                    });
            }

            var exam = await _context.Exams
                .FirstOrDefaultAsync(x =>
                    x.Id == application.ExamId);
            if (exam == null)
            {
                return NotFound("The selected examination was not found.");
            }
            ViewBag.ExamName = exam.ExamName;
            ViewBag.ApplicationNumber = application.ApplicationNumber;
            ViewBag.ApplicationFee = exam.ApplicationFee;
            ViewBag.ApplicationId = application.Id;
            return View();
        }

        // PAYMENT - CREATE RAZORPAY ORDER
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePaymentOrder(int applicationId)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "User is not authenticated."
                });
            }

            //demo code
            if (applicationId <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"Invalid applicationId received: {applicationId}"
                });
            }
            //demo end

            var application = await _context.StudentApplications
            .FirstOrDefaultAsync(x =>
                x.Id == applicationId &&
                x.UserId == user.Id);

            if (application == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Application not found."
                });
            }

            var exam = await _context.Exams
                .FirstOrDefaultAsync(x =>
                    x.Id == application.ExamId);

            if (exam == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Examination not found."
                });
            }

            if (exam.ApplicationFee <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid application fee."
                });
            }

            var existingPayment = await _context.StudentApplicationPayments
                .FirstOrDefaultAsync(x =>
                    x.StudentApplicationId == application.Id &&
                    x.Status == "Paid");
            if (existingPayment != null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Application payment is already completed."
                });
            }
            var keyId = _configuration["Razorpay:KeyId"];
            var keySecret = _configuration["Razorpay:KeySecret"];
            if (string.IsNullOrWhiteSpace(keyId) ||
                string.IsNullOrWhiteSpace(keySecret) ||
                keyId.StartsWith("YOUR_"))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Razorpay test credentials are not configured."
                });
            }
            var amountInPaise = (int)Math.Round(exam.ApplicationFee * 100);
            var client = new RazorpayClient(keyId, keySecret);
            var options = new Dictionary<string, object>
            {
                { "amount", amountInPaise },
                { "currency", "INR" },
                { "receipt", application.ApplicationNumber },
                { "payment_capture", 1 }
            };

            Razorpay.Api.Order order = client.Order.Create(options);
            var payment = new StudentApplicationPayment
            {
                StudentApplicationId = application.Id,
                OrderId = order["id"].ToString()!,
                Amount = exam.ApplicationFee,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.StudentApplicationPayments.Add(payment);
            await _context.SaveChangesAsync();
            return Json(new
            {
                success = true,
                key = keyId,
                orderId = order["id"].ToString(),
                amount = amountInPaise,
                currency = "INR",
                name = exam.ExamName,
                description = "Examination Application Fee",
                prefill = new
                {
                    name = application.FullName,
                    email = application.Email ?? user.Email,
                    contact = application.Mobile ?? user.PhoneNumber
                }
            });
        }

        // PAYMENT - VERIFY
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyPayment(
            [FromBody] RazorpayPaymentVerificationRequest model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "User is not authenticated."
                });
            }

            if (string.IsNullOrWhiteSpace(model.RazorpayOrderId) ||
                string.IsNullOrWhiteSpace(model.RazorpayPaymentId) ||
                string.IsNullOrWhiteSpace(model.RazorpaySignature))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid payment response."
                });
            }

            var payment = await _context.StudentApplicationPayments
                .Include(x => x.StudentApplication)
                .FirstOrDefaultAsync(x =>
                    x.OrderId == model.RazorpayOrderId &&
                    x.StudentApplication != null &&
                    x.StudentApplication.UserId == user.Id);

            if (payment == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Payment order not found."
                });
            }

            var keySecret = _configuration["Razorpay:KeySecret"];
            if (string.IsNullOrWhiteSpace(keySecret) ||
                keySecret.StartsWith("YOUR_"))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Razorpay credentials are not configured."
                });
            }

            try
            {
                var attributes = new Dictionary<string, string>
            {
            {
                "razorpay_order_id",
                model.RazorpayOrderId
            },
            {
                "razorpay_payment_id",
                model.RazorpayPaymentId
            },
            {
                "razorpay_signature",
                model.RazorpaySignature
            }
            };
                Utils.verifyPaymentSignature(attributes);
                payment.PaymentId = model.RazorpayPaymentId;
                payment.Signature = model.RazorpaySignature;
                payment.Status = "Paid";
                payment.PaymentMethod = "Razorpay";
                payment.PaidAt = DateTime.UtcNow;
                payment.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return Json(new
                {
                    success = true,
                    applicationId = payment.StudentApplicationId
                });
            }
            catch
            {
                payment.Status = "Failed";
                payment.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return BadRequest(new
                {
                    success = false,
                    message = "Payment signature verification failed."
                });
            }
        }

        // REVIEW - GET
        [HttpGet]
        public async Task<IActionResult> Review(int applicationId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var application = await _context.StudentApplications
                .FirstOrDefaultAsync(x =>
                    x.Id == applicationId &&
                    x.UserId == user.Id);

            if (application == null)
            {
                return NotFound("Application not found.");
            }

            // Approved application → Hall Ticket
            if (application.Status == "Approved")
            {
                return RedirectToAction(nameof(HallTicket),new
                    {
                        applicationId = application.Id
                    });
            }

            // Submitted application → Application Details
            if (application.Status == "Submitted")
            {
                return RedirectToAction(nameof(ApplicationDetails),new
                    {
                        applicationId = application.Id
                    });
            }

            var exam = await _context.Exams
                            .FirstOrDefaultAsync(x => x.Id == application.ExamId);

            if (exam == null)
            {
                return NotFound("The selected examination was not found.");
            }

            var payment = await _context.StudentApplicationPayments
                .FirstOrDefaultAsync(x =>
                    x.StudentApplicationId == application.Id &&
                    x.Status == "Paid");

            if (payment == null)
            {
                return RedirectToAction(nameof(Payment),new { applicationId = application.Id });
            }

            var documents = await _context.StudentApplicationDocuments
                .Where(x => x.StudentApplicationId == application.Id)
                .ToListAsync();
            var selectedSubjects = await _context.StudentApplicationSubjects
                .Where(x => x.StudentApplicationId == application.Id)
                .OrderBy(x => x.Id)
                .ToListAsync();
            ViewBag.ExamName = exam.ExamName;
            ViewBag.ApplicationFee = exam.ApplicationFee;
            ViewBag.Payment = payment;
            ViewBag.Documents = documents;
            ViewBag.SelectedSubjects = selectedSubjects;
            return View(application);
        }

        // FINAL SUBMIT - GET
        [HttpGet]
        public async Task<IActionResult> Submit(int applicationId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var application = await _context.StudentApplications
                .FirstOrDefaultAsync(x =>
                    x.Id == applicationId &&
                    x.UserId == user.Id);

            if (application == null)
            {
                return NotFound("Application not found.");
            }

            // Already approved → Hall Ticket
            if (application.Status == "Approved")
            {
                return RedirectToAction(nameof(HallTicket),new
                    {
                        applicationId = application.Id
                    });
            }

            // Already submitted → Confirmation
            if (application.Status == "Submitted")
            {
                return RedirectToAction(nameof(Confirmation),new
                    {
                        applicationId = application.Id
                    });
            }

            var exam = await _context.Exams
                .FirstOrDefaultAsync(x => x.Id == application.ExamId);

            if (exam == null)
            {
                return NotFound("The selected examination was not found.");
            }

            // Payment check
            var payment = await _context.StudentApplicationPayments
                .FirstOrDefaultAsync(x =>
                    x.StudentApplicationId == application.Id &&
                    x.Status == "Paid");

            if (payment == null)
            {
                return RedirectToAction(nameof(Payment),new { applicationId = application.Id });
            }

            // Documents check
            var documents = await _context.StudentApplicationDocuments
            .Where(x => x.StudentApplicationId == application.Id)
            .ToListAsync();

            var requiredDocumentTypes = new List<string>
            {
                "Photo",
                "Signature",
                "10thMarksheet",
                "AadhaarCard"
            };

            if (string.Equals(application.TwelfthStatus,"Passed",StringComparison.OrdinalIgnoreCase))
            {
                requiredDocumentTypes.Add("12thMarksheet");
            }

            if (application.HasGraduation)
            {
                requiredDocumentTypes.Add("GraduationDocument");
            }

            var hasAllRequiredDocuments = requiredDocumentTypes
                .All(type => documents.Any(x => x.DocumentType == type));

            if (!hasAllRequiredDocuments)
            {
                TempData["ErrorMessage"] = "Please upload all required documents before final submission.";
                return RedirectToAction(nameof(Documents),new
                    {
                        applicationId = application.Id
                    });
            }
            ViewBag.ExamName = exam.ExamName;
            ViewBag.Payment = payment;
            ViewBag.Documents = documents;
            return View(application);
        }

        // FINAL SUBMIT - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(int applicationId, string confirmation)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var application = await _context.StudentApplications
                .FirstOrDefaultAsync(x =>
                    x.Id == applicationId &&
                    x.UserId == user.Id);

            if (application == null)
            {
                return NotFound("Application not found.");
            }

            // Already approved → Hall Ticket
            if (application.Status == "Approved")
            {
                return RedirectToAction(nameof(HallTicket),new
                    {
                        applicationId = application.Id
                    });
            }

            // Prevent duplicate submission
            if (application.Status == "Submitted")
            {
                return RedirectToAction(nameof(Confirmation),new
                    {
                        applicationId = application.Id
                    });
            }

            // Payment verification
            var payment = await _context.StudentApplicationPayments
                .FirstOrDefaultAsync(x =>
                    x.StudentApplicationId == application.Id &&
                    x.Status == "Paid");

            if (payment == null)
            {
                TempData["ErrorMessage"] = "Application payment is not completed.";
                return RedirectToAction(nameof(Payment),new { applicationId = application.Id });
            }

            // Documents verification
            var documents = await _context.StudentApplicationDocuments
                .Where(x => x.StudentApplicationId == application.Id)
                .ToListAsync();

            var requiredDocumentTypes = new List<string>
            {
                "Photo",
                "Signature",
                "10thMarksheet",
                "AadhaarCard"
            };

            if (string.Equals(application.TwelfthStatus,"Passed",StringComparison.OrdinalIgnoreCase))
            {
                requiredDocumentTypes.Add("12thMarksheet");
            }

            if (application.HasGraduation)
            {
                requiredDocumentTypes.Add("GraduationDocument");
            }

            var hasAllRequiredDocuments = requiredDocumentTypes
                .All(type => documents.Any(x => x.DocumentType == type));

            if (!hasAllRequiredDocuments)
            {
                TempData["ErrorMessage"] = "Please upload all required documents before final submission.";
                return RedirectToAction(nameof(Documents),new
                    {
                        applicationId = application.Id
                    });
            }

            // Final application status
            application.Status = "Submitted";
            application.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Your application has been submitted successfully.";
            return RedirectToAction(nameof(Confirmation),new { applicationId = application.Id });
        }

        // CONFIRMATION - GET
        [HttpGet]
        public async Task<IActionResult> Confirmation(int applicationId)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            var application = await _context.StudentApplications
                .FirstOrDefaultAsync(x =>
                    x.Id == applicationId &&
                    x.UserId == user.Id);

            if (application == null)
            {
                return NotFound("Application not found.");
            }

            var exam = await _context.Exams
                .FirstOrDefaultAsync(x => x.Id == application.ExamId);

            if (exam == null)
            {
                return NotFound("The selected examination was not found.");
            }

            ViewBag.ExamName = exam.ExamName;
            return View(application);
        }

        // APPLICATION DETAILS - GET
        [HttpGet]
        public async Task<IActionResult> ApplicationDetails(int applicationId)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            var application = await _context.StudentApplications
                .FirstOrDefaultAsync(x =>
                    x.Id == applicationId &&
                    x.UserId == user.Id);

            if (application == null)
            {
                return NotFound("Application not found.");
            }

            var exam = await _context.Exams
                .FirstOrDefaultAsync(x => x.Id == application.ExamId);

            if (exam == null)
            {
                return NotFound("The selected examination was not found.");
            }

            var payment = await _context.StudentApplicationPayments
                .FirstOrDefaultAsync(x =>
                    x.StudentApplicationId == application.Id &&
                    x.Status == "Paid");

            var documents = await _context.StudentApplicationDocuments
                .Where(x => x.StudentApplicationId == application.Id)
                .ToListAsync();
            var selectedSubjects = await _context.StudentApplicationSubjects
                .Where(x => x.StudentApplicationId == application.Id)
                .OrderBy(x => x.Id)
                .ToListAsync();
            ViewBag.ExamName = exam.ExamName;
            ViewBag.ExamCode = exam.ExamCode;
            ViewBag.ExamDate = exam.ExamDate;
            ViewBag.ApplicationFee = exam.ApplicationFee;
            ViewBag.Payment = payment;
            ViewBag.Documents = documents;
            ViewBag.SelectedSubjects = selectedSubjects;
            return View(application);
        }

        // HALL TICKET
        [HttpGet]
        public async Task<IActionResult> HallTicket(int applicationId)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            var application = await _context.StudentApplications
                .FirstOrDefaultAsync(x =>
                    x.Id == applicationId &&
                    x.UserId == user.Id);

            if (application == null)
            {
                return NotFound("Application not found.");
            }

            // Hall ticket only for approved application
            if (application.Status != "Approved")
            {
                return BadRequest("Hall Ticket is available only after application approval.");
            }

            // Payment must be completed
            var payment = await _context.StudentApplicationPayments
                .FirstOrDefaultAsync(x =>
                    x.StudentApplicationId == application.Id &&
                    x.Status == "Paid");

            if (payment == null)
            {
                return BadRequest("Hall Ticket is not available because payment is not completed.");
            }

            // Hall Ticket Number must exist
            if (string.IsNullOrWhiteSpace(application.HallTicketNumber))
            {
                return BadRequest("Hall Ticket number has not been generated yet.");
            }

            var exam = await _context.Exams
                .FirstOrDefaultAsync(x =>
                    x.Id == application.ExamId);

            if (exam == null)
            {
                return NotFound("Examination not found.");
            }

            if (!application.ExamCenterId.HasValue)
            {
                return BadRequest("Exam center has not been assigned.");
            }

            var center = await _context.ExamCenters
                .FirstOrDefaultAsync(x =>
                    x.Id == application.ExamCenterId.Value);

            if (center == null)
            {
                return NotFound("Exam center not found.");
            }

            var documents = await _context.StudentApplicationDocuments
            .Where(x =>
                x.StudentApplicationId == application.Id)
            .ToListAsync();
            var photo = documents
                .FirstOrDefault(x => x.DocumentType == "Photo");
            var signature = documents
                .FirstOrDefault(x => x.DocumentType == "Signature");
            // Institute Settings
            var institute = await _context.InstituteSettings
                .FirstOrDefaultAsync();
            ViewBag.Exam = exam;
            ViewBag.ExamCenter = center;
            ViewBag.Photo = photo;
            ViewBag.Signature = signature;
            ViewBag.Institute = institute;
            return View(application);
        }
    }
}
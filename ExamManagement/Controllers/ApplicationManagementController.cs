using ExamManagement.Data;
using ExamManagement.Models;
using ExamManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ApplicationManagementController : Controller
    {
        private readonly ApplicationDbContext _context;
        public ApplicationManagementController(ApplicationDbContext context)
        {
            _context = context;
        }

        // INDEX
        [HttpGet]
        public async Task<IActionResult> Index(int? examId,string? status)
        {
            var query = _context.StudentApplications
                .AsQueryable();

            if (examId.HasValue)
            {
                query = query.Where(x => x.ExamId == examId.Value);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(x => x.Status == status);
            }

            var applications = await query
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            var model = new ApplicationManagementViewModel
            {
                Applications = applications,

                Exams = await _context.Exams
                    .OrderByDescending(x => x.Id)
                    .ToListAsync(),

                ExamCenters = await _context.ExamCenters
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.CenterName)
                    .ToListAsync(),

                ExamId = examId,
                Status = status
            };
            return View(model);
        }

        // DETAILS
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var application = await _context.StudentApplications
                .FirstOrDefaultAsync(x => x.Id == id);

            if (application == null)
            {
                return NotFound("Application not found.");
            }

            var exam = await _context.Exams
                .FirstOrDefaultAsync(x => x.Id == application.ExamId);

            if (exam == null)
            {
                return NotFound("Examination not found.");
            }

            var documents = await _context.StudentApplicationDocuments
                .Where(x => x.StudentApplicationId == id)
                .ToListAsync();

            var payment = await _context.StudentApplicationPayments
                .FirstOrDefaultAsync(x =>
                    x.StudentApplicationId == id &&
                    x.Status == "Paid");

            var centers = await _context.ExamCenters
                .Where(x =>
                    x.ExamId == application.ExamId &&
                    x.IsActive)
                .OrderBy(x => x.CenterName)
                .ToListAsync();

            ViewBag.Exam = exam;
            ViewBag.Documents = documents;
            ViewBag.Payment = payment;
            ViewBag.ExamCenters = centers;
            return View(application);
        }

        // APPROVE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var application = await _context.StudentApplications
                .FirstOrDefaultAsync(x => x.Id == id);

            if (application == null)
            {
                return NotFound("Application not found.");
            }

            if (application.Status != "Submitted")
            {
                TempData["ErrorMessage"] = "Only submitted applications can be approved.";
                return RedirectToAction(nameof(Details), new { id });
            }
            application.Status = "Approved";
            application.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Application approved successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }


        // REJECT
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var application = await _context.StudentApplications
                .FirstOrDefaultAsync(x => x.Id == id);

            if (application == null)
            {
                return NotFound("Application not found.");
            }

            if (application.Status != "Submitted")
            {
                TempData["ErrorMessage"] = "Only submitted applications can be rejected.";
                return RedirectToAction(nameof(Details), new { id });
            }
            application.Status = "Rejected";
            application.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Application rejected.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // ASSIGN CENTER + GENERATE HALL TICKET
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignCenter(int id,int centerId,DateTime? reportingTime)
        {
            var application = await _context.StudentApplications
                .FirstOrDefaultAsync(x => x.Id == id);

            if (application == null)
            {
                return NotFound("Application not found.");
            }

            if (application.Status != "Approved")
            {
                TempData["ErrorMessage"] = "Application must be approved before assigning exam center.";
                return RedirectToAction(nameof(Details), new { id });
            }
            
            var center = await _context.ExamCenters
                .FirstOrDefaultAsync(x =>
                    x.Id == centerId &&
                    x.ExamId == application.ExamId &&
                    x.IsActive);

            if (center == null)
            {
                TempData["ErrorMessage"] = "Invalid examination center.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (!reportingTime.HasValue)
            {
                TempData["ErrorMessage"] = "Reporting time is required.";
                return RedirectToAction(nameof(Details), new { id });
            }

            application.ExamCenterId = center.Id;
            application.ExamReportingTime = reportingTime;
            if (string.IsNullOrWhiteSpace(application.HallTicketNumber))
            {
                application.HallTicketNumber = $"HT-{DateTime.UtcNow:yyyy}-{application.Id:D6}";
            }

            application.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Exam center assigned and Hall Ticket generated successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // VIEW HALL TICKET
        [HttpGet]
        public async Task<IActionResult> HallTicket(int id)
        {
            var application = await _context.StudentApplications
                .FirstOrDefaultAsync(x => x.Id == id);

            if (application == null)
            {
                return NotFound("Application not found.");
            }

            if (application.Status != "Approved")
            {
                return BadRequest("Hall Ticket is available only for approved applications.");
            }
            return RedirectToAction("HallTicket","StudentApplication",new { applicationId = id });
        }

        [HttpGet]
        public async Task<IActionResult> Document(int documentId)
        {
            var document = await _context.StudentApplicationDocuments
                .FirstOrDefaultAsync(x => x.Id == documentId);

            if (document == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(document.FilePath))
            {
                return NotFound();
            }

            var relativePath = document.FilePath
                .TrimStart('/')
                .Replace('/', Path.DirectorySeparatorChar);
            var webRootPath = Path.Combine(Directory.GetCurrentDirectory(),"wwwroot");
            var filePath = Path.GetFullPath(Path.Combine(webRootPath, relativePath));
            var uploadRoot = Path.GetFullPath(Path.Combine(webRootPath,"uploads","applications"));
            if (!filePath.StartsWith(uploadRoot + Path.DirectorySeparatorChar,
                    StringComparison.OrdinalIgnoreCase))
            {
                return NotFound();
            }

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            var contentType = string.IsNullOrWhiteSpace(document.ContentType)
                ? "application/octet-stream"
                : document.ContentType;
            return PhysicalFile(filePath,contentType,enableRangeProcessing: true);
        }
    }
}
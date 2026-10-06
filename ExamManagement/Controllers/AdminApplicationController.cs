using ExamManagement.Data;
using ExamManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminApplicationController : Controller
    {
        private readonly ApplicationDbContext _context;
        public AdminApplicationController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: AdminApplication
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var applications = await _context.StudentApplications
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
            return View(applications);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var application = await _context.StudentApplications
                .FirstOrDefaultAsync(x => x.Id == id);

            if (application == null)
                return NotFound();

            var exam = await _context.Exams
                .FirstOrDefaultAsync(x => x.Id == application.ExamId);

            var centers = await _context.ExamCenters
                .Where(x =>
                    x.ExamId == application.ExamId &&
                    x.IsActive)
                .OrderBy(x => x.CenterName)
                .ToListAsync();

            ViewBag.Exam = exam;
            ViewBag.Centers = centers;
            return View(application);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id,int examCenterId,DateTime reportingTime)
        {
            var application = await _context.StudentApplications
                .FirstOrDefaultAsync(x => x.Id == id);

            if (application == null)
                return NotFound("Application not found.");

            var payment = await _context.StudentApplicationPayments
                .Where(x => x.StudentApplicationId == application.Id)
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();

            if (payment == null)
            {
                TempData["ErrorMessage"] = $"No payment record found for Application ID {application.Id}.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (payment.Status != "Paid")
            {
                TempData["ErrorMessage"] = $"Payment is not completed. Current payment status: {payment.Status}";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (string.IsNullOrWhiteSpace(payment.PaymentId))
            {
                TempData["ErrorMessage"] = "Payment record exists, but Razorpay Payment ID is missing.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var center = await _context.ExamCenters
                .FirstOrDefaultAsync(x =>
                    x.Id == examCenterId &&
                    x.ExamId == application.ExamId &&
                    x.IsActive);

            if (center == null)
            {
                TempData["ErrorMessage"] = "Please select a valid exam center.";
                return RedirectToAction(nameof(Details), new { id });
            }

            application.Status = "Approved";
            application.ExamCenterId = center.Id;
            application.ExamReportingTime = reportingTime;
            application.ApprovedAt = DateTime.UtcNow;
            if (string.IsNullOrWhiteSpace(application.HallTicketNumber))
            {
                application.HallTicketNumber = $"HT-{DateTime.UtcNow:yyyy}-{application.Id:D6}";
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Application approved and exam center assigned successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(
         int id,
         string rejectionReason)
        {
            var application = await _context.StudentApplications
                .FirstOrDefaultAsync(x => x.Id == id);

            if (application == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(rejectionReason))
            {
                TempData["ErrorMessage"] = "Rejection reason is required.";
                return RedirectToAction(nameof(Details), new { id });
            }

            application.Status = "Rejected";
            application.RejectionReason = rejectionReason.Trim();
            application.ExamCenterId = null;
            application.ApprovedAt = null;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Application rejected successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
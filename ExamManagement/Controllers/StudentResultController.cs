using System.Security.Claims;
using ExamManagement.Data;
using ExamManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamManagement.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentResultController : Controller
    {
        private readonly ApplicationDbContext _context;
        public StudentResultController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /StudentResult
        [HttpGet]
        public async Task<IActionResult> Index(int applicationId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var application = await _context.StudentApplications
                .FirstOrDefaultAsync(x =>
                    x.Id == applicationId &&
                    x.UserId == userId);

            if (application == null)
            {
                return NotFound();
            }

            var result = await _context.ExamResults
                .Include(x => x.Exam)
                .FirstOrDefaultAsync(x =>
                    x.StudentApplicationId == applicationId &&
                    x.ExamId == application.ExamId &&
                    x.IsPublished);

            if (result == null)
            {
                TempData["ErrorMessage"] = "No published result is available.";
                return RedirectToAction("Index","Student");
            }

            var subjects = await _context.ExamResultSubjects
                .Where(x => x.ExamResultId == result.Id)
                .OrderBy(x => x.Id)
                .ToListAsync();

            var category = string.IsNullOrWhiteSpace(application.Category)
                ? "General"
                : application.Category;

            var cutoff = await _context.ExamCutoffs
                .FirstOrDefaultAsync(x =>
                    x.ExamId == result.ExamId &&
                    x.Category == category &&
                    x.IsActive);

            var model = new StudentResultViewModel
            {
                Application = application,
                Exam = result.Exam!,
                Result = result,
                Subjects = subjects,
                Cutoff = cutoff
            };
            return View(model);
        }
    }
}
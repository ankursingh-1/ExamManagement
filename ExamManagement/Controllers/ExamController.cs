using ExamManagement.Data;
using ExamManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ExamController : Controller
    {
        private readonly ApplicationDbContext _context;
        public ExamController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Exam
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var exams = await _context.Exams
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return View(exams);
        }

        // GET: /Exam/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Exam/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Exam model)
        {
            ValidateExam(model);
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var examCodeExists = await _context.Exams
                .AnyAsync(x => x.ExamCode == model.ExamCode);

            if (examCodeExists)
            {
                ModelState.AddModelError("ExamCode","This exam code already exists.");
                return View(model);
            }

            model.CreatedAt = DateTime.UtcNow;
            model.IsActive = true;
            _context.Exams.Add(model);
            await _context.SaveChangesAsync();

            // Create Eligibility Configuration
            var eligibility = new ExamEligibility
            {
                ExamId = model.Id,
                Requires10th = Request.Form["Requires10th"] == "true",
                Requires12th = Request.Form["Requires12th"] == "true",
                RequiresGraduation = Request.Form["RequiresGraduation"] == "true",
                Allow12thPassed = Request.Form["Allow12thPassed"] == "true",
                Allow12thAppearing = Request.Form["Allow12thAppearing"] == "true",
                Allow12thResultAwaited = Request.Form["Allow12thResultAwaited"] == "true",
                RequiresPhysics = Request.Form["RequiresPhysics"] == "true",
                RequiresChemistry = Request.Form["RequiresChemistry"] == "true",
                RequiresBiology = Request.Form["RequiresBiology"] == "true",
                RequiresMathematics = Request.Form["RequiresMathematics"] == "true"
            };

            var minimumPercentage = Request.Form["MinimumPercentage"].ToString();

            if (decimal.TryParse(minimumPercentage,out decimal percentage))
            {
                eligibility.MinimumPercentage = percentage;
            }
            _context.ExamEligibilities.Add(eligibility);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Exam and eligibility configuration created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Exam/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var exam = await _context.Exams
                .Include(x => x.EligibilityConfiguration)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (exam == null)
            {
                return NotFound();
            }
            return View(exam);
        }

        // GET: /Exam/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var exam = await _context.Exams
                .Include(x => x.EligibilityConfiguration)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (exam == null)
            {
                return NotFound();
            }
            return View(exam);
        }

        // POST: /Exam/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,Exam model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }
            ValidateExam(model);
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var exam = await _context.Exams
                .FirstOrDefaultAsync(x => x.Id == id);

            if (exam == null)
            {
                return NotFound();
            }

            var duplicateCode = await _context.Exams
                .AnyAsync(x => x.ExamCode == model.ExamCode &&  x.Id != id);

            if (duplicateCode)
            {
                ModelState.AddModelError("ExamCode", "This exam code already exists.");
                return View(model);
            }
            exam.ExamName = model.ExamName;
            exam.ExamCode = model.ExamCode;
            exam.ExamType = model.ExamType;
            exam.Description = model.Description;
            exam.ApplicationStartDate = model.ApplicationStartDate;
            exam.ApplicationEndDate = model.ApplicationEndDate;
            exam.ExamDate = model.ExamDate;
            exam.StartTime = model.StartTime;
            exam.EndTime = model.EndTime;
            exam.ApplicationFee = model.ApplicationFee;
            exam.Eligibility = model.Eligibility;
            exam.Instructions = model.Instructions;
            exam.UpdatedAt = DateTime.UtcNow;
            // Update Eligibility Configuration
            var eligibility = await _context.ExamEligibilities
                .FirstOrDefaultAsync(x => x.ExamId == id);
            if (eligibility == null)
            {
                eligibility = new ExamEligibility
                {
                    ExamId = id
                };
                _context.ExamEligibilities.Add(eligibility);
            }
            // Qualification
            eligibility.Requires10th = Request.Form["Requires10th"] == "true";
            eligibility.Requires12th = Request.Form["Requires12th"] == "true";
            eligibility.RequiresGraduation = Request.Form["RequiresGraduation"] == "true";
            // 12th Status
            eligibility.Allow12thPassed = Request.Form["Allow12thPassed"] == "true";
            eligibility.Allow12thAppearing = Request.Form["Allow12thAppearing"] == "true";
            eligibility.Allow12thResultAwaited = Request.Form["Allow12thResultAwaited"] == "true";
            // Subjects
            eligibility.RequiresPhysics = Request.Form["RequiresPhysics"] == "true";
            eligibility.RequiresChemistry = Request.Form["RequiresChemistry"] == "true";
            eligibility.RequiresBiology = Request.Form["RequiresBiology"] == "true";
            eligibility.RequiresMathematics = Request.Form["RequiresMathematics"] == "true";
            // Minimum Percentage
            var minimumPercentage = Request.Form["MinimumPercentage"].ToString();

            if (decimal.TryParse(minimumPercentage,out decimal percentage))
            {
                eligibility.MinimumPercentage = percentage;
            }
            else
            {
                eligibility.MinimumPercentage = null;
            }
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Exam updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Exam/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var exam = await _context.Exams
                .FirstOrDefaultAsync(x => x.Id == id);

            if (exam == null)
            {
                return NotFound();
            }
            exam.IsActive = !exam.IsActive;
            exam.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = exam.IsActive
                    ? "Exam activated successfully."
                    : "Exam deactivated successfully.";
            return RedirectToAction(nameof(Index));
        }

        private void ValidateExam(Exam model)
        {
            if (model.ApplicationStartDate.Date >
                model.ApplicationEndDate.Date)
            {
                ModelState.AddModelError("ApplicationEndDate", "Application end date must be after start date.");
            }

            if (model.ApplicationEndDate.Date >
                model.ExamDate.Date)
            {
                ModelState.AddModelError("ExamDate", "Exam date must be on or after application end date.");
            }

            if (model.StartTime >= model.EndTime)
            {
                ModelState.AddModelError("EndTime", "Exam end time must be after start time.");
            }
        }
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Apply(int id)
        {
            var exam = await _context.Exams
                .Include(x => x.EligibilityConfiguration)
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);
            if (exam == null)
            {
                return NotFound();
            }
            return View(exam);
        }

        // POST: /Exam/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var exam = await _context.Exams
                .FirstOrDefaultAsync(x => x.Id == id);

            if (exam == null)
            {
                return NotFound();
            }

            // Exam can only be deleted after application deadline
            // or after the examination date has passed.
            var today = DateTime.Today;

            if (exam.ApplicationEndDate.Date >= today &&
                exam.ExamDate.Date >= today)
            {
                TempData["ErrorMessage"] =
                    "This examination cannot be deleted before its application deadline or examination date has passed.";
                return RedirectToAction(nameof(Index));
            }

            // Do not delete an exam if student applications already exist.
            var hasApplications = await _context.StudentApplications
                .AnyAsync(x => x.ExamId == id);

            if (hasApplications)
            {
                TempData["ErrorMessage"] =
                    "This examination cannot be deleted because student applications already exist for this exam.";
                return RedirectToAction(nameof(Index));
            }

            // Delete eligibility configuration first.
            var eligibility = await _context.ExamEligibilities
                .FirstOrDefaultAsync(x => x.ExamId == id);

            if (eligibility != null)
            {
                _context.ExamEligibilities.Remove(eligibility);
            }

            // Delete exam.
            _context.Exams.Remove(exam);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Examination deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
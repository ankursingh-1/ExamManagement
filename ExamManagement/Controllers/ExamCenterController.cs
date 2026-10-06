using ExamManagement.Data;
using ExamManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ExamCenterController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ExamCenterController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ExamCenter
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var centers = await _context.ExamCenters
                .Join(
                    _context.Exams,
                    center => center.ExamId,
                    exam => exam.Id,
                    (center, exam) => new ExamCenterListViewModel
                    {
                        Id = center.Id,
                        ExamId = exam.Id,
                        ExamName = exam.ExamName,
                        CenterName = center.CenterName,
                        City = center.City,
                        State = center.State,
                        Capacity = center.Capacity,
                        IsActive = center.IsActive
                    })
                .OrderBy(x => x.ExamName)
                .ThenBy(x => x.CenterName)
                .ToListAsync();

            return View(centers);
        }

        // GET: ExamCenter/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Exams = await _context.Exams
                .Where(x => x.IsActive)
                .OrderBy(x => x.ExamName)
                .ToListAsync();

            return View();
        }

        // POST: ExamCenter/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ExamCenter model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Exams = await _context.Exams
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.ExamName)
                    .ToListAsync();
                return View(model);
            }
            _context.ExamCenters.Add(model);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Exam center added successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: ExamCenter/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var center = await _context.ExamCenters
                .FirstOrDefaultAsync(x => x.Id == id);

            if (center == null)
            {
                return NotFound();
            }

            ViewBag.Exams = await _context.Exams
                .Where(x => x.IsActive)
                .OrderBy(x => x.ExamName)
                .ToListAsync();
            return View(center);
        }

        // POST: ExamCenter/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ExamCenter model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Exams = await _context.Exams
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.ExamName)
                    .ToListAsync();
                return View(model);
            }

            var center = await _context.ExamCenters
                .FirstOrDefaultAsync(x => x.Id == model.Id);

            if (center == null)
            {
                return NotFound();
            }

            center.ExamId = model.ExamId;
            center.CenterName = model.CenterName;
            center.Address = model.Address;
            center.City = model.City;
            center.State = model.State;
            center.Pincode = model.Pincode;
            center.Capacity = model.Capacity;
            center.IsActive = model.IsActive;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Exam center updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // POST: ExamCenter/ToggleStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var center = await _context.ExamCenters
                .FirstOrDefaultAsync(x => x.Id == id);

            if (center == null)
            {
                return NotFound();
            }
            center.IsActive = !center.IsActive;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // POST: ExamCenter/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var center = await _context.ExamCenters
                .FirstOrDefaultAsync(x => x.Id == id);

            if (center == null)
            {
                return NotFound();
            }
            _context.ExamCenters.Remove(center);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Exam center deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }

    public class ExamCenterListViewModel
    {
        public int Id { get; set; }
        public int ExamId { get; set; }
        public string ExamName { get; set; } = string.Empty;
        public string CenterName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public bool IsActive { get; set; }
    }
}
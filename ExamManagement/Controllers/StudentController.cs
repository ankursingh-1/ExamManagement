using ExamManagement.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamManagement.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public StudentController(ApplicationDbContext context,UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // STUDENT DASHBOARD
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            // Active exams
            var exams = await _context.Exams
                .Where(x => x.IsActive && x.ApplicationEndDate.Date >= DateTime.Today)
                .OrderBy(x => x.ApplicationEndDate)
                .ToListAsync();

            // Student applications
            var applications = await _context.StudentApplications
                .Where(x => x.UserId == user.Id)
                .ToListAsync();

            var publishedResults = await _context.ExamResults
                .Where(x => x.StudentApplication != null &&
                    x.StudentApplication.UserId == user.Id &&
                    x.IsPublished)
                .Select(x => x.StudentApplicationId)
                .ToListAsync();
            ViewBag.Applications = applications;
            ViewBag.PublishedResultApplicationIds = publishedResults;
            return View(exams);
        }
    }
}
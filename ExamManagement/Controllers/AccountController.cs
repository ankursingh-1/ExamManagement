using ExamManagement.Data;
using ExamManagement.Models;
using ExamManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamManagement.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;

        public AccountController(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            ApplicationDbContext context)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _context = context;
        }

        // LOGIN - GET
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null,int? examId = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (examId.HasValue)
            {
                ViewBag.ExamId = examId.Value;
            }
            return View();
        }

        // LOGIN - POST
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model,
            string? returnUrl = null,int? examId = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!ModelState.IsValid)
            {
                ViewBag.ExamId = examId;
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty,"Invalid email or password.");
                ViewBag.ExamId = examId;
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(user.UserName!,model.Password,
                model.RememberMe,lockoutOnFailure: true);
            if (result.Succeeded)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("Admin"))
                {
                    return RedirectToAction("Index","Admin");
                }

                if (roles.Contains("Student"))
                {
                    return RedirectToAction("Index","Student");
                }

                await _signInManager.SignOutAsync();
                ModelState.AddModelError(string.Empty,"No valid role assigned to this account.");
                return View(model);
            }
            ModelState.AddModelError(string.Empty,"Invalid email or password.");
            ViewBag.ExamId = examId;
            return View(model);
        }

        // STUDENT REGISTRATION - GET
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Register(int examId)
        {
            var exam = await _context.Exams
                .FirstOrDefaultAsync(x =>
                    x.Id == examId &&
                    x.IsActive);
            if (exam == null)
            {
                return NotFound("The selected examination is not available.");
            }
            ViewBag.ExamId = examId;
            ViewBag.ExamName = exam.ExamName;
            return View();
        }

        // STUDENT REGISTRATION - POST
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model,int examId)
        {
            // Check selected exam
            var exam = await _context.Exams
                .FirstOrDefaultAsync(x =>
                    x.Id == examId &&
                    x.IsActive);

            if (exam == null)
            {
                return NotFound("The selected examination is not available.");
            }

            // Model validation
            if (!ModelState.IsValid)
            {
                ViewBag.ExamId = examId;
                ViewBag.ExamName = exam.ExamName;
                return View(model);
            }

            // Check existing email
            var existingUser = await _userManager.FindByEmailAsync(model.Email);

            if (existingUser != null)
            {
                ModelState.AddModelError("Email","An account with this email address already exists.");
                ViewBag.ExamId = examId;
                ViewBag.ExamName = exam.ExamName;
                return View(model);
            }

            // Create Identity User
            var user = new IdentityUser
            {
                UserName = model.Email,
                Email = model.Email,
                PhoneNumber = model.Mobile,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user,model.Password);

            // Identity creation failed
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty,error.Description);
                }
                ViewBag.ExamId = examId;
                ViewBag.ExamName = exam.ExamName;
                return View(model);
            }

            // Assign Student role
            var roleExists = await _userManager.IsInRoleAsync(user,"Student");

            if (!roleExists)
            {
                await _userManager.AddToRoleAsync(user,"Student");
            }

            // Create Student Profile
            var studentProfile = new StudentProfile
            {
                UserId = user.Id,
                FullName = model.FullName,
                Mobile = model.Mobile,
                CreatedAt = DateTime.Now
            };

            _context.StudentProfiles.Add(studentProfile);
            await _context.SaveChangesAsync();

            // Automatically login student
            await _signInManager.SignInAsync(user,isPersistent: false);

            // For now go to Student Dashboard
            return RedirectToAction("Index","Student");
        }

        // LOGOUT
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login","Account");
        }

        // ACCESS DENIED
        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
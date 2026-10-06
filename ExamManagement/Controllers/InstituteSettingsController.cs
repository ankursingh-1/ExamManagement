using ExamManagement.Data;
using ExamManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class InstituteSettingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        public InstituteSettingsController(ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: /InstituteSettings
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var setting = await _context.InstituteSettings
                .FirstOrDefaultAsync();
            if (setting == null)
            {
                setting = new InstituteSetting();
            }
            return View(setting);
        }

        // POST: /InstituteSettings/Save
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(InstituteSetting model,IFormFile? logoFile)
        {
            // Remove validation for LogoPath because it is managed by server.
            ModelState.Remove(nameof(InstituteSetting.LogoPath));
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }
            var setting = await _context.InstituteSettings
                .FirstOrDefaultAsync();
            if (setting == null)
            {
                setting = new InstituteSetting
                {
                    CreatedAt = DateTime.UtcNow
                };
                _context.InstituteSettings.Add(setting);
            }

            // Logo upload
            if (logoFile != null && logoFile.Length > 0)
            {
                var allowedExtensions = new[]
                {
                    ".jpg",
                    ".jpeg",
                    ".png",
                    ".webp"
                };

                var extension = Path.GetExtension(logoFile.FileName)
                    .ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("logoFile","Only JPG, JPEG, PNG and WEBP files are allowed.");
                    return View("Index", model);
                }

                if (logoFile.Length > 2 * 1024 * 1024)
                {
                    ModelState.AddModelError("logoFile","Logo size must be less than 2 MB.");
                    return View("Index", model);
                }

                var uploadFolder = Path.Combine(
                    _environment.WebRootPath,"uploads","institute");

                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                // Delete old logo
                if (!string.IsNullOrWhiteSpace(setting.LogoPath))
                {
                    var oldLogoPath = Path.Combine(
                        _environment.WebRootPath,setting.LogoPath.TrimStart('/'));

                    if (System.IO.File.Exists(oldLogoPath))
                    {
                        System.IO.File.Delete(oldLogoPath);
                    }
                }
                var fileName = $"{Guid.NewGuid():N}{extension}";
                var filePath = Path.Combine(uploadFolder,fileName);
                using (var stream = new FileStream(filePath,FileMode.Create))
                {
                    await logoFile.CopyToAsync(stream);
                }
                setting.LogoPath = $"/uploads/institute/{fileName}";
            }
            setting.InstituteName = model.InstituteName;
            setting.InstituteCode = model.InstituteCode;
            setting.Address = model.Address;
            setting.City = model.City;
            setting.State = model.State;
            setting.PinCode = model.PinCode;
            setting.PhoneNumber = model.PhoneNumber;
            setting.EmailAddress = model.EmailAddress;
            setting.Website = model.Website;
            setting.SupportEmail = model.SupportEmail;
            setting.SupportPhone = model.SupportPhone;
            setting.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Institute settings saved successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
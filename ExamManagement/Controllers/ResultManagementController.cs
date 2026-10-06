using ExamManagement.Data;
using ExamManagement.Models;
using ExamManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;

namespace ExamManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ResultManagementController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ResultManagementController(ApplicationDbContext context)
        {
            _context = context;
        }

        // RESULT MANAGEMENT
        [HttpGet]
        public async Task<IActionResult> Index(int? examId,string? category)
        {
            var model = new ResultManagementViewModel
            {
                Exams = await _context.Exams
                    .OrderByDescending(x => x.Id)
                    .ToListAsync(),
                SelectedExamId = examId,
                SelectedCategory = category
            };

            if (!examId.HasValue)
            {
                return View(model);
            }

            model.SelectedExam = await _context.Exams
                .FirstOrDefaultAsync(x => x.Id == examId.Value);

            if (model.SelectedExam == null)
            {
                TempData["ErrorMessage"] = "Exam not found.";
                return View(model);
            }

            model.Cutoffs = await _context.ExamCutoffs
                .Where(x => x.ExamId == examId.Value && x.IsActive)
                .OrderBy(x => x.Category)
                .ToListAsync();

            var applicationsQuery = _context.StudentApplications
             .Where(x => x.ExamId == examId.Value && x.Status == "Approved");
            if (!string.IsNullOrWhiteSpace(category))
            {
                var selectedCategory = category.Trim();
                if (selectedCategory == "General")
                {
                    applicationsQuery = applicationsQuery
                        .Where(x => x.Category == null || x.Category == "" || x.Category == "General");
                }
                else
                {
                    applicationsQuery = applicationsQuery .Where(x => x.Category == selectedCategory);
                }
            }
            model.Applications = await applicationsQuery
                .OrderBy(x => x.ApplicationNumber)
                .ToListAsync();
            model.Results = await _context.ExamResults
                 .Where(x => x.ExamId == examId.Value)
                 .ToListAsync();
            model.ResultSubjects = await _context.ExamResultSubjects
                 .Where(x => x.ExamResult != null && x.ExamResult.ExamId == examId.Value)
                 .ToListAsync();
            return View(model);
        }

        // DOWNLOAD MARKS EXCEL TEMPLATE
        [HttpGet]
        public async Task<IActionResult> DownloadMarksTemplate(int examId,string? category)
        {
            var exam = await _context.Exams
                .FirstOrDefaultAsync(x => x.Id == examId);
            if (exam == null)
            {
                return NotFound("Exam not found.");
            }

            var applicationsQuery = _context.StudentApplications
                .Where(x => x.ExamId == examId && x.Status == "Approved");

            if (!string.IsNullOrWhiteSpace(category))
            {
                var selectedCategory = category.Trim();
                if (selectedCategory.Equals("General",StringComparison.OrdinalIgnoreCase))
                {
                    applicationsQuery = applicationsQuery
                        .Where(x => string.IsNullOrWhiteSpace(x.Category) || x.Category == "General");
                }
                else
                {
                    applicationsQuery = applicationsQuery
                        .Where(x => x.Category == selectedCategory);
                }
            }

            var applications = await applicationsQuery
                .OrderBy(x => x.ApplicationNumber)
                .ToListAsync();

            if (!applications.Any())
            {
                TempData["ErrorMessage"] = "No approved students found for this exam.";
                return RedirectToAction(nameof(Index),new
                    {
                        examId,
                        category
                    });
            }

            // Existing results
            var applicationIds = applications
                .Select(x => x.Id)
                .ToList();
            var existingResults = await _context.ExamResults
                .Where(x =>
                    applicationIds.Contains(x.StudentApplicationId) &&
                    x.ExamId == examId)
                .ToListAsync();
            var resultIds = existingResults
                .Select(x => x.Id)
                .ToList();
            var existingSubjects = await _context.ExamResultSubjects
                .Where(x => resultIds.Contains(x.ExamResultId))
                .ToListAsync();
            using var workbook = new XLWorkbook();

            // SHEET 1 - OVERALL MARKS
            var overallSheet = workbook.Worksheets.Add("Overall Marks");
            overallSheet.Cell(1, 1).Value = $"{exam.ExamName} - Overall Marks";
            overallSheet.Range(1, 1, 1, 5).Merge();
            overallSheet.Cell(1, 1).Style.Font.Bold = true;
            overallSheet.Cell(1, 1).Style.Font.FontSize = 16;
            overallSheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            overallSheet.Cell(3, 1).Value = "Application Number";
            overallSheet.Cell(3, 2).Value = "Student Name";
            overallSheet.Cell(3, 3).Value = "Category";
            overallSheet.Cell(3, 4).Value = "Total Marks";
            overallSheet.Cell(3, 5).Value = "Obtained Marks";
            var overallHeader = overallSheet.Range(3, 1, 3, 5);
            overallHeader.Style.Font.Bold = true;
            int overallRow = 4;
            foreach (var application in applications)
            {
                var result = existingResults
                    .FirstOrDefault(x => x.StudentApplicationId == application.Id);
                overallSheet.Cell(overallRow, 1).Value = application.ApplicationNumber;
                overallSheet.Cell(overallRow, 2).Value = application.FullName;
                overallSheet.Cell(overallRow, 3).Value = string.IsNullOrWhiteSpace(application.Category)
                        ? "General"
                        : application.Category;
                overallSheet.Cell(overallRow, 4).Value = result?.TotalMarks ?? 0;
                overallSheet.Cell(overallRow, 5).Value = result?.ObtainedMarks ?? 0;
                overallRow++;
            }

            // SHEET 2 - SUBJECT MARKS
            var subjectSheet = workbook.Worksheets.Add("Subject Marks");
            subjectSheet.Cell(1, 1).Value = $"{exam.ExamName} - Subject Marks";
            subjectSheet.Range(1, 1, 1, 5).Merge();
            subjectSheet.Cell(1, 1).Style.Font.Bold = true;
            subjectSheet.Cell(1, 1).Style.Font.FontSize = 16;
            subjectSheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            subjectSheet.Cell(3, 1).Value = "Application Number";
            subjectSheet.Cell(3, 2).Value = "Student Name";
            subjectSheet.Cell(3, 3).Value = "Subject";
            subjectSheet.Cell(3, 4).Value = "Total Marks";
            subjectSheet.Cell(3, 5).Value = "Obtained Marks";
            var subjectHeader = subjectSheet.Range(3, 1, 3, 5);
            subjectHeader.Style.Font.Bold = true;
            int subjectRow = 4;
            foreach (var application in applications)
            {
                var result = existingResults
                    .FirstOrDefault(x => x.StudentApplicationId == application.Id);
                var studentSubjects = new List<string>();
                if (application.HasPhysics)
                {
                    studentSubjects.Add("Physics");
                }

                if (application.HasChemistry)
                {
                    studentSubjects.Add("Chemistry");
                }

                if (application.HasBiology)
                {
                    studentSubjects.Add("Biology");
                }

                if (application.HasMathematics)
                {
                    studentSubjects.Add("Mathematics");
                }

                foreach (var subjectName in studentSubjects)
                {
                    var subjectResult = existingSubjects.FirstOrDefault(x => x.ExamResultId == result?.Id && x.SubjectName == subjectName);
                    subjectSheet.Cell(subjectRow, 1).Value = application.ApplicationNumber;
                    subjectSheet.Cell(subjectRow, 2).Value = application.FullName;
                    subjectSheet.Cell(subjectRow, 3).Value = subjectName;
                    subjectSheet.Cell(subjectRow, 4).Value = subjectResult?.TotalMarks ?? 0;
                    subjectSheet.Cell(subjectRow, 5).Value = subjectResult?.ObtainedMarks ?? 0;
                    subjectRow++;
                }
            }

            // FORMATTING
            overallSheet.Columns().AdjustToContents();
            subjectSheet.Columns().AdjustToContents();
            overallSheet.Column(4).Width = 15;
            overallSheet.Column(5).Width = 18;
            subjectSheet.Column(4).Width = 15;
            subjectSheet.Column(5).Width = 18;
            overallSheet.SheetView.FreezeRows(3);
            subjectSheet.SheetView.FreezeRows(3);
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;
            var fileName = $"MarksTemplate-{exam.ExamName}-{DateTime.Now:yyyyMMddHHmmss}.xlsx";
            return File(stream.ToArray(),"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",fileName);
        }

        // UPLOAD MARKS EXCEL
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadMarksExcel(int examId,string? category,IFormFile excelFile)
        {
            if (excelFile == null || excelFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select an Excel file.";
                return RedirectToAction(nameof(Index),new
                    {
                        examId,
                        category
                    });
            }

            if (!Path.GetExtension(excelFile.FileName)
                .Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Only .xlsx Excel files are allowed.";
                return RedirectToAction(nameof(Index),
                    new
                    {
                        examId,
                        category
                    });
            }

            var exam = await _context.Exams
                .FirstOrDefaultAsync(x => x.Id == examId);

            if (exam == null)
            {
                return NotFound("Exam not found.");
            }

            using var stream = new MemoryStream();
            await excelFile.CopyToAsync(stream);
            stream.Position = 0;
            using var workbook = new XLWorkbook(stream);

            if (!workbook.Worksheets.Contains("Overall Marks") || !workbook.Worksheets.Contains("Subject Marks"))
            {
                TempData["ErrorMessage"] = "Invalid Excel file. Both 'Overall Marks' and 'Subject Marks' sheets are required.";
                return RedirectToAction(nameof(Index),
                    new
                    {
                        examId,
                        category
                    });
            }

            var overallSheet = workbook.Worksheet("Overall Marks");
            var subjectSheet = workbook.Worksheet("Subject Marks");
            var errors = new List<string>();
            int overallSuccess = 0;
            int subjectSuccess = 0;

            // 1. OVERALL MARKS
            var overallLastRow = overallSheet.LastRowUsed()?.RowNumber() ?? 0;
            for (int row = 4; row <= overallLastRow; row++)
            {
                var applicationNumber = overallSheet.Cell(row, 1)
                        .GetString()
                        .Trim();

                if (string.IsNullOrWhiteSpace(applicationNumber))
                {
                    continue;
                }

                if (!overallSheet.Cell(row, 4)
                    .TryGetValue<decimal>(out var totalMarks))
                {
                    errors.Add($"Overall row {row}: Total Marks is invalid.");
                    continue;
                }

                if (!overallSheet.Cell(row, 5)
                    .TryGetValue<decimal>(out var obtainedMarks))
                {
                    errors.Add($"Overall row {row}: Obtained Marks is invalid.");
                    continue;
                }

                if (totalMarks <= 0)
                {
                    errors.Add( $"Overall row {row}: Total Marks must be greater than zero.");
                    continue;
                }

                if (obtainedMarks < 0 || obtainedMarks > totalMarks)
                {
                    errors.Add($"Overall row {row}: Obtained Marks must be between 0 and Total Marks.");
                    continue;
                }

                var application = await _context.StudentApplications
                        .FirstOrDefaultAsync(x =>
                            x.ApplicationNumber == applicationNumber &&
                            x.ExamId == examId && x.Status == "Approved");

                if (application == null)
                {
                    errors.Add($"Overall row {row}: Application '{applicationNumber}' not found.");
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(category))
                {
                    var applicationCategory = string.IsNullOrWhiteSpace(application.Category)
                            ? "General": application.Category;

                    if (!applicationCategory.Equals( category.Trim(),StringComparison.OrdinalIgnoreCase))
                    {
                        errors.Add( $"Overall row {row}: Category does not match.");
                        continue;
                    }
                }

                var result = await _context.ExamResults
                        .FirstOrDefaultAsync(x =>
                            x.StudentApplicationId == application.Id && x.ExamId == examId);

                if (result?.IsPublished == true)
                {
                    errors.Add($"Overall row {row}: Result is already published.");
                    continue;
                }

                var percentage = Math.Round((obtainedMarks / totalMarks) * 100,2);
                if (result == null)
                {
                    result = new ExamResult
                    {
                        StudentApplicationId = application.Id,
                        ExamId = examId,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.ExamResults.Add(result);
                }

                result.TotalMarks = totalMarks;
                result.ObtainedMarks = obtainedMarks;
                result.Percentage = percentage;
                result.ResultStatus = "Completed";
                result.IsPublished = false;
                result.PublishedAt = null;
                result.UpdatedAt = DateTime.UtcNow;

                // Cutoff
                var applicationCategoryForCutoff = string.IsNullOrWhiteSpace(application.Category)
                        ? "General": application.Category;

                var cutoff = await _context.ExamCutoffs
                        .FirstOrDefaultAsync(x =>
                            x.ExamId == examId &&
                            x.Category == applicationCategoryForCutoff && x.IsActive);

                if (cutoff == null)
                {
                    result.CutoffStatus = "Pending";
                }
                else if (cutoff.CutoffType == "Percentage")
                {
                    result.CutoffStatus = percentage >= cutoff.CutoffValue
                            ? "Qualified": "Not Qualified";
                }
                else
                {
                    result.CutoffStatus = "Pending";
                }
                overallSuccess++;
            }
            await _context.SaveChangesAsync();

            // 2. SUBJECT MARKS
            var subjectLastRow = subjectSheet.LastRowUsed()?.RowNumber() ?? 0;
            for (int row = 4; row <= subjectLastRow; row++)
            {
                var applicationNumber = subjectSheet.Cell(row, 1)
                        .GetString()
                        .Trim();

                if (string.IsNullOrWhiteSpace(applicationNumber))
                {
                    continue;
                }

                var subjectName = subjectSheet.Cell(row, 3)
                        .GetString()
                        .Trim();
                if (string.IsNullOrWhiteSpace(subjectName))
                {
                    errors.Add($"Subject row {row}: Subject is required.");
                    continue;
                }

                if (!subjectSheet.Cell(row, 4)
                    .TryGetValue<decimal>(out var totalMarks))
                {
                    errors.Add($"Subject row {row}: Total Marks is invalid.");
                    continue;
                }

                if (!subjectSheet.Cell(row, 5)
                    .TryGetValue<decimal>(out var obtainedMarks))
                {
                    errors.Add($"Subject row {row}: Obtained Marks is invalid.");
                    continue;
                }

                if (totalMarks <= 0)
                {
                    errors.Add($"Subject row {row}: Total Marks must be greater than zero.");
                    continue;
                }

                if (obtainedMarks < 0 || obtainedMarks > totalMarks)
                {
                    errors.Add($"Subject row {row}: Obtained Marks must be between 0 and Total Marks.");
                    continue;
                }
                var application = await _context.StudentApplications
                        .FirstOrDefaultAsync(x =>
                            x.ApplicationNumber == applicationNumber &&
                            x.ExamId == examId && x.Status == "Approved");

                if (application == null)
                {
                    errors.Add($"Subject row {row}: Application '{applicationNumber}' not found.");
                    continue;
                }

                var validSubject = subjectName.Equals("Physics",StringComparison.OrdinalIgnoreCase)
                    && application.HasPhysics || subjectName.Equals("Chemistry",
                    StringComparison.OrdinalIgnoreCase) && application.HasChemistry
                    || subjectName.Equals("Biology",StringComparison.OrdinalIgnoreCase)
                    && application.HasBiology || subjectName.Equals("Mathematics",
                    StringComparison.OrdinalIgnoreCase) && application.HasMathematics;

                if (!validSubject)
                {
                    errors.Add($"Subject row {row}: '{subjectName}' is not a subject selected by the student.");
                    continue;
                }

                var examResult = await _context.ExamResults
                        .FirstOrDefaultAsync(x =>
                            x.StudentApplicationId == application.Id && x.ExamId == examId);

                if (examResult == null)
                {
                    examResult = new ExamResult
                    {
                        StudentApplicationId = application.Id,
                        ExamId = examId,
                        TotalMarks = 0,
                        ObtainedMarks = 0,
                        Percentage = 0,
                        ResultStatus = "Draft",
                        CutoffStatus = "Pending",
                        IsPublished = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.ExamResults.Add(examResult);
                    await _context.SaveChangesAsync();
                }

                if (examResult.IsPublished)
                {
                    errors.Add($"Subject row {row}: Result is already published.");
                    continue;
                }

                var percentage = Math.Round((obtainedMarks / totalMarks) * 100,2);
                var subjectResult = await _context.ExamResultSubjects
                        .FirstOrDefaultAsync(x =>
                            x.ExamResultId == examResult.Id &&
                            x.SubjectName == subjectName);

                if (subjectResult == null)
                {
                    subjectResult = new ExamResultSubject
                    {
                        ExamResultId = examResult.Id,
                        SubjectName = subjectName,
                        TotalMarks = totalMarks,
                        ObtainedMarks = obtainedMarks,
                        Percentage = percentage,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.ExamResultSubjects.Add(subjectResult);
                }
                else
                {
                    subjectResult.TotalMarks = totalMarks;
                    subjectResult.ObtainedMarks = obtainedMarks;
                    subjectResult.Percentage = percentage;
                    subjectResult.UpdatedAt = DateTime.UtcNow;
                }
                subjectSuccess++;
            }
            await _context.SaveChangesAsync();

            // 3. RECALCULATE OVERALL FROM SUBJECTS
            var results = await _context.ExamResults
                .Where(x => x.ExamId == examId)
                .ToListAsync();

            foreach (var result in results)
            {
                var subjects =
                    await _context.ExamResultSubjects
                        .Where(x =>
                            x.ExamResultId == result.Id)
                        .ToListAsync();

                if (!subjects.Any())
                {
                    continue;
                }
                result.TotalMarks = subjects.Sum(x => x.TotalMarks);
                result.ObtainedMarks = subjects.Sum(x => x.ObtainedMarks);

                if (result.TotalMarks > 0)
                {
                    result.Percentage = Math.Round((result.ObtainedMarks / result.TotalMarks) * 100,2);
                }

                result.ResultStatus = "Completed";
                result.IsPublished = false;
                result.PublishedAt = null;
                result.UpdatedAt = DateTime.UtcNow;
                var application = await _context.StudentApplications
                        .FirstOrDefaultAsync(x =>
                            x.Id == result.StudentApplicationId);
                if (application != null)
                {
                    var studentCategory = string.IsNullOrWhiteSpace(application.Category)
                            ? "General"
                            : application.Category;

                    var cutoff = await _context.ExamCutoffs
                            .FirstOrDefaultAsync(x =>
                                x.ExamId == examId &&
                                x.Category == studentCategory &&
                                x.IsActive);

                    if (cutoff == null)
                    {
                        result.CutoffStatus = "Pending";
                    }
                    else if (cutoff.CutoffType == "Percentage")
                    {
                        result.CutoffStatus = result.Percentage >= cutoff.CutoffValue
                                ? "Qualified"
                                : "Not Qualified";
                    }
                    else
                    {
                        result.CutoffStatus = "Pending";
                    }
                }
            }

            await _context.SaveChangesAsync();

            if (overallSuccess > 0 || subjectSuccess > 0)
            {
                TempData["SuccessMessage"] = $"Excel imported successfully. Overall: {overallSuccess}, Subjects: {subjectSuccess}.";
            }

            if (errors.Any())
            {
                TempData["ErrorMessage"] = string.Join(" | ", errors.Take(10));

                if (errors.Count > 10)
                {
                    TempData["ErrorMessage"] += $" | And {errors.Count - 10} more error(s).";
                }
            }

            return RedirectToAction( nameof(Index),
                new
                {
                    examId,
                    category
                });
        }

        // SAVE / UPDATE CUTOFF
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveCutoff(int examId,string category,
            string cutoffType,decimal cutoffValue)
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                TempData["ErrorMessage"] = "Category is required.";
                return RedirectToAction(nameof(Index),new { examId });
            }

            if (cutoffValue < 0 || cutoffValue > 100)
            {
                TempData["ErrorMessage"] = "Cut-off value must be between 0 and 100.";
                return RedirectToAction(nameof(Index),new { examId });
            }

            var exam = await _context.Exams
                .FirstOrDefaultAsync(x => x.Id == examId);

            if (exam == null)
            {
                return NotFound("Exam not found.");
            }

            var cutoff = await _context.ExamCutoffs
                .FirstOrDefaultAsync(x =>
                    x.ExamId == examId &&
                    x.Category == category);

            if (cutoff == null)
            {
                cutoff = new ExamCutoff
                {
                    ExamId = examId,
                    Category = category,
                    CutoffType = cutoffType,
                    CutoffValue = cutoffValue,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                _context.ExamCutoffs.Add(cutoff);
            }
            else
            {
                cutoff.CutoffType = cutoffType;
                cutoff.CutoffValue = cutoffValue;
                cutoff.IsActive = true;
                cutoff.UpdatedAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"{category} cut-off saved successfully.";
            return RedirectToAction(nameof(Index),new { examId });
        }

        // DELETE / DISABLE CUTOFF
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DisableCutoff(int id,int examId)
        {
            var cutoff = await _context.ExamCutoffs
                .FirstOrDefaultAsync(x => x.Id == id);

            if (cutoff == null)
            {
                return NotFound();
            }

            cutoff.IsActive = false;
            cutoff.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Cut-off disabled successfully.";
            return RedirectToAction(nameof(Index),new { examId });
        }

        // SAVE RESULT
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveResult(int applicationId,
            decimal totalMarks,decimal obtainedMarks)
        {
            var application = await _context.StudentApplications
                .FirstOrDefaultAsync(x => x.Id == applicationId);
            if (application == null)
            {
                return NotFound("Application not found.");
            }
            if (application.Status != "Approved")
            {
                TempData["ErrorMessage"] = "Only approved applications can receive results.";
                return RedirectToAction(nameof(Index),new { examId = application.ExamId });
            }
            if (totalMarks <= 0)
            {
                TempData["ErrorMessage"] = "Total marks must be greater than zero.";
                return RedirectToAction(nameof(Index),new { examId = application.ExamId });
            }
            if (obtainedMarks < 0 || obtainedMarks > totalMarks)
            {
                TempData["ErrorMessage"] = "Obtained marks must be between 0 and total marks.";
                return RedirectToAction(nameof(Index),new { examId = application.ExamId });
            }
            var percentage = Math.Round((obtainedMarks / totalMarks) * 100,2);
            var result = await _context.ExamResults
                .FirstOrDefaultAsync(x =>
                    x.StudentApplicationId == applicationId &&
                    x.ExamId == application.ExamId);

            if (result == null)
            {
                result = new ExamResult
                {
                    StudentApplicationId = applicationId,
                    ExamId = application.ExamId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.ExamResults.Add(result);
            }
            result.TotalMarks = totalMarks;
            result.ObtainedMarks = obtainedMarks;
            result.Percentage = percentage;
            result.ResultStatus = "Completed";
            result.CutoffStatus = "Pending";
            result.IsPublished = false;
            result.UpdatedAt = DateTime.UtcNow;

            // CUT-OFF CHECK
            var category = string.IsNullOrWhiteSpace(application.Category)
                ? "General"
                : application.Category;

            var cutoff = await _context.ExamCutoffs
                .FirstOrDefaultAsync(x =>
                    x.ExamId == application.ExamId &&
                    x.Category == category &&
                    x.IsActive);

            if (cutoff != null)
            {
                if (cutoff.CutoffType == "Percentage")
                {
                    result.CutoffStatus = percentage >= cutoff.CutoffValue
                            ? "Qualified"
                            : "Not Qualified";
                }
            }
            else
            {
                result.CutoffStatus = "Pending";
            }
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Result saved and cut-off evaluated successfully.";
            return RedirectToAction(nameof(Index),new { examId = application.ExamId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveSubjectResult(int applicationId,string subjectName,
            decimal totalMarks,decimal obtainedMarks)
        {
            var application = await _context.StudentApplications
                .FirstOrDefaultAsync(x => x.Id == applicationId);

            if (application == null)
            {
                return NotFound("Application not found.");
            }

            if (application.Status != "Approved")
            {
                TempData["ErrorMessage"] = "Only approved applications can receive results.";
                return RedirectToAction(nameof(Index),new { examId = application.ExamId });
            }

            if (string.IsNullOrWhiteSpace(subjectName))
            {
                TempData["ErrorMessage"] = "Subject is required.";
                return RedirectToAction(nameof(Index),new { examId = application.ExamId });
            }

            if (totalMarks <= 0)
            {
                TempData["ErrorMessage"] = "Total marks must be greater than zero.";
                return RedirectToAction(nameof(Index),new { examId = application.ExamId });
            }
            if (obtainedMarks < 0 || obtainedMarks > totalMarks)
            {
                TempData["ErrorMessage"] = "Obtained marks must be between 0 and total marks.";
                return RedirectToAction(nameof(Index),new { examId = application.ExamId });
            }

            var examResult = await _context.ExamResults
                .FirstOrDefaultAsync(x =>
                    x.StudentApplicationId == applicationId &&
                    x.ExamId == application.ExamId);

            if (examResult == null)
            {
                examResult = new ExamResult
                {
                    StudentApplicationId = applicationId,
                    ExamId = application.ExamId,
                    TotalMarks = 0,
                    ObtainedMarks = 0,
                    Percentage = 0,
                    ResultStatus = "Draft",
                    CutoffStatus = "Pending",
                    IsPublished = false,
                    CreatedAt = DateTime.UtcNow
                };
                _context.ExamResults.Add(examResult);
                await _context.SaveChangesAsync();
            }
            var percentage = Math.Round((obtainedMarks / totalMarks) * 100,2);
            var subjectResult = await _context.ExamResultSubjects
                .FirstOrDefaultAsync(x =>
                    x.ExamResultId == examResult.Id &&
                    x.SubjectName == subjectName);

            if (subjectResult == null)
            {
                subjectResult = new ExamResultSubject
                {
                    ExamResultId = examResult.Id,
                    SubjectName = subjectName,
                    TotalMarks = totalMarks,
                    ObtainedMarks = obtainedMarks,
                    Percentage = percentage,
                    CreatedAt = DateTime.UtcNow
                };
                _context.ExamResultSubjects.Add(subjectResult);
            }
            else
            {
                subjectResult.TotalMarks = totalMarks;
                subjectResult.ObtainedMarks = obtainedMarks;
                subjectResult.Percentage = percentage;
                subjectResult.UpdatedAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
            // Recalculate overall result from subjects
            var subjects = await _context.ExamResultSubjects
                .Where(x => x.ExamResultId == examResult.Id)
                .ToListAsync();

            examResult.TotalMarks = subjects.Sum(x => x.TotalMarks);
            examResult.ObtainedMarks = subjects.Sum(x => x.ObtainedMarks);
            if (examResult.TotalMarks > 0)
            {
                examResult.Percentage = Math.Round((examResult.ObtainedMarks / examResult.TotalMarks) * 100,2);
            }
            examResult.ResultStatus = "Completed";
            examResult.IsPublished = false;
            examResult.PublishedAt = null;
            examResult.UpdatedAt = DateTime.UtcNow;
            // Cutoff evaluation
            var category = string.IsNullOrWhiteSpace(application.Category)
                ? "General"
                : application.Category;

            var cutoff = await _context.ExamCutoffs
                .FirstOrDefaultAsync(x =>
                    x.ExamId == application.ExamId &&
                    x.Category == category &&
                    x.IsActive);

            if (cutoff == null)
            {
                examResult.CutoffStatus = "Pending";
            }
            else if (cutoff.CutoffType == "Percentage")
            {
                examResult.CutoffStatus = examResult.Percentage >= cutoff.CutoffValue
                        ? "Qualified"
                        : "Not Qualified";
            }
            else
            {
                examResult.CutoffStatus = "Pending";
            }
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"{subjectName} marks saved successfully.";
            return RedirectToAction(nameof(Index),new { examId = application.ExamId });
        }

        // PUBLISH RESULT
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PublishResult(int resultId)
        {
            var result = await _context.ExamResults
                .FirstOrDefaultAsync(x => x.Id == resultId);

            if (result == null)
            {
                return NotFound("Result not found.");
            }
            if (result.ResultStatus != "Completed")
            {
                TempData["ErrorMessage"] = "Complete the result before publishing.";
                return RedirectToAction(nameof(Index),new { examId = result.ExamId });
            }
            if (result.CutoffStatus == "Pending")
            {
                TempData["ErrorMessage"] = "Cut-off evaluation is still pending.";
                return RedirectToAction(nameof(Index),new { examId = result.ExamId });
            }
            result.IsPublished = true;
            result.PublishedAt = DateTime.UtcNow;
            result.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Result published successfully.";
            return RedirectToAction(nameof(Index),new { examId = result.ExamId });
        }
    }
}
using ExamManagement.Data;
using ExamManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;

namespace ExamManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ResultReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public ResultReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? examId,string? category,string? status)
        {
            var exams = await _context.Exams
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            var query = _context.ExamResults
                .Include(x => x.StudentApplication)
                .Include(x => x.Exam)
                .AsQueryable();

            if (examId.HasValue)
            {
                query = query.Where(x =>
                    x.ExamId == examId.Value);
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                if (!string.IsNullOrWhiteSpace(category))
                {
                    var selectedCategory = category.Trim();

                    query = query.Where(x =>
                        x.StudentApplication != null &&
                        (
                            selectedCategory == "General"
                                ? string.IsNullOrWhiteSpace(
                                      x.StudentApplication.Category)
                                  || x.StudentApplication.Category == "General"
                                : x.StudentApplication.Category == selectedCategory
                        ));
                }
                else
                {
                    query = query.Where(x =>
                        x.StudentApplication != null &&
                        x.StudentApplication.Category == category);
                }
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (status == "Qualified")
                {
                    query = query.Where(x =>
                        x.CutoffStatus == "Qualified");
                }
                else if (status == "Not Qualified")
                {
                    query = query.Where(x =>
                        x.CutoffStatus == "Not Qualified");
                }
                else if (status == "Pending")
                {
                    query = query.Where(x =>
                        x.CutoffStatus == "Pending");
                }
                else if (status == "Published")
                {
                    query = query.Where(x =>
                        x.IsPublished);
                }
                else if (status == "Unpublished")
                {
                    query = query.Where(x =>
                        !x.IsPublished);
                }
            }

            var results = await query
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            var model = new ResultReportViewModel
            {
                Exams = exams,
                Results = results,
                SelectedExamId = examId,
                SelectedCategory = category,
                SelectedStatus = status,
                TotalResults = results.Count,
                QualifiedCount = results.Count(x =>
                    x.CutoffStatus == "Qualified"),
                NotQualifiedCount = results.Count(x =>
                    x.CutoffStatus == "Not Qualified"),
                PendingCount = results.Count(x =>
                    x.CutoffStatus == "Pending"),
                PublishedCount = results.Count(x =>
                    x.IsPublished),
                UnpublishedCount = results.Count(x =>
                    !x.IsPublished)
            };
            return View(model);
        }
        [HttpGet]
        public async Task<IActionResult> ExportExcel(int? examId,string? category,string? status)
        {
            var query = _context.ExamResults
                .Include(x => x.StudentApplication)
                .Include(x => x.Exam)
                .AsQueryable();

            if (examId.HasValue)
            {
                query = query.Where(x =>
                    x.ExamId == examId.Value);
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                if (!string.IsNullOrWhiteSpace(category))
                {
                    var selectedCategory = category.Trim();
                    query = query.Where(x =>
                        x.StudentApplication != null &&
                        (
                            selectedCategory == "General"
                                ? string.IsNullOrWhiteSpace(x.StudentApplication.Category)
                                  || x.StudentApplication.Category == "General"
                                : x.StudentApplication.Category == selectedCategory
                        ));
                }
                else
                {
                    query = query.Where(x =>
                        x.StudentApplication != null &&
                        x.StudentApplication.Category == category);
                }
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (status == "Qualified")
                {
                    query = query.Where(x =>
                        x.CutoffStatus == "Qualified");
                }
                else if (status == "Not Qualified")
                {
                    query = query.Where(x =>
                        x.CutoffStatus == "Not Qualified");
                }
                else if (status == "Pending")
                {
                    query = query.Where(x =>
                        x.CutoffStatus == "Pending");
                }
                else if (status == "Published")
                {
                    query = query.Where(x =>
                        x.IsPublished);
                }
                else if (status == "Unpublished")
                {
                    query = query.Where(x =>
                        !x.IsPublished);
                }
            }

            var results = await query
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Result Report");

            // TITLE
            worksheet.Cell(1, 1).Value = "Examination Result Report";
            worksheet.Range(1, 1, 1, 11).Merge();
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 16;
            worksheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // HEADERS
            var headers = new[]
            {
            "Sr. No.",
            "Student Name",
            "Application Number",
            "Exam",
            "Category",
            "Total Marks",
            "Obtained Marks",
            "Percentage",
            "Cutoff Status",
            "Result Status",
            "Published"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(3, i + 1).Value = headers[i];
            }
            var headerRange = worksheet.Range(3, 1, 3, headers.Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // DATA
            int row = 4;
            int serialNumber = 1;

            foreach (var result in results)
            {
                var studentName = result.StudentApplication?.FullName ?? "N/A";
                var applicationNumber = result.StudentApplication?.ApplicationNumber ?? "N/A";
                var categoryName =
                    string.IsNullOrWhiteSpace(result.StudentApplication?.Category)
                        ? "General"
                        : result.StudentApplication!.Category;
                worksheet.Cell(row, 1).Value = serialNumber;
                worksheet.Cell(row, 2).Value = studentName;
                worksheet.Cell(row, 3).Value = applicationNumber;
                worksheet.Cell(row, 4).Value = result.Exam?.ExamName ?? "N/A";
                worksheet.Cell(row, 5).Value = categoryName;
                worksheet.Cell(row, 6).Value = (double)result.TotalMarks;
                worksheet.Cell(row, 7).Value = (double)result.ObtainedMarks;
                worksheet.Cell(row, 8).Value = (double)result.Percentage;
                worksheet.Cell(row, 9).Value = result.CutoffStatus;
                worksheet.Cell(row, 10).Value = result.ResultStatus;
                worksheet.Cell(row, 11).Value = result.IsPublished ? "Published" : "Unpublished";
                row++;
                serialNumber++;
            }

            // NUMBER FORMATTING
            if (row > 4)
            {
                worksheet.Range(4, 6, row - 1, 8).Style.NumberFormat.Format = "0.00";
            }

            // BORDERS
            var usedRange = worksheet.Range(3,1,Math.Max(row - 1, 3),headers.Length);
            usedRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            usedRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            // ALIGNMENT
            worksheet.Range(3,1,Math.Max(row - 1, 3),headers.Length)
                .Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Columns().AdjustToContents();

            // Freeze header
            worksheet.SheetView.FreezeRows(3);
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;
            var fileName = $"ResultReport-{DateTime.Now:yyyyMMddHHmmss}.xlsx";
            return File(stream.ToArray(),"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",fileName);
        }
    }
}
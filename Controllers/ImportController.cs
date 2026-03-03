using Attendance.Models.Entities;
using Attendance.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Attendance.Controllers;

[Authorize(Policy = PermissionKeys.DatabaseManagement)]
public class ImportController : Controller
{
    private readonly IStudentService _studentService;
    public ImportController(IStudentService s) => _studentService = s;

    public IActionResult Index() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(IFormFile excelFile)
    {
        if (excelFile == null || excelFile.Length == 0)
        {
            ModelState.AddModelError("", "Please select a valid Excel file.");
            return View();
        }

        if (!excelFile.FileName.EndsWith(".xlsx"))
        {
            ModelState.AddModelError("", "Only .xlsx files are supported.");
            return View();
        }

        using var stream = excelFile.OpenReadStream();
        var (success, total, message) = await _studentService.ImportStudentsFromExcelAsync(stream);
        
        if (success > 0) TempData["Success"] = $"Successfully imported {success} out of {total} students.";
        else TempData["Error"] = "Failed to import students. " + message;

        return RedirectToAction("Index", "Students");
    }
}

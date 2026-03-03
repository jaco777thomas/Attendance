using Attendance.Models.Entities;
using Attendance.Models.ViewModels;
using Attendance.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Attendance.Controllers;

[Authorize(Policy = PermissionKeys.ManageStudents)]
public class PromotionController : Controller
{
    private readonly IPromotionService _promotionService;
    private readonly string _finalClass;

    public PromotionController(IPromotionService p, IConfiguration config)
    {
        _promotionService = p;
        _finalClass = config["AppSettings:FinalClass"] ?? "15";
    }

    public async Task<IActionResult> Index(string? filterClass, Division? filterDivision, Language? filterLanguage, bool promoteAll = false)
    {
        var students = await _promotionService.GetPromotionPreviewAsync(filterClass, filterDivision, filterLanguage, promoteAll, _finalClass);
        var vm = new PromotionViewModel
        {
            FilterClass = filterClass, FilterDivision = filterDivision, FilterLanguage = filterLanguage,
            PromoteAll = promoteAll,
            PreviewStudents = students, FinalClass = _finalClass,
            ShowFinalClassWarning = students.Any(s => s.Class == _finalClass)
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Promote([FromBody] PromoteRequest req)
    {
        if (req.StudentIds == null || req.StudentIds.Count == 0)
            return Json(new { success = false, message = "No students selected." });
        var count = await _promotionService.PromoteStudentsAsync(req.StudentIds, _finalClass, req.MarkFinalAsInactive);
        return Json(new { success = true, count, message = $"{count} student(s) processed successfully." });
    }
}

public class PromoteRequest
{
    public List<int> StudentIds { get; set; } = new();
    public bool MarkFinalAsInactive { get; set; } = true;
}

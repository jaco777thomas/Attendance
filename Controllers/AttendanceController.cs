using Attendance.Models.Entities;
using Attendance.Models.ViewModels;
using Attendance.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Attendance.Controllers;

[Authorize]
public class AttendanceController : Controller
{
    private readonly IAttendanceService _attendanceService;
    private readonly IStudentService _studentService;
    private readonly IUserService _userService;

    public AttendanceController(IAttendanceService a, IStudentService s, IUserService u)
    { _attendanceService = a; _studentService = s; _userService = u; }

    // ---- Student Attendance ----
    [Authorize(Policy = PermissionKeys.TakeStudentAttendance)]
    public async Task<IActionResult> Students()
    {
        var roleClaims = User.FindAll(ClaimTypes.Role);
        UserRole role = 0;
        foreach (var c in roleClaims) if (Enum.TryParse<UserRole>(c.Value, true, out var r)) role |= r;

        bool isRestricted = role.HasFlag(UserRole.Teacher) && !role.HasFlag(UserRole.HeadMaster) && !role.HasFlag(UserRole.AttendanceIncharge) && !role.HasFlag(UserRole.StudentManager) && !role.HasFlag(UserRole.Admin);
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _userService.GetByIdAsync(userId);

        var vm = new AttendanceMarkViewModel { Date = DateTime.Today };
        if (isRestricted && user != null)
        {
            vm.Class = user.AssignedClass ?? "";
            vm.Division = user.AssignedDivision ?? Division.A;
            vm.Language = user.AssignedLanguage ?? Language.English;
        }
        return View(vm);
    }

    [HttpGet, Authorize(Policy = PermissionKeys.TakeStudentAttendance)]
    public async Task<IActionResult> LoadStudents(string classNo, Division division, Language language, DateTime date)
    {
        var roleClaims = User.FindAll(ClaimTypes.Role);
        UserRole role = 0;
        foreach (var c in roleClaims) if (Enum.TryParse<UserRole>(c.Value, true, out var r)) role |= r;

        // Restriction check for Teachers
        bool isRestricted = role.HasFlag(UserRole.Teacher) && !role.HasFlag(UserRole.HeadMaster) && !role.HasFlag(UserRole.AttendanceIncharge) && !role.HasFlag(UserRole.StudentManager) && !role.HasFlag(UserRole.Admin);
        if (isRestricted)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _userService.GetByIdAsync(userId);
            if (user != null && (user.AssignedClass != classNo || user.AssignedDivision != division || user.AssignedLanguage != language))
                return Json(new { success = false, message = "Not authorized for this class/division/language" });
        }

        var students = await _studentService.GetForAttendanceAsync(classNo, division, language, date);
        var studentIds = students.Select(s => s.Id).ToList();
        var existing = await _attendanceService.GetStudentAttendanceAsync(studentIds, date);
        var rows = students.Select(s => new StudentAttendanceRow
        {
            StudentId = s.Id, StudentName = s.Name,
            Status = existing.ContainsKey(s.Id) ? existing[s.Id] : AttendanceStatus.Present
        }).ToList();
        return Json(rows);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = PermissionKeys.TakeStudentAttendance)]
    public async Task<IActionResult> SaveStudentAttendance([FromBody] SaveAttendanceRequest req)
    {
        if (req == null || req.Rows == null || req.Rows.Count == 0)
            return Json(new { success = false, message = "No rows provided or invalid data." });
        await _attendanceService.SaveStudentAttendanceAsync(req.Date, req.Rows);
        return Json(new { success = true });
    }

    // ---- Staff Attendance ----
    [Authorize(Policy = PermissionKeys.TakeTeacherAttendance)]
    public async Task<IActionResult> Staff(DateTime? date)
    {
        date ??= DateTime.Today;
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        
        var roleClaims = User.FindAll(ClaimTypes.Role);
        UserRole role = 0;
        foreach (var c in roleClaims) if (Enum.TryParse<UserRole>(c.Value, true, out var r)) role |= r;

        bool isAdmin = role.HasFlag(UserRole.HeadMaster) || role.HasFlag(UserRole.AttendanceIncharge) || role.HasFlag(UserRole.NoticeManager) || role.HasFlag(UserRole.StudentManager) || role.HasFlag(UserRole.DBAdmin) || role.HasFlag(UserRole.Admin);

        List<User> staff;
        if (isAdmin)
            staff = (await _userService.GetAllAsync()).Where(u => u.IsActive).ToList();
        else
        {
            var self = await _userService.GetByIdAsync(userId);
            staff = self != null ? new List<User> { self } : new List<User>();
        }

        var existing = await _attendanceService.GetStaffAttendanceAsync(date.Value);
        var rows = staff.Where(t => t != null).Select(t => new StaffAttendanceRow
        {
            UserId = t.Id, StaffName = t.Name,
            Status = existing.ContainsKey(t.Id) ? existing[t.Id] : AttendanceStatus.Present,
            CanEdit = isAdmin || t.Id == userId
        }).ToList();

        return View(new StaffAttendanceMarkViewModel { Date = date.Value, Rows = rows });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = PermissionKeys.TakeTeacherAttendance)]
    public async Task<IActionResult> SaveStaffAttendance(StaffAttendanceMarkViewModel vm)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _attendanceService.SaveStaffAttendanceAsync(vm.Date, userId, vm.Rows);
        TempData["Success"] = "Staff attendance saved.";
        return RedirectToAction(nameof(Staff), new { date = vm.Date.ToString("yyyy-MM-dd") });
    }
}

public class SaveAttendanceRequest
{
    public DateTime Date { get; set; }
    public List<StudentAttendanceRow> Rows { get; set; } = new();
}

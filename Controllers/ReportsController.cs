using Attendance.Models.Entities;
using Attendance.Models.ViewModels;
using Attendance.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Attendance.Controllers;

[Authorize]
[Authorize(Policy = PermissionKeys.ViewReports)]
public class ReportsController : Controller
{
    private readonly IAttendanceService _attendanceService;
    private readonly IStudentService _studentService;
    private readonly IUserService _userService;
    private readonly IExportService _exportService;

    public ReportsController(IAttendanceService a, IStudentService s, IUserService u, IExportService e)
    { _attendanceService = a; _studentService = s; _userService = u; _exportService = e; }

    public async Task<IActionResult> Index(ReportFilterViewModel filter)
    {
        string? teacherClass = null;
        Division? teacherDiv = null;
        Language? teacherLang = null;
        int? restrictedTeacherId = null;

        var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var roleClaims = User.FindAll(ClaimTypes.Role);
        UserRole role = 0;
        foreach (var c in roleClaims) if (Enum.TryParse<UserRole>(c.Value, true, out var r)) role |= r;

        bool isRestricted = role.HasFlag(UserRole.Teacher) && !role.HasFlag(UserRole.HeadMaster) && !role.HasFlag(UserRole.AttendanceIncharge) && !role.HasFlag(UserRole.StudentManager) && !role.HasFlag(UserRole.Admin);
        if (isRestricted)
        {
            var user = await _userService.GetByIdAsync(currentUserId);
            if (user != null)
            {
                teacherClass = user.AssignedClass;
                teacherDiv = user.AssignedDivision;
                teacherLang = user.AssignedLanguage;
                restrictedTeacherId = user.Id;
                
                // Force filters for Teacher
                filter.Class = teacherClass;
                filter.Division = teacherDiv;
                filter.Language = teacherLang;
                if (filter.ReportType == "Staff") filter.TeacherId = restrictedTeacherId;
            }
        }

        var students = await _studentService.GetAllAsync(teacherClass, teacherDiv, teacherLang, null, null);
        var staff = await _userService.GetAllAsync();

        var vm = new ReportViewModel { Filter = filter, Students = students, Teachers = staff };

        if (filter.FromDate.HasValue || filter.Month.HasValue)
        {
            var from = filter.Month.HasValue && filter.Year.HasValue
                ? new DateTime(filter.Year.Value, filter.Month.Value, 1)
                : (filter.FromDate ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1));
            var to = filter.Month.HasValue && filter.Year.HasValue
                ? new DateTime(filter.Year.Value, filter.Month.Value, DateTime.DaysInMonth(filter.Year.Value, filter.Month.Value))
                : (filter.ToDate ?? DateTime.Today);

            if (filter.ReportType == "Staff")
            {
                var targetUserId = isRestricted ? restrictedTeacherId : filter.TeacherId;
                var records = await _attendanceService.GetStaffAttendanceRangeAsync(targetUserId, from, to);
                vm.StaffRows = records.GroupBy(r => new { r.UserId, r.User.Name })
                    .Select(g => new StaffAttendanceReportRow
                    {
                        UserId = g.Key.UserId, StaffName = g.Key.Name,
                        TotalDays = g.Count(), PresentDays = g.Count(r => r.Status == AttendanceStatus.Present)
                    }).ToList();
            }
            else
            {
                var targetClass = isRestricted ? teacherClass : (string.IsNullOrEmpty(filter.Class) ? null : filter.Class);
                var targetDiv = isRestricted ? teacherDiv : filter.Division;
                var targetLang = isRestricted ? teacherLang : filter.Language;

                var activeStudents = await _studentService.GetAllAsync(targetClass, targetDiv, targetLang, StudentStatus.Active, null);
                var records = await _attendanceService.GetStudentAttendanceRangeAsync(filter.StudentId, targetClass, targetDiv, targetLang, from, to);
                
                // Total sessions: distinct dates in the attendance table for this group in this range
                var totalSessions = records.Select(r => r.Date.Date).Distinct().Count();

                vm.StudentRows = activeStudents.Select(s => {
                    var studentRecords = records.Where(r => r.StudentId == s.Id).ToList();
                    var presentDays = studentRecords.Count(r => r.Status == AttendanceStatus.Present);
                    return new StudentAttendanceReportRow
                    {
                        StudentId = s.Id, StudentName = s.Name,
                        Class = s.Class, Division = s.Division ?? Division.A, Language = s.Language ?? Language.English,
                        TotalDays = totalSessions, PresentDays = presentDays
                    };
                }).OrderBy(r => r.Class).ThenBy(r => r.StudentName).ToList();
            }
        }
        return View(vm);
    }

    [Authorize(Policy = PermissionKeys.ExportReports)]
    public async Task<IActionResult> ExportExcel(ReportFilterViewModel filter)
    {
        await ApplyRestrictionsAsync(filter);
        if (filter.ReportType == "Staff")
        {
            var data = await _exportService.ExportStaffAttendanceExcelAsync(filter);
            return File(data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "StaffAttendance.xlsx");
        }
        else
        {
            var data = await _exportService.ExportStudentAttendanceExcelAsync(filter);
            return File(data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "StudentAttendance.xlsx");
        }
    }

    [Authorize(Policy = PermissionKeys.ExportReports)]
    public async Task<IActionResult> ExportPdf(ReportFilterViewModel filter)
    {
        await ApplyRestrictionsAsync(filter);
        if (filter.ReportType == "Staff")
        {
            var data = await _exportService.ExportStaffAttendancePdfAsync(filter);
            return File(data, "application/pdf", "StaffAttendance.pdf");
        }
        else
        {
            var data = await _exportService.ExportStudentAttendancePdfAsync(filter);
            return File(data, "application/pdf", "StudentAttendance.pdf");
        }
    }

    private async Task ApplyRestrictionsAsync(ReportFilterViewModel filter)
    {
        var roleClaims = User.FindAll(ClaimTypes.Role);
        UserRole role = 0;
        foreach (var c in roleClaims) if (Enum.TryParse<UserRole>(c.Value, true, out var r)) role |= r;

        bool isRestricted = role.HasFlag(UserRole.Teacher) && !role.HasFlag(UserRole.HeadMaster) && !role.HasFlag(UserRole.AttendanceIncharge) && !role.HasFlag(UserRole.StudentManager) && !role.HasFlag(UserRole.Admin);
        if (isRestricted)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _userService.GetByIdAsync(userId);
            if (user != null)
            {
                filter.Class = user.AssignedClass;
                filter.Division = user.AssignedDivision;
                filter.Language = user.AssignedLanguage;
                if (filter.ReportType == "Staff") filter.TeacherId = user.Id;
            }
        }
    }
}

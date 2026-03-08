using Attendance.Models.Entities;
using Attendance.Models.ViewModels;
using Attendance.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Attendance.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IUserService _userService;
    private readonly IStudentService _studentService;
    private readonly IAttendanceService _attendanceService;
    private readonly INoticeService _noticeService;

    public DashboardController(IUserService u, IStudentService s, IAttendanceService a, INoticeService n)
    { _userService = u; _studentService = s; _attendanceService = a; _noticeService = n; }

    public async Task<IActionResult> Index()
    {
        var roleClaims = User.FindAll(ClaimTypes.Role).Select(c => c.Value);
        UserRole aggregateRole = UserRole.None;
        foreach (var rName in roleClaims) 
        {
            if (Enum.TryParse<UserRole>(rName, true, out var rEnum)) aggregateRole |= rEnum;
        }
        var role = aggregateRole;

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        bool isRestricted = role.HasFlag(UserRole.Teacher) && !role.HasFlag(UserRole.HeadMaster) && !role.HasFlag(UserRole.AttendanceIncharge) && !role.HasFlag(UserRole.StudentManager) && !role.HasFlag(UserRole.Admin);

        string? fClass = null; Division? fDiv = null; Language? fLang = null;
        if (isRestricted)
        {
            var user = await _userService.GetByIdAsync(userId);
            if (user != null) { fClass = user.AssignedClass; fDiv = user.AssignedDivision; fLang = user.AssignedLanguage; }
        }

        var allActiveStudents = await _studentService.GetAllAsync(fClass, fDiv, fLang, StudentStatus.Active, null);
        var totalStudents = await _studentService.GetAllAsync(fClass, fDiv, fLang, null, null);
        List<User> staffs;
        if (isRestricted)
        {
            var self = await _userService.GetByIdAsync(userId);
            staffs = self != null ? new List<User> { self } : new List<User>();
        }
        else
        {
            staffs = await _userService.GetAllAsync();
        }
        var notices = await _noticeService.GetForRoleAsync(role);

        // Attendance stats
        int presentToday = 0, absentToday = 0;
        var today = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified);

        if (isRestricted && !string.IsNullOrEmpty(fClass))
        {
            var ids = allActiveStudents.Select(s => s.Id).ToList();
            if (ids.Any()) {
                var att = await _attendanceService.GetStudentAttendanceAsync(ids, today);
                presentToday = att.Values.Count(v => v == AttendanceStatus.Present);
                absentToday = att.Values.Count(v => v == AttendanceStatus.Absent);
            }
        }
        else
        {
            var summary = await _attendanceService.GetTodaySummaryAsync();
            presentToday = summary.Present; absentToday = summary.Absent;
        }

        var (presentStaff, absentStaff) = await _attendanceService.GetStaffTodaySummaryAsync();
        var staffAttendance = await _attendanceService.GetStaffAttendanceAsync(today);

        List<string> absentStudentNames = new();
        if (isRestricted && !string.IsNullOrEmpty(fClass))
        {
            var ids = allActiveStudents.Select(s => s.Id).ToList();
            absentStudentNames = await _attendanceService.GetAbsentStudentNamesAsync(ids, today);
        }
        else
        {
            absentStudentNames = await _attendanceService.GetAbsentStudentNamesAsync(null, today);
        }

        var absentStaffNames = await _attendanceService.GetAbsentStaffNamesAsync(today);

        var vm = new DashboardViewModel
        {
            TotalStudents = totalStudents.Count,
            ActiveStudents = allActiveStudents.Count,
            TotalTeachers = staffs.Count,
            PresentStudentsToday = presentToday,
            AbsentStudentsToday = absentToday,
            PresentTeachersToday = presentStaff,
            AbsentTeachersToday = absentStaff,
            AbsentStudentNames = absentStudentNames,
            AbsentTeacherNames = absentStaffNames,
            RecentNotices = notices.Take(5).ToList(),
            AttendanceMarkedToday = staffAttendance.ContainsKey(userId)
        };
        ViewBag.Role = role;
        return View(vm);
    }
}

using Attendance.Models.Entities;
using Attendance.Models.ViewModels;
using Attendance.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Attendance.Controllers;

[Authorize]
[Authorize(Policy = PermissionKeys.ViewStudents)]
public class StudentsController : Controller
{
    private readonly IStudentService _studentService;
    private readonly IUserService _userService;
    public StudentsController(IStudentService s, IUserService u) { _studentService = s; _userService = u; }

    public async Task<IActionResult> Index(string? filterClass, Division? filterDivision, Language? filterLanguage, StudentStatus? filterStatus, string? search)
    {
        var roleClaims = User.FindAll(ClaimTypes.Role);
        UserRole role = 0;
        foreach (var c in roleClaims) if (Enum.TryParse<UserRole>(c.Value, true, out var r)) role |= r;

        bool isRestricted = role.HasFlag(UserRole.Teacher) && !role.HasFlag(UserRole.HeadMaster) && !role.HasFlag(UserRole.AttendanceIncharge) && !role.HasFlag(UserRole.StudentManager) && !role.HasFlag(UserRole.Admin);
        if (isRestricted)
        {
            var userId = int.Parse(User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier)!);
            var user = await _userService.GetByIdAsync(userId);
            if (user != null)
            {
                filterClass = user.AssignedClass;
                filterDivision = user.AssignedDivision;
                filterLanguage = user.AssignedLanguage;
            }
        }

        var students = await _studentService.GetAllAsync(filterClass, filterDivision, filterLanguage, filterStatus, search);
        var vm = new StudentListViewModel
        {
            Students = students, FilterClass = filterClass,
            FilterDivision = filterDivision, FilterLanguage = filterLanguage,
            FilterStatus = filterStatus,
            SearchName = search, TotalCount = students.Count
        };
        return View(vm);
    }

    [Authorize(Policy = PermissionKeys.ManageStudents)]
    public IActionResult Create() => View(new StudentCreateViewModel { AcademicYear = DateTime.Now.Year });

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = PermissionKeys.ManageStudents)]
    public async Task<IActionResult> Create(StudentCreateViewModel vm)
    {
        var roleClaims = User.FindAll(ClaimTypes.Role);
        UserRole role = 0;
        foreach (var c in roleClaims) if (Enum.TryParse<UserRole>(c.Value, true, out var r)) role |= r;

        bool isRestricted = role.HasFlag(UserRole.Teacher) && !role.HasFlag(UserRole.HeadMaster) && !role.HasFlag(UserRole.AttendanceIncharge) && !role.HasFlag(UserRole.StudentManager) && !role.HasFlag(UserRole.Admin);
        if (isRestricted)
        {
            var userId = int.Parse(User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier)!);
            var user = await _userService.GetByIdAsync(userId);
            if (user != null)
            {
                 vm.Class = user.AssignedClass ?? vm.Class;
                 vm.Division = user.AssignedDivision ?? vm.Division;
                 vm.Language = user.AssignedLanguage ?? vm.Language;
            }
        }

        if (!ModelState.IsValid) return View(vm);
        await _studentService.CreateAsync(vm);
        TempData["Success"] = "Student added successfully.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = PermissionKeys.ManageStudents)]
    public async Task<IActionResult> Edit(int id)
    {
        var s = await _studentService.GetByIdAsync(id);
        if (s == null) return NotFound();

        var roleClaims = User.FindAll(ClaimTypes.Role);
        UserRole role = 0;
        foreach (var c in roleClaims) if (Enum.TryParse<UserRole>(c.Value, true, out var r)) role |= r;

        bool isRestricted = role.HasFlag(UserRole.Teacher) && !role.HasFlag(UserRole.HeadMaster) && !role.HasFlag(UserRole.AttendanceIncharge) && !role.HasFlag(UserRole.StudentManager) && !role.HasFlag(UserRole.Admin);
        if (isRestricted)
        {
            var userId = int.Parse(User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier)!);
            var user = await _userService.GetByIdAsync(userId);
            if (user != null && (s.Class != user.AssignedClass || s.Division != user.AssignedDivision || s.Language != user.AssignedLanguage))
            {
                 TempData["Error"] = "Not authorized to edit this student.";
                 return RedirectToAction(nameof(Index));
            }
        }

        return View(new StudentEditViewModel
        {
            Id = s.Id, Name = s.Name, Class = s.Class, Division = s.Division ?? Division.A,
            Language = s.Language,
            DateOfBirth = s.DateOfBirth, Address = s.Address, FatherName = s.FatherName,
            MotherName = s.MotherName, Phone = s.Phone, Status = s.Status, AcademicYear = s.AcademicYear
        });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = PermissionKeys.ManageStudents)]
    public async Task<IActionResult> Edit(StudentEditViewModel vm)
    {
        var roleClaims = User.FindAll(ClaimTypes.Role);
        UserRole role = 0;
        foreach (var c in roleClaims) if (Enum.TryParse<UserRole>(c.Value, true, out var r)) role |= r;

        bool isRestricted = role.HasFlag(UserRole.Teacher) && !role.HasFlag(UserRole.HeadMaster) && !role.HasFlag(UserRole.AttendanceIncharge) && !role.HasFlag(UserRole.StudentManager) && !role.HasFlag(UserRole.Admin);
        if (isRestricted)
        {
            var userId = int.Parse(User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier)!);
            var user = await _userService.GetByIdAsync(userId);
            if (user != null && (vm.Class != user.AssignedClass || vm.Division != user.AssignedDivision || vm.Language != user.AssignedLanguage))
            {
                 ModelState.AddModelError("", "You cannot move a student outside your assigned class/division.");
                 return View(vm);
            }
        }

        if (!ModelState.IsValid) return View(vm);
        await _studentService.UpdateAsync(vm);
        TempData["Success"] = "Student updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = PermissionKeys.ManageStudents)]
    public async Task<IActionResult> Delete(int id)
    {
        var s = await _studentService.GetByIdAsync(id);
        if (s == null) return NotFound();
        return View(s);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = PermissionKeys.ManageStudents), ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _studentService.DeleteAsync(id);
        TempData["Success"] = "Student deleted.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, Authorize(Policy = PermissionKeys.ManageStudents)]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        await _studentService.ToggleStatusAsync(id);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, Authorize(Policy = PermissionKeys.ManageStudents)]
    public async Task<IActionResult> BulkStatus([FromBody] BulkStatusRequest req)
    {
        var count = await _studentService.BulkStatusUpdateAsync(req.Ids, req.Status);
        return Json(new { success = true, count });
    }
}

public class BulkStatusRequest
{
    public List<int> Ids { get; set; } = new();
    public StudentStatus Status { get; set; }
}

using Attendance.Models.Entities;
using Attendance.Models.ViewModels;
using Attendance.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Attendance.Controllers;

[Authorize(Policy = PermissionKeys.UserManagement)]
public class UsersController : Controller
{
    private readonly IUserService _userService;
    private readonly IRoleService _roleService;
    public UsersController(IUserService u, IRoleService r) {
        _userService = u;
        _roleService = r;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _userService.GetAllAsync();
        if (!User.IsInRole("HeadMaster"))
             users = users.Where(u => !u.UserRoles.Any(ur => ur.Role.Name == "HeadMaster")).ToList();
        return View(users);
    }

    public async Task<IActionResult> Create()
    {
        var roles = await _roleService.GetAllRolesAsync();
        return View(new UserCreateViewModel { AvailableRoles = roles.ToList() });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserCreateViewModel vm)
    {
        var roles = await _roleService.GetAllRolesAsync();
        var selectedRoles = roles.Where(r => vm.SelectedRoleIds.Contains(r.Id)).ToList();

        if (selectedRoles.Any(r => r.Name == "HeadMaster") && !User.IsInRole("HeadMaster"))
        {
            ModelState.AddModelError("SelectedRoleIds", "Only Headmasters can create other Headmasters.");
            vm.AvailableRoles = roles.ToList();
            return View(vm);
        }

        if (!ModelState.IsValid) { vm.AvailableRoles = roles.ToList(); return View(vm); }
        var (success, error) = await _userService.CreateAsync(vm);
        if (!success) { ModelState.AddModelError("Email", error); vm.AvailableRoles = roles.ToList(); return View(vm); }
        TempData["Success"] = "User created successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var u = await _userService.GetByIdAsync(id);
        if (u == null) return NotFound();

        if (u.UserRoles.Any(ur => ur.Role.Name == "HeadMaster") && !User.IsInRole("HeadMaster"))
        {
            TempData["Error"] = "You do not have permission to edit this user.";
            return RedirectToAction(nameof(Index));
        }

        var roles = await _roleService.GetAllRolesAsync();

        return View(new UserEditViewModel { 
            Id = u.Id, Name = u.Name, Email = u.Email, Phone = u.Phone, 
            SelectedRoleIds = u.UserRoles.Select(ur => ur.RoleId).ToList(), IsActive = u.IsActive,
            AssignedClass = u.AssignedClass, AssignedDivision = u.AssignedDivision,
            AssignedLanguage = u.AssignedLanguage,
            AvailableRoles = roles.ToList()
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserEditViewModel vm)
    {
        var u = await _userService.GetByIdAsync(vm.Id);
        if (u == null) return NotFound();

        if (u.UserRoles.Any(ur => ur.Role.Name == "HeadMaster") && !User.IsInRole("HeadMaster"))
            return Forbid();

        var roles = await _roleService.GetAllRolesAsync();
        var selectedRoles = roles.Where(r => vm.SelectedRoleIds.Contains(r.Id)).ToList();

        if (selectedRoles.Any(r => r.Name == "HeadMaster") && !User.IsInRole("HeadMaster"))
        {
            ModelState.AddModelError("SelectedRoleIds", "You cannot promote a user to Headmaster.");
            vm.AvailableRoles = roles.ToList();
            return View(vm);
        }

        if (!ModelState.IsValid) { vm.AvailableRoles = roles.ToList(); return View(vm); }
        await _userService.UpdateAsync(vm);
        TempData["Success"] = "User updated.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = PermissionKeys.UserManagement)]
    public async Task<IActionResult> Delete(int id)
    {
        var u = await _userService.GetByIdAsync(id);
        if (u == null) return NotFound();

        if (u.UserRoles.Any(ur => ur.Role.Name == "HeadMaster") && !User.IsInRole("HeadMaster"))
            return Forbid();

        return View(u);
    }

    [HttpPost, ValidateAntiForgeryToken, ActionName("Delete"), Authorize(Policy = PermissionKeys.UserManagement)]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var u = await _userService.GetByIdAsync(id);
        if (u == null) return NotFound();

        if (u.UserRoles.Any(ur => ur.Role.Name == "HeadMaster") && !User.IsInRole("HeadMaster"))
             return Forbid();

        await _userService.DeleteAsync(id);
        TempData["Success"] = "User deleted.";
        return RedirectToAction(nameof(Index));
    }
}

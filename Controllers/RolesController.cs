using Attendance.Models.Entities;
using Attendance.Models.ViewModels;
using Attendance.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Attendance.Controllers;

[Authorize(Policy = PermissionKeys.RoleManagement)]
public class RolesController : Controller
{
    private readonly IRoleService _roleService;

    public RolesController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    public async Task<IActionResult> Index()
    {
        var roles = await _roleService.GetAllRolesAsync();
        var vm = roles.Select(r => new RoleIndexViewModel
        {
            Id = r.Id,
            Name = r.Name,
            Description = r.Description,
            PermissionCount = r.RolePermissions.Count
        }).ToList();
        return View(vm);
    }

    public async Task<IActionResult> Create()
    {
        var permissions = await _roleService.GetAllPermissionsAsync();
        var vm = new RoleEditViewModel
        {
            AvailablePermissions = permissions.Select(p => new PermissionViewModel
            {
                Id = p.Id, Name = p.Name, Key = p.Key
            }).ToList()
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RoleEditViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var permissions = await _roleService.GetAllPermissionsAsync();
            vm.AvailablePermissions = permissions.Select(p => new PermissionViewModel { Id = p.Id, Name = p.Name, Key = p.Key }).ToList();
            return View(vm);
        }

        await _roleService.CreateRoleAsync(vm.Name, vm.Description, vm.SelectedPermissionIds);
        TempData["Success"] = "Role created successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var role = await _roleService.GetByIdAsync(id);
        if (role == null) return NotFound();

        var permissions = await _roleService.GetAllPermissionsAsync();
        var vm = new RoleEditViewModel
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            SelectedPermissionIds = role.RolePermissions.Select(rp => rp.PermissionId).ToList(),
            AvailablePermissions = permissions.Select(p => new PermissionViewModel { Id = p.Id, Name = p.Name, Key = p.Key }).ToList()
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(RoleEditViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var permissions = await _roleService.GetAllPermissionsAsync();
            vm.AvailablePermissions = permissions.Select(p => new PermissionViewModel { Id = p.Id, Name = p.Name, Key = p.Key }).ToList();
            return View(vm);
        }

        await _roleService.UpdateRoleAsync(vm.Id, vm.Name, vm.Description, vm.SelectedPermissionIds);
        TempData["Success"] = "Role updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await _roleService.DeleteRoleAsync(id);
        TempData["Success"] = "Role deleted.";
        return RedirectToAction(nameof(Index));
    }
}

using Attendance.Models.Entities;
using Attendance.Models.ViewModels;
using Attendance.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Attendance.Controllers;

[Authorize]
public class NoticesController : Controller
{
    private readonly INoticeService _noticeService;
    public NoticesController(INoticeService n) => _noticeService = n;

    public async Task<IActionResult> Index()
    {
        var roleClaims = User.FindAll(ClaimTypes.Role);
        UserRole role = 0;
        foreach (var c in roleClaims)
        {
            if (Enum.TryParse<UserRole>(c.Value, true, out var r)) role |= r;
        }
        var notices = await _noticeService.GetForRoleAsync(role);
        return View(notices);
    }

    [Authorize(Policy = PermissionKeys.ManageNotifications)]
    public IActionResult Create() => View(new NoticeCreateViewModel());
    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = PermissionKeys.ManageNotifications)]
    public async Task<IActionResult> Create(NoticeCreateViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _noticeService.CreateAsync(vm, userId);
        TempData["Success"] = "Notice posted.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = PermissionKeys.ManageNotifications)]
    public async Task<IActionResult> Edit(int id)
    {
        var n = await _noticeService.GetByIdAsync(id);
        if (n == null) return NotFound();
        return View(new NoticeEditViewModel { Id = n.Id, Title = n.Title, Message = n.Message, Target = n.Target });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = PermissionKeys.ManageNotifications)]
    public async Task<IActionResult> Edit(NoticeEditViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        await _noticeService.UpdateAsync(vm);
        TempData["Success"] = "Notice updated.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = PermissionKeys.ManageNotifications)]
    public async Task<IActionResult> Delete(int id)
    {
        var n = await _noticeService.GetByIdAsync(id);
        if (n == null) return NotFound();
        return View(n);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = PermissionKeys.ManageNotifications), ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _noticeService.DeleteAsync(id);
        TempData["Success"] = "Notice deleted.";
        return RedirectToAction(nameof(Index));
    }
}

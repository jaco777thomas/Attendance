using Attendance.Models.Entities;
using Attendance.Models.ViewModels;
using Attendance.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Attendance.Controllers;

public class AccountController : Controller
{
    private readonly IUserService _userService;
    public AccountController(IUserService userService) => _userService = userService;

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Dashboard");
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var user = await _userService.AuthenticateAsync(vm.Email, vm.Password);
        if (user == null)
        {
            ModelState.AddModelError("", "Invalid email or password.");
            return View(vm);
        }
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email),
        };
        foreach (var ur in user.UserRoles)
        {
            if (ur.Role != null)
                claims.Add(new Claim(ClaimTypes.Role, ur.Role.Name));
        }
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8) });

        return LocalRedirect(vm.ReturnUrl ?? Url.Action("Index", "Dashboard")!);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _userService.GetByIdAsync(userId);
        if (user == null) return NotFound();
        return View(new UserProfileViewModel { Id = user.Id, Name = user.Name, Email = user.Email, Phone = user.Phone });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(UserProfileViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _userService.GetByIdAsync(userId);
        if (user == null) return NotFound();
        user.Name = vm.Name; user.Email = vm.Email; user.Phone = vm.Phone;
        if (!string.IsNullOrWhiteSpace(vm.NewPassword))
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(vm.NewPassword);
        TempData["Success"] = "Profile updated successfully.";
        return RedirectToAction("Profile");
    }
}

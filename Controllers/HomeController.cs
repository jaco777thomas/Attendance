using Microsoft.AspNetCore.Mvc;

namespace Attendance.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return View();
        }
        return View();
    }
}

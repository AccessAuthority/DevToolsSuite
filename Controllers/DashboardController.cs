using DevToolsSuite.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DevToolsSuite.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IToolService _toolService;

        public DashboardController(IToolService toolService)
        {
            _toolService = toolService;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var savedTools = await _toolService.GetUserSavedToolsAsync(userId);

            ViewData["SavedTools"] = savedTools;
            return View();
        }

        public IActionResult Profile()
        {
            return View();
        }

        public IActionResult Billing()
        {
            return View();
        }

        public IActionResult Settings()
        {
            return View();
        }
    }
}
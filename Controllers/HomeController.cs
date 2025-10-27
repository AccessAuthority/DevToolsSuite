using DevToolsSuite.Models;
using DevToolsSuite.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace DevToolsSuite.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IToolService _toolService;

        public HomeController(ILogger<HomeController> logger, IToolService toolService)
        {
            _logger = logger;
            _toolService = toolService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Pricing()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(int? statusCode = null)
        {
            var errorViewModel = new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            };

            if (statusCode.HasValue)
            {
                if (statusCode == 404)
                {
                    return View("NotFound");
                }

                errorViewModel.RequestId += $"- StatusCode: {statusCode}";
            }

            return View(errorViewModel);
        }

        [Route("/health")]
        public IActionResult Health()
        {
            return Ok(new { status = "Healthy", timestamp = DateTime.UtcNow });
        }
    }
}
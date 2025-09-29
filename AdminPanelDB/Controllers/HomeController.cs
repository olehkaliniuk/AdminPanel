using System.Diagnostics;
using AdminPanelDB.Models;
using Microsoft.AspNetCore.Mvc;

namespace AdminPanelDB.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }


        public IActionResult Index()
        {
            return View();
        }

    }
}

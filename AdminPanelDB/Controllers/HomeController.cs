using AdminPanelDB.Exeptions;
using AdminPanelDB.Models;
using AdminPanelDB.Models;
using AdminPanelDB.Repository;
using AdminPanelDB.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using AdminPanelDB.Logs;

namespace AdminPanelDB.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ConfigService _configService;

        public HomeController(ILogger<HomeController> logger, ConfigService configService)
        {
            _logger = logger;
            _configService = configService;
        }

        // GET: Startseite.
        [HttpGet]
        public IActionResult Index()
        {
            try
            {
                // Falls Config noch nicht geladen wurde - laden.
                if (_configService.CurrentConfig == null)
                    _configService.RefreshConfig();

                return View();
            }
            catch (RepositoryExceptions ex)
            {
                Logger.LogError(ex, "Fehler beim Laden des Configs.", 1001);



                TempData["FriendlyMessage"] = "Fehler beim Laden des Configs.";

                TempData["ErrorDetails"] = ex.ToString();

                return RedirectToAction("Index", "ExceptionsError");
            }
        }


    }
}
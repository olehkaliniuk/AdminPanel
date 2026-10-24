using AdminPanelDB.Exeptions;
using AdminPanelDB.Models;
using AdminPanelDB.Repository;
using Microsoft.AspNetCore.Mvc;
using AdminPanelDB.Logs;

namespace AdminPanelDB.Controllers
{
    public class VorlagenVerwaltungController : Controller
    {
        [HttpGet]
        public IActionResult VorlagenVerwaltung()
        {
            return View();
        }
    }
}

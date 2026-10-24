using Microsoft.AspNetCore.Mvc;

namespace AdminPanelDB.Controllers
{
    public class ExceptionsErrorController: Controller
    {
        public IActionResult Index()
        {
            var friendly = TempData["FriendlyMessage"] as string;
            var details = TempData["ErrorDetails"] as string;

   
            var model = (FriendlyMessage: friendly, ErrorDetails: details);

            return View("ExceptionsError", model);
        }


    }
}

using AdminPanelDB.Exeptions;
using AdminPanelDB.Models;
using AdminPanelDB.Repository;
using Microsoft.AspNetCore.Mvc;
using AdminPanelDB.Logs;

namespace AdminPanelDB.Controllers
{
    public class UserController : Controller
    {
        private readonly UserRepository _userRepository;

        public UserController(IConfiguration configuration)
        {
            // Repository initialisieren.
            string connString = configuration.GetConnectionString("DefaultConnection");
            _userRepository = new UserRepository(connString);
        }



        [HttpGet]
        public IActionResult UserData()
        {
            try
            {
                // Email aus Session holen.
                var email = HttpContext.Session.GetString("UserEmail");
                if (string.IsNullOrEmpty(email))
                {
                    // Wenn nicht angemeldet, zu Login weiterleiten.
                    return RedirectToAction("Login", "Account");
                }

                var user = _userRepository.GetUserByEmail(email);
                if (user == null)
                {
                    // Kein Benutzer gefunden.
                    return Content("No user");
                }

                // Objekt für View zusammenstellen.
                var userData = new
                {
                    user.Id,
                    user.Titel,
                    user.Name,
                    user.Vorname,
                    user.Email,
                    user.UId,
                    user.Abteilung,
                    user.Referat,
                    user.Stelle,
                    user.Kennwort,
                    user.Rolle,
                    user.IstAdmin
                };

                return View(userData); // View anzeigen.

            }
            catch(RepositoryExceptions ex)
            {
                Logger.LogError(ex, ex.Message, 1001);

                TempData["FriendlyMessage"] = ex.Message;

                TempData["ErrorDetails"] = ex.ToString();

                return RedirectToAction("Index", "ExceptionsError");
            }
        }


        [HttpGet]
        public IActionResult RedirectToPersonen()
        {
            return RedirectToAction("Personen", "Admin");
        }
    }

}

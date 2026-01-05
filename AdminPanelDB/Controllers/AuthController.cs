using AdminPanelDB.Exeptions;
using AdminPanelDB.Repository;
using Microsoft.AspNetCore.Mvc;
using AdminPanelDB.Logs;

namespace AdminPanelDB.Controllers
{
    public class AuthController : Controller
    {
        private readonly AuthRepository _authRepository;

        public AuthController(IConfiguration configuration)
        {
            // Connection-String aus Konfiguration holen.
            string connString = configuration.GetConnectionString("DefaultConnection");
            _authRepository = new AuthRepository(connString);
        }



        // GET: Login-Seite anzeigen.
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }



        // POST: Login-Formular verarbeiten.
        [HttpPost]
        public IActionResult Login(string email, string kennwort)
        {
            try
            {
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(kennwort))
                {
                    ViewBag.Error = "Bitte Email und Passwort eingeben.";
                    return View();
                }

                var result = _authRepository.TryLogin(email, kennwort);

                if (result.Success)
                {
                    var user = _authRepository.GetUserByEmail(email);

                    if (user.userId == 0)
                    {
                        ViewBag.Error = "Benutzer nicht gefunden.";
                        return View();
                    }

                    HttpContext.Session.SetInt32("UserId", user.userId);
                    HttpContext.Session.SetString("UserEmail", email);
                    HttpContext.Session.SetString("UserName", user.fullName ?? email);
                    HttpContext.Session.SetInt32("IstAdmin", user.isAdmin ? 1 : 0);
                    HttpContext.Session.SetString("Rolle", user.rolle);

                    return RedirectToAction("Index", "Home");
                }

                ViewBag.Error = result.Message;
                return View();
            }
            catch (RepositoryExceptions ex)
            {
                Logger.LogError(ex, ex.Message, 1001);

                TempData["FriendlyMessage"] = ex.Message;

                TempData["ErrorDetails"] = ex.ToString();

                return RedirectToAction("Index", "ExceptionsError");
            }
        }




        // Logout: Session löschen.
        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Auth");
        }
    }
}

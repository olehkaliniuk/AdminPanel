using AdminPanelDB.Repository;
using Microsoft.AspNetCore.Mvc;

namespace AdminPanelDB.Controllers
{
    public class AuthController : Controller
    {
        private readonly AuthRepository _authRepository;

        public AuthController(IConfiguration configuration)
        {
            string connString = configuration.GetConnectionString("DefaultConnection");
            _authRepository = new AuthRepository(connString);
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string email, string kennwort)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(kennwort))
            {
                ViewBag.Error = "Bitte Email und Passwort eingeben.";
                return View();
            }

            if (_authRepository.UserExists(email, kennwort))
            {
                string userName = _authRepository.GetUserNameByEmail(email);

                HttpContext.Session.SetString("UserEmail", email);
                HttpContext.Session.SetString("UserName", userName ?? email);

                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Benutzer mit dieser Email und Passwort existiert nicht.";
            return View();
        }

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}

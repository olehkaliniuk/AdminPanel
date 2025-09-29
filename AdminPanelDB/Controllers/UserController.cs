using AdminPanelDB.Models;
using AdminPanelDB.Repository;
using Microsoft.AspNetCore.Mvc;

namespace AdminPanelDB.Controllers
{
    public class UserController : Controller
    {
        private readonly UserRepository _userRepository;

        public UserController(IConfiguration configuration)
        {
            string connString = configuration.GetConnectionString("DefaultConnection");
            _userRepository = new UserRepository(connString);
        }

        // Экшн для отображения страницы с данными пользователя
        public IActionResult UserData()
        {
            // Получаем email пользователя из сессии
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Login", "Account"); // Если нет сессии, редирект на логин
            }

            var user = _userRepository.GetUserByEmail(email);
            if (user == null)
            {
                return Content("No user");
            }

           

            // Создаём объект для передачи в View
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
                user.Kennwort
            };

            return View(userData);
        }
    }
}

using AdminPanelDB.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Text;
using System.Xml.Linq;

namespace AdminPanelDB.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class APIController : Controller
    {
        private readonly AuthRepository _authRepository;



        public APIController(IConfiguration configuration)
        {
            string connString = configuration.GetConnectionString("DefaultConnection");
            _authRepository = new AuthRepository(connString);
        }

   

        [HttpGet("user")]
        public IActionResult GetUserData()
        {
            // Читаем Authorization header
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Basic "))
                return Unauthorized("Missing or invalid Authorization header.");

            // Декодируем Base64
            var encodedCredentials = authHeader.Substring("Basic ".Length).Trim();
            var decodedBytes = Convert.FromBase64String(encodedCredentials);
            var decodedString = Encoding.UTF8.GetString(decodedBytes);

            // Формат email:password
            var parts = decodedString.Split(':', 2);
            if (parts.Length != 2)
                return Unauthorized("Invalid credentials format.");

            var email = parts[0];
            var kennwort = parts[1];

            // Проверяем пользователя (без хеширования)
            if (!_authRepository.UserExists(email, kennwort))
                return Unauthorized("Invalid email or password.");

            // Получаем данные пользователя
            var user = _authRepository.GetUserByEmail(email);
            if (user == null)
                return NotFound("User not found.");




            // Формируем XML
            var xml = new XElement("User",
                new XElement("Id", user.Id),
                new XElement("Titel", user.Titel ?? ""),
                new XElement("Name", user.Name ?? ""),
                new XElement("Vorname", user.Vorname ?? ""),
                new XElement("Email", user.Email ?? ""),
                new XElement("UId", user.UId ?? ""),
                new XElement("Abteilung", user.Abteilung ?? ""),
                new XElement("Referat", user.Referat ?? ""),
                new XElement("Stelle", user.Stelle ?? ""),
                new XElement("Kennwort", user.Kennwort ?? "")
            );

            return Content(xml.ToString(), "application/xml", Encoding.UTF8);
        }
    }
}

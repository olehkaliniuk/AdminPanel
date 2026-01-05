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
            // Connection-String aus Konfiguration holen.
            string connString = configuration.GetConnectionString("DefaultConnection");
            _authRepository = new AuthRepository(connString);
        }



        // API-Endpunkt: Benutzerdaten abrufen.
        [HttpGet("user")]
        public IActionResult GetUserData()
        {
            // Authorization-Header auslesen.
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Basic "))
                return Unauthorized("Missing or invalid Authorization header.");

            // Base64-dekodieren.
            var encodedCredentials = authHeader.Substring("Basic ".Length).Trim();
            var decodedBytes = Convert.FromBase64String(encodedCredentials);
            var decodedString = Encoding.UTF8.GetString(decodedBytes);

            // Email:Password aufteilen.
            var parts = decodedString.Split(':', 2);
            if (parts.Length != 2)
                return Unauthorized("Invalid credentials format.");

            var email = parts[0];
            var kennwort = parts[1];

            // Login prüfen.
            var (success, message) = _authRepository.TryLogin(email, kennwort);
            if (!success)
                return Unauthorized(message);

            // Benutzerdaten holen.
            var user = _authRepository.GetUserByEmailAPI(email);
            if (user == null)
                return NotFound("User not found.");

            // XML erstellen.
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

            // XML als Content zurückgeben.
            return Content(xml.ToString(), "application/xml", Encoding.UTF8);
        }
    }
}


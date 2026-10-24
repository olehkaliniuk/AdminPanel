using AdminPanelDB.Repository;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace AdminPanelDB.Controllers
{
    public class MandantController : Controller
    {
        private readonly MandantenRepository _mandantenRepository;

        public MandantController(IConfiguration configuration)
        {
            string connString = configuration.GetConnectionString("DefaultConnection");
            _mandantenRepository = new MandantenRepository(connString);
        }

        [HttpGet]
        public IActionResult GetAllMandanten()
        {
            if (!IsDreiF())
            {
                return StatusCode(403);
            }

            var mandanten = _mandantenRepository.GetAllMandanten();
            return Json(mandanten);
        }

        [HttpGet]
        public IActionResult GetActiveMandant()
        {
            if (!IsDreiF())
            {
                return StatusCode(403);
            }

            var activeMandant = HttpContext.Session.GetInt32("ActiveMandantId");
            var loginMandant = HttpContext.Session.GetInt32("LoginMandantId");

            return Json(new { mandantId = activeMandant ?? loginMandant ?? 0 });
        }

        [HttpPost]
        public IActionResult SwitchMandant([FromForm] int mandantId)
        {
            if (!IsDreiF())
            {
                return StatusCode(403);
            }

            HttpContext.Session.SetInt32("ActiveMandantId", mandantId);
            return Ok();
        }

        [HttpPost]
        public IActionResult ResetMandant()
        {
            HttpContext.Session.Remove("ActiveMandantId");
            return Ok();
        }

        // Sonderrolle "DreiF": darf zwischen allen Mandanten wechseln.
        private bool IsDreiF()
        {
            var rollenJson = HttpContext.Session.GetString("Rollen");
            if (string.IsNullOrEmpty(rollenJson))
            {
                return false;
            }

            var rollen = JsonConvert.DeserializeObject<List<string>>(rollenJson) ?? new List<string>();
            return rollen.Any(r => r.Equals("DreiF", StringComparison.OrdinalIgnoreCase));
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Text.Json;

namespace AdminPanelDB.Filters
{
    // Rollenprüfung.
    public class SessionAuthorizeAttribute : ActionFilterAttribute
    {
        public bool AdminOnly { get; set; } = false;

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;

            var userId = session.GetInt32("UserId");
            var rollenJson = session.GetString("Rollen");

            var rollen = string.IsNullOrEmpty(rollenJson)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(rollenJson);

            bool isAdmin = rollen.Any(r =>
                r.Equals("Administrator", StringComparison.OrdinalIgnoreCase));

            if (userId == null || (AdminOnly && !isAdmin))
            {
                session.Clear();
                context.Result = new RedirectToActionResult("Login", "Auth", null);
            }
        }
    }
}
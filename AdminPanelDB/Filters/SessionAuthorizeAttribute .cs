using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AdminPanelDB.Filters
{
    // Rollenprüfung.
    public class SessionAuthorizeAttribute : ActionFilterAttribute
    {
        public bool AdminOnly { get; set; } = false;

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var userId = context.HttpContext.Session.GetInt32("UserId");
            var rolle = context.HttpContext.Session.GetString("Rolle"); 

            if (userId == null || (AdminOnly && rolle != "Administrator"))
            {
                context.HttpContext.Session.Clear();
                context.Result = new RedirectToActionResult("Login", "Auth", null);

            }
        }
    }
}

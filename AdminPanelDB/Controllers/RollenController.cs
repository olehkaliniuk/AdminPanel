using AdminPanelDB.Abstractions;
using AdminPanelDB.Exeptions;
using AdminPanelDB.Filters;
using AdminPanelDB.Logs;
using AdminPanelDB.Models;
using AdminPanelDB.Repository;
using AdminPanelDB.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.RegularExpressions;

namespace AdminPanelDB.Controllers
{
    [SessionAuthorize(AdminOnly = true)]
    public class RollenController : Controller
    {

        private readonly RollenRepository _repo;

        public RollenController(IConfiguration configuration, ICurrentMandantProvider mandantProvider)
        {
            string connString = configuration.GetConnectionString("DefaultConnection");
            _repo = new RollenRepository(connString, mandantProvider);
        }

        [HttpGet]
        public IActionResult Rollen(int? pageNumber, int pageSize = 10, string rolleSearchTerm = "", string sortColumn = null, string sortDirection = null)
        {
            try
            {
                int currentPage = pageNumber ?? 1;

                var effectiveSortColumn = string.IsNullOrEmpty(sortColumn) ? "Id" : sortColumn;

                var effectiveSortDirection = string.IsNullOrEmpty(sortDirection) ? "ASC" : sortDirection;

                int totalCount = _repo.GetRollenCount(rolleSearchTerm);

                int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                if (totalPages == 0)
                {
                    totalPages = 1;
                }

                if (currentPage > totalPages)
                {
                    currentPage = 1;
                }

                var items = _repo.GetRollenPage(
                    pageNumber: currentPage,
                    pageSize: pageSize,
                    searchTerm: rolleSearchTerm,
                    sortColumn: effectiveSortColumn,
                    sortDirection: effectiveSortDirection
                );

                var pagedList =
                    new PaginatedList<Rolle>(
                        items,
                        totalCount,
                        currentPage,
                        pageSize
                    );

                var model = new RolleViewModel
                {
                    Rollen = pagedList,

                    RolleSearchTerm = rolleSearchTerm,

                    RolleSortColumn = sortColumn,

                    RolleSortDirection = sortDirection
                };

                ViewBag.TotalCount = totalCount;

                return View(model);
            }
            catch (RepositoryExceptions ex)
            {
                Logger.LogError(ex, ex.Message, 1001);

                TempData["FriendlyMessage"] = ex.Message;
                TempData["ErrorDetails"] = ex.ToString();

                return RedirectToAction(
                    "Index",
                    "ExceptionsError"
                );
            }
        }

        [HttpPost]
        public IActionResult CreateRolle(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    TempData["Message"] = "Name darf nicht leer sein!";

                    TempData["MessageType"] = "error";

                    return RedirectToAction("Rollen");
                }

                bool success = _repo.CreateRolle(name);

                if (!success)
                {
                    TempData["Message"] = $"Rolle '{name}' existiert bereits!";

                    TempData["MessageType"] = "error";

                    return RedirectToAction("Rollen");
                }

                int pageSize = 10;

                int totalCount = _repo.GetRollenCount();

                int lastPage = (int)Math.Ceiling(totalCount / (double)pageSize);

                TempData["Message"] = $"Rolle '{name}' erfolgreich erstellt!";

                TempData["MessageType"] = "success";

                return RedirectToAction("Rollen",new
                    {
                        pageNumber = lastPage,
                        pageSize
                    });
            }
            catch (RepositoryExceptions ex)
            {
                Logger.LogError(ex, ex.Message, 1001);

                TempData["FriendlyMessage"] = ex.Message;
                TempData["ErrorDetails"] = ex.ToString();

                return RedirectToAction(
                    "Index",
                    "ExceptionsError"
                );
            }
        }

        [HttpPost]
        public IActionResult EditRolle(int id, string newName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(newName))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Name darf nicht leer sein."
                    });
                }

                bool success = _repo.EditRolle(id, newName);

                if (!success)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Rolle '{newName}' existiert bereits!"
                    });
                }

                return Json(new
                {
                    success = true,
                    message = $"Rolle '{newName}' erfolgreich bearbeitet!"
                });
            }
            catch (RepositoryExceptions ex)
            {
                Logger.LogError(ex, ex.Message, 1001);

                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost]
        public IActionResult DeleteRolle(int id)
        {
            try
            {
                bool deleted = _repo.DeleteRolle(id);

                if (!deleted)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Rolle konnte nicht gelöscht werden."
                    });
                }

                return Json(new
                {
                    success = true,
                    message = "Rolle erfolgreich gelöscht!"
                });
            }
            catch (RepositoryExceptions ex)
            {
                Logger.LogError(ex, ex.Message, 1001);

                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
    }
}

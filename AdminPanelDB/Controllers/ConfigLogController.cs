using AdminPanelDB.Exeptions;
using AdminPanelDB.Models;
using AdminPanelDB.Repository;
using Microsoft.AspNetCore.Mvc;
using AdminPanelDB.Logs;

namespace AdminPanelDB.Controllers
{
    public class ConfigLogController: Controller
    {
        private readonly ConfigLogRepository _configLogRepository;

        public ConfigLogController(ConfigLogRepository configLog)
        {
            _configLogRepository = configLog;
        }

        // GET: Config-Logs.
        [HttpGet]
        public IActionResult ConfigLog(
            string tabelleKey, string tabelleName, string aktion, string alterWert, string neuerWert, string geaendertVon,
            string geaendertAm,
            string sortColumn = "GeaendertAm", string sortDirection = "DESC",
            int page = 1, int pageSize = 10)
        {
            try
            {
                // Gesamtanzahl der gefilterten Einträge ermitteln.
                var totalCount = _configLogRepository.GetFilteredCount(
                    tabelleKey, tabelleName, aktion, alterWert, neuerWert, geaendertVon, geaendertAm);

                // Gesamtseiten berechnen.
                int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                if (totalPages == 0)
                {
                    totalPages = 1; 
                }
                if (page > totalPages) 
                {
                    page = 1;
                }

                // Gefilterte, sortierte und paginierte Einträge abrufen.
                var entries = _configLogRepository.GetPagedFiltered(
                    tabelleKey, tabelleName, aktion, alterWert, neuerWert, geaendertVon, geaendertAm,
                    sortColumn, sortDirection, page, pageSize);

                // Filterwerte für die View speichern.
                ViewBag.Filters = new
                {
                    tabelleKey,
                    tabelleName,
                    aktion,
                    alterWert,
                    neuerWert,
                    geaendertVon,
                    geaendertAm,
                    sortColumn,
                    sortDirection
                };

                // Pagination-Daten für die View.
                ViewBag.Page = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalPages = totalPages;
                ViewBag.TotalCount = totalCount;

                return View(entries);
            }
            catch (RepositoryExceptions ex)
            {
                Logger.LogError(ex, ex.Message, 1001);

                TempData["FriendlyMessage"] = ex.Message;
                TempData["ErrorDetails"] = ex.ToString();

                return RedirectToAction("Index", "ExceptionsError");
            }
        }

    }
}

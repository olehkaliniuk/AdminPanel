using AdminPanelDB.Abstractions;
using AdminPanelDB.Exeptions;
using AdminPanelDB.Logs;
using AdminPanelDB.Models;
using AdminPanelDB.Repository;
using Microsoft.AspNetCore.Mvc;

namespace AdminPanelDB.Controllers
{
    public class ConfigLogController : Controller
    {
        private readonly ConfigLogRepository _configLogRepository;

        public ConfigLogController(IConfiguration configuration, ICurrentMandantProvider mandantProvider)
        {
            _configLogRepository = new ConfigLogRepository(configuration, mandantProvider);
        }

        // GET: Config-Logs.
        [HttpGet]
        public IActionResult ConfigLog(
        string tabelleKey, string tabelleName, string aktion, string alterWert, string neuerWert, string geaendertVon,
        string geaendertAm, string geaendertAmMode, string mandantId,         
        string sortColumn = "GeaendertAm", string sortDirection = "DESC",
        int page = 1, int pageSize = 10)
        {
            try
            {
                var totalCount = _configLogRepository.GetFilteredCount(
                    tabelleKey: tabelleKey, tabelleName: tabelleName, aktion: aktion, alterWert: alterWert,
                    neuerWert: neuerWert, geaendertVon: geaendertVon, geaendertAm: geaendertAm,
                    mandantId: mandantId, geaendertAmMode: geaendertAmMode);

                int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                if (totalPages == 0) totalPages = 1;
                if (page > totalPages) page = 1;

                var entries = _configLogRepository.GetPagedFiltered(
                    tabelleKey: tabelleKey, tabelleName: tabelleName, aktion: aktion, alterWert: alterWert,
                    neuerWert: neuerWert, geaendertVon: geaendertVon, geaendertAm: geaendertAm,
                    mandantId: mandantId,
                    sortColumn: sortColumn, sortDirection: sortDirection, page: page, pageSize: pageSize,
                    geaendertAmMode: geaendertAmMode);

                ViewBag.Filters = new
                {
                    tabelleKey,
                    tabelleName,
                    aktion,
                    alterWert,
                    neuerWert,
                    geaendertVon,
                    geaendertAm,
                    geaendertAmMode = geaendertAmMode ?? "after",
                    mandantId,
                    sortColumn,
                    sortDirection
                };

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

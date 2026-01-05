using AdminPanelDB.Filters;
using AdminPanelDB.Models;
using AdminPanelDB.Services;
using Microsoft.AspNetCore.Mvc;

namespace AdminPanelDB.Controllers
{
    [SessionAuthorize(AdminOnly = true)]
    public class LogsController : Controller
    {
        private readonly WindowsEventLogService _eventService;

        public LogsController()
        {
            _eventService = new WindowsEventLogService();
        }

        public IActionResult Logs(
        string searchLevel = "",
        string searchSource = "",
        string searchEventId = "",
        string searchMessage = "",
        string searchTime = "",
        int page = 1,
        int pageSize = 10,
        string sortColumn = "Time",
        string sortDirection = "DESC") // ASC oder DESC.
        {
            var logs = _eventService.GetApplicationErrors(1000); // Maximal 1000 letzte Logs.



            // Filtration nach jeder Spalte.
            if (!string.IsNullOrEmpty(searchLevel))
            {
                logs = logs.Where(l => l.Level.Contains(searchLevel, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrEmpty(searchSource))
            {
                logs = logs.Where(l => l.Source.Contains(searchSource, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrEmpty(searchTime))
            {
                logs = logs.Where(l => l.Time.ToString().Contains(searchTime)).ToList();
            }

            if (!string.IsNullOrEmpty(searchEventId))
            {
                logs = logs.Where(l => l.EventId.ToString().Contains(searchEventId)).ToList();
            }

            if (!string.IsNullOrEmpty(searchMessage))
            {
                logs = logs.Where(l => l.Message.Contains(searchMessage, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Sortierung.
            logs = (sortColumn, sortDirection.ToUpper()) switch
            {
                ("Level", "ASC") => logs.OrderBy(l => l.Level).ToList(),
                ("Level", "DESC") => logs.OrderByDescending(l => l.Level).ToList(),
                ("Source", "ASC") => logs.OrderBy(l => l.Source).ToList(),
                ("Source", "DESC") => logs.OrderByDescending(l => l.Source).ToList(),
                ("EventId", "ASC") => logs.OrderBy(l => l.EventId).ToList(),
                ("EventId", "DESC") => logs.OrderByDescending(l => l.EventId).ToList(),
                ("Time", "ASC") => logs.OrderBy(l => l.Time).ToList(),
                _ => logs.OrderByDescending(l => l.Time).ToList()
            };

            var pagedLogs = logs.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var model = new LogsViewModel
            {
                Logs = pagedLogs,
                Page = page,
                PageSize = pageSize,
                TotalCount = logs.Count,
                SearchLevel = searchLevel,
                SearchSource = searchSource,
                SearchEventId = searchEventId,
                SearchMessage = searchMessage,
                SearchTime = searchTime,
                SortColumn = sortColumn,
                SortDirection = sortDirection.ToUpper()
            };

            ViewBag.Filters = model;

            return View(model);
        }

    }
}


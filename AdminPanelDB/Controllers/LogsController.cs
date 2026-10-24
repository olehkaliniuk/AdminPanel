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
        string geaendertAmMode = "after",
        string geaendertAm = "",
        int page = 1,
        int pageSize = 10,
        string sortColumn = "Time",
        string sortDirection = "DESC")
        {
            var logs = _eventService.GetApplicationErrors(1000);

            if (!string.IsNullOrEmpty(searchLevel))
                logs = logs.Where(l => l.Level.Contains(searchLevel, StringComparison.OrdinalIgnoreCase)).ToList();

            if (!string.IsNullOrEmpty(searchSource))
                logs = logs.Where(l => l.Source.Contains(searchSource, StringComparison.OrdinalIgnoreCase)).ToList();

            if (!string.IsNullOrEmpty(searchEventId))
                logs = logs.Where(l => l.EventId.ToString().Contains(searchEventId)).ToList();

            if (!string.IsNullOrEmpty(searchMessage))
                logs = logs.Where(l => l.Message.Contains(searchMessage, StringComparison.OrdinalIgnoreCase)).ToList();

            // Zeitfilter
            if (!string.IsNullOrEmpty(geaendertAm) && DateTime.TryParse(geaendertAm, out var filterDate))
            {
                logs = geaendertAmMode == "before"
                    ? logs.Where(l => l.Time <= filterDate).ToList()
                    : logs.Where(l => l.Time >= filterDate).ToList();
            }

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

            int totalCount = logs.Count;
            int totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
            page = Math.Clamp(page, 1, totalPages);

            var pagedLogs = logs.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var model = new LogsViewModel
            {
                Logs = pagedLogs,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                SearchLevel = searchLevel,
                SearchSource = searchSource,
                SearchEventId = searchEventId,
                SearchMessage = searchMessage,
                SortColumn = sortColumn,
                SortDirection = sortDirection.ToUpper()
            };

            ViewBag.Filters = new
            {
                SortColumn = sortColumn,
                SortDirection = sortDirection.ToUpper(),
                geaendertAmMode = geaendertAmMode,
                geaendertAm = geaendertAm
            };

            return View(model);
        }
    }
}


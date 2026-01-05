using AdminPanelDB.Models;

namespace AdminPanelDB.ViewModels
{
    public class ConfigViewModel
    {
        public required PaginatedList<ConfigEntry> ConfigEntries { get; set; }
        public required string SearchTerm { get; set; }
    }

}

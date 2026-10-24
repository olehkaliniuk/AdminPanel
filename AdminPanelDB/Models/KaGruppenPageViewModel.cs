using AdminPanelDB.Models;

namespace AdminPanelDB.ViewModels
{
    public class KaGruppenPageViewModel
    {
        public PaginatedList<KaGruppenViewModel>? HauptGruppen { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public string HauptgruppeSearchTerm { get; set; } = "";
        public string NebengruppeSearchTerm { get; set; } = "";
        public string SortColumn { get; set; } = "Id";
        public string SortDirection { get; set; } = "ASC";
    }
}
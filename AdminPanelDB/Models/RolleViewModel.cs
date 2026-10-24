namespace AdminPanelDB.Models
{
    public class RolleViewModel
    {
        public PaginatedList<Rolle> Rollen { get; set; }

        public string RolleSearchTerm { get; set; }

        public string RolleSortColumn { get; set; }

        public string RolleSortDirection { get; set; }
    }
}

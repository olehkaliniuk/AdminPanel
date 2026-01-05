using AdminPanelDB.Models;

namespace AdminPanelDB.ViewModels
{
    public class AdresseViewModel
    {
        public required PaginatedList<Adresse> Adresses { get; set; }
        public required string SearchTerm { get; set; }
    }
}

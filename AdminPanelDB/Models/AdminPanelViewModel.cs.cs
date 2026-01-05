using AdminPanelDB.ViewModels;
using AdminPanelDB.Models;
using System.Collections.Generic;

namespace AdminPanelDB.ViewModels
{
    public class AdminPanelViewModel
    {
        public PaginatedList<AbteilungReferateViewModel>? Abteilungen { get; set; } 
        public PaginatedList<Personen>? Personen { get; set; }

        public PersonenFilter? Filter { get; set; }

        public string? AbteilungSearchTerm { get; set; }
        public string? ReferatSearchTerm { get; set; }

        //Neue Properties für Sortierung.
        public string? SortColumn { get; set; } 
        public string? SortDirection { get; set; } 


    }
}

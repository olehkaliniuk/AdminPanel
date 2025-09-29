using AdminPanelDB.ViewModels;
using AdminPanelDB.Models;
using System.Collections.Generic;

namespace AdminPanelDB.ViewModels
{
    public class AdminPanelViewModel
    {
        public List<AbteilungReferateViewModel> Abteilungen { get; set; }
        public List<Personen> Personen { get; set; }
  
    }
}

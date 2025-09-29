using System.Collections.Generic;
using AdminPanelDB.Models;

namespace AdminPanelDB.ViewModels
{
    public class AbteilungReferateViewModel
    {
        public Abteilung Abteilung { get; set; }
        public List<Referat> Referate { get; set; }

    }
}

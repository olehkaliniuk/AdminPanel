using System.Collections.Generic;
using AdminPanelDB.Models;

namespace AdminPanelDB.ViewModels
{
    public class AbteilungReferateViewModel
    {
        public required Abteilung Abteilung { get; init; }
        public required List<Referat> Referate { get; init; } = new();

    }
}

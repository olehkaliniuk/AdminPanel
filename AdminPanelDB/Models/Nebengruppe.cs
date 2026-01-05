namespace AdminPanelDB.Models
{
    public class Nebengruppe
    {
        public int Id { get; set; }
        public int GehoertZuKategorie { get; set; }
        public int GehoertZuHauptgruppe { get; set; }
        public string? Bezeichnung { get; set; }
        public string? Kuerzel { get; set; }
        public string? Beschreibung { get; set; }
        public int NebengruppeNummer { get; set; }
        public bool IstAktiv { get; set; }
        public bool IstTestGruppe { get; set; }
    }
}

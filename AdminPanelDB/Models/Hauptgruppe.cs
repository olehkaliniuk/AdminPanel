namespace AdminPanelDB.Models
{
    public class Hauptgruppe
    {
        public Guid ObjektId { get; set; }
        public int Id { get; set; }
        public int GehoertZuKategorie { get; set; }
        public string? Bezeichnung { get; set; }
        public string? Kuerzel { get; set; }
        public string? Beschreibung { get; set; }
        public int HauptgruppeNummer { get; set; }
        public int LaufenderAktenzeichenZaehler { get; set; }
        public bool IstAktiv { get; set; }
        public bool IstTestGruppe { get; set; }
        public List<string> Referate { get; set; } = new();
    }
}

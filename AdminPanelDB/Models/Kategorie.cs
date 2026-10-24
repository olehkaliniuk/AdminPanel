namespace AdminPanelDB.Models
{
    public class Kategorie
    {
        public Guid ObjektId { get; set; }
        public int Id { get; set; }
        public string? Bezeichnung { get; set; }
        public string? Kuerzel {  get; set; }
        public string? Beschreibung { get; set; }
        public int KategorieNummer { get; set; }
        public bool IstAktiv {  get; set; }
        public bool IstTestKategorie { get; set; }
        public List<string> Abteilungen { get; set; } = new();

    }
}

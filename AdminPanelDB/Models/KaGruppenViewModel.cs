namespace AdminPanelDB.Models
{
    public class KaGruppenViewModel
    {
        public Kategorie? Kategorie { get; set; }
        public List <Hauptgruppe>? Hauptgruppen { get; set; }
        public List <Nebengruppe>? Nebengruppen { get; set; }
    }
}

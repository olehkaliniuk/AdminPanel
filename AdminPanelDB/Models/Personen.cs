namespace AdminPanelDB.Models
{
    public class Personen
    {
        public int Id { get; set; }
        public string? Titel { get; set; }
        public string? Name { get; set; }
        public string? Vorname { get; set; }
        public string? Email { get; set; }
        public string? UId { get; set; }
        public string? Abteilung { get; set; }
        public string? Referat { get; set; }
        public string? Stelle { get; set; }
        public string? Kennwort { get; set; }
        public string? TKennowort { get; set; }
        public int FailedLoginAttempts { get; set; }
        public DateTime? LastFailedAttempt { get; set; }
        public bool IstAdmin { get; set; }
        public string? Rolle { get; set; }
    }
}

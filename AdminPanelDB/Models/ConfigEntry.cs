namespace AdminPanelDB.Models
{
    public class ConfigEntry
    {
        public int Id { get; set; }
        public string? System { get; set; }
        public string? ProjektName { get; set; }
        public string? KeyBezeichnung { get; set; }
        public string? StringValue { get; set; }
        public int? IntValue { get; set; }
        public bool? BoolValue { get; set; }
        public DateTime? DateTimeValue { get; set; }
        public string? Beschreibung { get; set; }
        public bool IstAktiv { get; set; }
        public bool AnpassungGesperrt { get; set; }
        public DateTime? EintragAngepasstAm { get; set; }
        public string? EintragAngepasstVon { get; set; }
    }
}

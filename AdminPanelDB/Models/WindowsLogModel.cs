namespace AdminPanelDB.Models
{
    public class WindowsLogModel
    {
        public int Index { get; set; }         // für den Klick auf die Zeile.
        public string? Level { get; set; }      // Error / Warning / Info.
        public DateTime Time { get; set; }
        public string? Source { get; set; }
        public string? EventId { get; set; }
        public string? Message { get; set; }    // Text aus Event Log.
    }
}

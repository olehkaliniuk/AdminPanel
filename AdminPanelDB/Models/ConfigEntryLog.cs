namespace AdminPanelDB.Models
{
    public class ConfigLog
    {
        public int Id { get; set; }                     
        public string? TabelleKey { get; set; }    
        public string? TabelleName { get; set; }
        public string? Aktion { get; set; }             
        public string? AlterWert { get; set; }          
        public string? NeuerWert { get; set; }          
        public string? GeaendertVon { get; set; }       
        public DateTime GeaendertAm { get; set; }      
    }
}

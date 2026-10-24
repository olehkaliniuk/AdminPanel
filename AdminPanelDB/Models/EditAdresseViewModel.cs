namespace AdminPanelDB.Models
{
    public class EditAdresseViewModel
    {
        public Adresse? Adresse { get; set; }
        public List<CountryCode>? Countries { get; set; }
        public List<string>? AddressFields { get; set; }
        public List<string>? BillingFields { get; set; }
        public List<string>? Errors { get; set; }


        public string? SelectedCountry { get; set; }
        public string? SelectedRechnungsCountry { get; set; }
    }
}

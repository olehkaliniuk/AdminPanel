namespace AdminPanelDB.Models
{
    public class Referat
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public int AbteilungId { get; set; }
    }
}

namespace AdminPanelDB.Models
{
    public class LogsViewModel
    {
        public List<WindowsLogModel>? Logs { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public int TotalCount { get; set; }

        public string? SearchLevel { get; set; }
        public string? SearchSource { get; set; }
        public string? SearchEventId { get; set; }
        public string? SearchMessage { get; set; }
        public string? SearchTime { get; set; }

        public string SortColumn { get; set; } = "Time";
        public string SortDirection { get; set; } = "DESC";

        public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)TotalCount / PageSize));



    }

}

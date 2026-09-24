namespace MvcApp.Core
{
    public class VisitorLog
    {
        public int Id { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public DateTime VisitTime { get; set; }
        public string? Country { get; set; }
        public string? City { get; set; }
        public string? Region { get; set; }
    }
}

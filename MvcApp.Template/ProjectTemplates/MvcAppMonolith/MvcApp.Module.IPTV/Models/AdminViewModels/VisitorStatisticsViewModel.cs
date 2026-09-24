using MvcApp.Common.Models.PagedList;

namespace MvcApp.Module.IPTV.Models.AdminViewModels;

public class VisitorStatisticsViewModel
{
    public int TotalVisits { get; set; }
    public int UniqueVisitors { get; set; }
    public PaginatedList<VisitPerDay> VisitsPerDay { get; set; }
    public PaginatedList<VisitorLocation>? VisitorLocations { get; set; }
    public string? VisitsSearchString { get; set; }
    public string? LocationsSearchString { get; set; }

    public VisitorStatisticsViewModel()
    {
        VisitsPerDay = new PaginatedList<VisitPerDay>(new List<VisitPerDay>(), 0, 1, 1);
    }
}

public class VisitPerDay
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
}

public class VisitorLocation
{
    public DateTime Date { get; set; }
    public string? Country { get; set; }
    public string? Region { get; set; }
    public string? City { get; set; }
    public int Count { get; set; }
}

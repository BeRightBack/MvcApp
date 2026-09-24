using MvcApp.Core;
using MvcApp.Core.Models;
using MvcApp.Infrastructure.Pagination;

namespace MvcApp.Infrastructure.Models;

public class MemberCacheModel
{
    public DateTime CacheTime { get; set; }
    public string SearchKey { get; set; }= string.Empty;
    public PaginationResponseModel<IEnumerable<MemberModel>> PaginatedResponse { get; set; } = new PaginationResponseModel<IEnumerable<MemberModel>>();
}

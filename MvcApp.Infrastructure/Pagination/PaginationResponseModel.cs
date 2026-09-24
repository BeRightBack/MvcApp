using MvcApp.Core.Pagination;
using MvcApp.Infrastructure.Models;

namespace MvcApp.Infrastructure.Pagination;

public class PaginationResponseModel<T> : ServiceResponseModel<T>
{
    public PaginationModel MetaData { get; set; } = new PaginationModel();
}

namespace MvcApp.Infrastructure.Models;

public class ServiceResponseModel<T>
{
    public bool Success { get; set; } = true;
    public T Data { get; set; } = default!;
    public string Message { get; set; } = string.Empty;
}

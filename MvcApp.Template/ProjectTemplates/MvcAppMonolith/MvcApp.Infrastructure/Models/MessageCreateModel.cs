using System.ComponentModel.DataAnnotations;

namespace MvcApp.Infrastructure.Models;

public class MessageCreateModel
{
    [MaxLength(50)]
    public string RecipientUsername { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Content { get; set; } = string.Empty;
}

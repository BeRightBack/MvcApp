using System.ComponentModel.DataAnnotations;
using MvcApp.Core;

namespace MvcApp.Module.IPTV.Models.StoreViewModels;

public class CheckoutViewModel
{
    public IEnumerable<ShoppingCartItem>? CartItems { get; set; }
    public string? PaymentMethod { get; set; }
    public string? Currency { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }

    [Required]
    public string DeviceType { get; set; } = string.Empty;

    [Required]
    public string AccountType { get; set; } = string.Empty;

    public string? SelectedCurrency { get; set; }
    public string? SelectedDeviceType { get; set; }
    public string? MacAddress { get; set; }

    [Required]
    [Display(Name = "Verification Code")]
    public required string VerificationCode { get; set; }
}

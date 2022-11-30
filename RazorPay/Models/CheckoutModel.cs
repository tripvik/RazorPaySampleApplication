using System.ComponentModel.DataAnnotations;

namespace RazorPay.Models;

public class CheckoutModel
{
    public string Name { get; set; }
    public string Phone { get; set; }

    [EmailAddress]
    public string Email { get; set; }
    public decimal Amount { get; set; }
}

namespace RazorPay.Models
{
    public class RazorPayOptions
    {
        public const string RazorPay = "RazorPay";

        public string Key { get; set; } = String.Empty;
        public string Secret { get; set; } = String.Empty;
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Razorpay.Api;
using RazorPay.Models;
using System.Security.Cryptography;
using System.Text;

namespace RazorPay.Controllers
{
    public class PaymentController : Controller
    {
        private readonly RazorPayOptions _razorPay;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(ILogger<PaymentController> logger, IOptions<RazorPayOptions> razorPay)
        {
            _logger = logger;
            _razorPay = razorPay.Value;
        }

        public ViewResult Checkout(Guid? id)
        {
            var model = new CheckoutModel() { Amount = 2 };
            return View(model);
        }

        public ViewResult Payment(CheckoutModel checkout)
        {
            OrderModel order = new()
            {
                OrderAmount = checkout.Amount,
                Currency = "INR",
                Payment_Capture = 1,    // 0 - Manual capture, 1 - Auto capture
                Notes = new Dictionary<string, string>()
                {
                    { "note 1", "first note while creating order" }, { "note 2", "you can add upto 15 notes" },
                }
            };

            var orderId = CreateOrder(order);

            RazorPayOptionsModel razorPayOptions = new()
            {
                Key = _razorPay.Key,
                AmountInSubUnits = order.OrderAmountInSubUnits,
                Currency = order.Currency,
                Name = "Chocolate Shop",
                Description = "All kind of chocolates!",
                ImageLogUrl = "",
                OrderId = orderId,
                ProfileName = checkout.Name,
                ProfileContact = checkout.Phone,
                ProfileEmail = checkout.Email,
                Notes = new Dictionary<string, string>()
                {
                    { "note 1", "this is a payment note" }, { "note 2", "another note, you can add upto 15 notes" }
                }
            };

            return View(razorPayOptions);
        }

        public ViewResult AfterPayment()
        {
            var paymentStatus = Request.Form["paymentstatus"].ToString();
            if (paymentStatus == "Fail")
                return View("Fail");

            var orderId = Request.Form["orderid"].ToString();
            var paymentId = Request.Form["paymentid"].ToString();
            var signature = Request.Form["signature"].ToString();

            var validSignature = CompareSignatures(orderId, paymentId, signature);
            if (validSignature)
            {
                ViewBag.Message = $"Congratulations!! Your payment was successful and the payment id is {paymentId}";
                return View("Success");
            }

            else
            {
                return View("Fail");
            }
        }

        public ViewResult Capture()
        {
            return View();
        }

        public ViewResult CapturePayment(string paymentId)
        {
            RazorpayClient client = new(_razorPay.Key, _razorPay.Secret);
            Payment payment = client.Payment.Fetch(paymentId);
            var amount = payment.Attributes["amount"];
            var currency = payment.Attributes["currency"];

            Dictionary<string, object> options = new()
            {
                { "amount", amount },
                { "currency", currency }
            };

            Payment paymentCaptured = payment.Capture(options);
            ViewBag.Message = "Payment capatured!";
            return View("Success");
        }

        private string CreateOrder(OrderModel order)
        {
            try
            {
                RazorpayClient client = new(_razorPay.Key, _razorPay.Secret);
                Dictionary<string, object> options = new()
                {
                    { "amount", order.OrderAmountInSubUnits },
                    { "currency", order.Currency },
                    { "payment_capture", order.Payment_Capture },
                    { "notes", order.Notes }
                };

                Order orderResponse = client.Order.Create(options);
                var orderId = orderResponse.Attributes["id"].ToString();
                return orderId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }

            return string.Empty;
        }

        private bool CompareSignatures(string orderId, string paymentId, string razorPaySignature)
        {
            var text = orderId + "|" + paymentId;
            var secret = _razorPay.Secret;
            var generatedSignature = CalculateSHA256(text, secret);
            if (generatedSignature == razorPaySignature)
            {
                return true;
            }

            return false;
        }

        private static string CalculateSHA256(string text, string secret)
        {
            var enc = Encoding.Default;
            var baText2BeHashed = enc.GetBytes(text);
            var baSalt = enc.GetBytes(secret);
            HMACSHA256 hasher = new(baSalt);
            var baHashedText = hasher.ComputeHash(baText2BeHashed);
            return string.Join("", baHashedText.ToList().Select(b => b.ToString("x2")).ToArray());
        }
    }
}

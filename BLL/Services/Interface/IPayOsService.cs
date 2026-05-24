using System.Threading.Tasks;

namespace AutoWashPro.BLL.Services
{
    public interface IPayOsService
    {
        Task<PayOsPaymentResult> CreatePaymentLinkAsync(long orderCode, int amount, string description, string userId);
        Task<PayOsWebhookResult?> VerifyWebhookDataAsync(object webhookBody);
    }

    public class PayOsPaymentResult
    {
        public string CheckoutUrl { get; set; } = null!;
        public long OrderCode { get; set; }
    }

    public class PayOsWebhookResult
    {
        public string Code { get; set; } = null!;
        public long OrderCode { get; set; }
        public bool IsSuccess => Code == "00";
    }
}

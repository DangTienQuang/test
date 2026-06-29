using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoWashPro.BLL.Services
{
    public class PayOsService : IPayOsService
    {
        private readonly dynamic? _payOS;

        public PayOsService(IConfiguration configuration)
        {
            var payOsConfig = configuration.GetSection("PayOS");
            var clientId = payOsConfig["ClientId"];
            var apiKey = payOsConfig["ApiKey"];
            var checksumKey = payOsConfig["ChecksumKey"];

            if (!string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(checksumKey))
            {
                try
                {
                    var asm = System.Reflection.Assembly.Load("Net.payOS");
                    var type = asm.GetType("Net.payOS.PayOS");
                    if (type != null) {
                        _payOS = Activator.CreateInstance(type, clientId, apiKey, checksumKey)!;
                    }
                }
                catch
                {
                    _payOS = null; // Fail silently to protect DI container
                }
            }
        }

        public async Task<PayOsPaymentResult> CreatePaymentLinkAsync(long orderCode, int amount, string description, string userId)
        {
            if (_payOS == null)
            {
                throw new Exception("PayOS configuration is missing (ClientId, ApiKey, or ChecksumKey). Cannot create payment link.");
            }

            var asm = _payOS.GetType().Assembly;
            var paymentDataType = asm.GetType("PayOS.Types.PaymentData") ?? throw new Exception("PaymentData type not found");
            var itemDataType = asm.GetType("PayOS.Types.ItemData");

            var itemsListType = typeof(List<>).MakeGenericType(itemDataType!);
            var items = Activator.CreateInstance(itemsListType);

            var paymentData = Activator.CreateInstance(
                paymentDataType,
                orderCode,
                amount,
                description,
                items,
                "http://localhost:5000/cancel",
                "http://localhost:5000/success");

            var resultTask = _payOS.createPaymentLink(paymentData);
            await resultTask;
            var result = resultTask.Result;

            return new PayOsPaymentResult
            {
                CheckoutUrl = result.checkoutUrl,
                OrderCode = orderCode
            };
        }

        public async Task<PayOsWebhookResult?> VerifyWebhookDataAsync(object webhookBody)
        {
            if (_payOS == null)
            {
                throw new Exception("PayOS configuration is missing (ClientId, ApiKey, or ChecksumKey). Cannot verify webhook data.");
            }

            await Task.Yield();
            try
            {
                var verifiedData = _payOS.verifyPaymentWebhookData(webhookBody);
                if (verifiedData == null) return null;

                return new PayOsWebhookResult
                {
                    Code = verifiedData.code,
                    OrderCode = (long)verifiedData.orderCode
                };
            }
            catch
            {
                return null;
            }
        }
    }
}

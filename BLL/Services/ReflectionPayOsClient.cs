using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AutoWashPro.BLL.Services;
using Microsoft.Extensions.Configuration;

namespace AutoWashPro.BLL.Services
{
    public class ReflectionPayOsClient : IPayOsClient
    {
        private readonly object _sdkInstance;
        private readonly MethodInfo _createPaymentLinkMethod;
        private readonly MethodInfo _verifyWebhookMethod;

        public ReflectionPayOsClient(IConfiguration configuration)
        {
            var clientId = configuration["PayOSConfig:ClientId"];
            var apiKey = configuration["PayOSConfig:ApiKey"];
            var checksum = configuration["PayOSConfig:ChecksumKey"];

            var asm = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name?.ToLower().Contains("payos") == true);

            if (asm == null)
            {
                try
                {
                    asm = Assembly.Load("payOS");
                }
                catch
                {
                    throw new Exception("PayOS assembly not found. Ensure the payOS NuGet package is restored.");
                }
            }

            var type = asm.GetTypes().FirstOrDefault(t =>
            {
                var ctors = t.GetConstructors();
                return ctors.Any(c =>
                {
                    var ps = c.GetParameters();
                    return ps.Length == 3 && ps.All(p => p.ParameterType == typeof(string));
                });
            });

            if (type == null) throw new Exception("No suitable PayOS SDK type found.");

            _sdkInstance = Activator.CreateInstance(type, clientId, apiKey, checksum)!;

            _createPaymentLinkMethod = type.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(m => m.Name.Equals("createPaymentLink", StringComparison.InvariantCultureIgnoreCase)
                    || m.Name.Equals("CreatePaymentLink", StringComparison.InvariantCultureIgnoreCase))!;

            _verifyWebhookMethod = type.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(m => m.Name.Contains("verify", StringComparison.InvariantCultureIgnoreCase) &&
                                      m.Name.Contains("webhook", StringComparison.InvariantCultureIgnoreCase))!;

            if (_createPaymentLinkMethod == null) throw new Exception("PayOS method 'createPaymentLink' not found.");
            if (_verifyWebhookMethod == null) throw new Exception("PayOS method 'verifyPaymentWebhookData' not found.");
        }

        public async Task<PaymentLinkResult> CreatePaymentLinkAsync(long orderCode, int amount, string description)
        {
            var paymentDataType = _createPaymentLinkMethod.GetParameters().FirstOrDefault()?.ParameterType;
            object param;

            if (paymentDataType != null && paymentDataType != typeof(object))
            {
                var pd = Activator.CreateInstance(paymentDataType)!;
                var orderProp = paymentDataType.GetProperties().FirstOrDefault(p => p.Name.Contains("order", StringComparison.InvariantCultureIgnoreCase));
                var amountProp = paymentDataType.GetProperties().FirstOrDefault(p => p.Name.Contains("amount", StringComparison.InvariantCultureIgnoreCase));
                var descProp = paymentDataType.GetProperties().FirstOrDefault(p => p.Name.Contains("desc", StringComparison.InvariantCultureIgnoreCase));
                var cancelUrlProp = paymentDataType.GetProperties().FirstOrDefault(p => p.Name.Contains("cancel", StringComparison.InvariantCultureIgnoreCase));
                var returnUrlProp = paymentDataType.GetProperties().FirstOrDefault(p => p.Name.Contains("return", StringComparison.InvariantCultureIgnoreCase));

                orderProp?.SetValue(pd, orderCode);
                amountProp?.SetValue(pd, amount);
                descProp?.SetValue(pd, description);
                cancelUrlProp?.SetValue(pd, "http://localhost:5000/cancel");
                returnUrlProp?.SetValue(pd, "http://localhost:5000/success");

                param = pd;
            }
            else
            {
                param = new { orderCode, amount, description, cancelUrl = "http://localhost:5000/cancel", returnUrl = "http://localhost:5000/success" };
            }

            var result = _createPaymentLinkMethod.Invoke(_sdkInstance, new object[] { param });

            if (result is Task task)
            {
                await task.ConfigureAwait(false);
                var resultProp = task.GetType().GetProperty("Result");
                result = resultProp?.GetValue(task);
            }

            if (result == null) throw new Exception("PayOS result is null.");

            var checkoutUrlProp = result.GetType().GetProperty("checkoutUrl") ?? result.GetType().GetProperty("CheckoutUrl");
            
            return new PaymentLinkResult
            {
                CheckoutUrl = checkoutUrlProp?.GetValue(result)?.ToString() ?? string.Empty,
                OrderCode = orderCode
            };
        }

        public async Task<WebhookVerificationResult?> VerifyWebhookAsync(object webhookBody)
        {
            var result = _verifyWebhookMethod.Invoke(_sdkInstance, new object[] { webhookBody });
            if (result == null) return null;

            var codeProp = result.GetType().GetProperty("code") ?? result.GetType().GetProperty("Code");
            var orderCodeProp = result.GetType().GetProperty("orderCode") ?? result.GetType().GetProperty("OrderCode");

            return new WebhookVerificationResult
            {
                Code = codeProp?.GetValue(result)?.ToString() ?? string.Empty,
                OrderCode = (long)(orderCodeProp?.GetValue(result) ?? 0L)
            };
        }
    }
}

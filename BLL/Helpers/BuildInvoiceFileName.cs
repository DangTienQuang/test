using BLL.DTOs.Business;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Helpers
{
    public static class InvoiceFileNameHelper
    {
        public static string BuildInvoiceFileName(InvoiceExportDTO invoice)
        {
            var businessName = SanitizeFileName(invoice.BusinessName);

            var generateDate = DateTime.UtcNow.ToString("yyyyMMdd");

            return
                $"invoice-{invoice.BillingPeriod}-{generateDate}-{businessName}.pdf";
        }

        private static string SanitizeFileName(string value)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(c, '-');
            }

            return value.Trim();
        }
    }
}
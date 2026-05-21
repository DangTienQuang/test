using System;
using System.Collections.Generic;

namespace AutoWashPro.BLL.DTOs
{
    public class WalletResponseDTO
    {
        public decimal Balance { get; set; }
        public int TotalPoints { get; set; }
    }

    public class TopUpRequestDTO
    {
        public decimal Amount { get; set; }
        public required string CancelUrl { get; set; }
        public required string ReturnUrl { get; set; }
    }

    public class TopUpResponseDTO
    {
        public required string PaymentUrl { get; set; }
        public required string OrderCode { get; set; }
    }

    public class TransactionResponseDTO
    {
        public int TransactionId { get; set; }
        public decimal Amount { get; set; }
        public required string TransactionType { get; set; }
        public required string Description { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class PointHistoryResponseDTO
    {
        public int LedgerId { get; set; }
        public int PointsAdded { get; set; }
        public int PointsDeducted { get; set; }
        public required string Reason { get; set; }
        public DateTime TransactionDate { get; set; }
    }

    public class WebhookTopUpDTO
    {
        public string Code { get; set; } = "00";
        public WebhookDataDTO? Data { get; set; }
        public string Signature { get; set; } = "test-signature-ignore-on-local";
    }

    public class WebhookDataDTO
    {
        public long OrderCode { get; set; }
        public int Amount { get; set; }
        public string Description { get; set; } = "";
    }
}

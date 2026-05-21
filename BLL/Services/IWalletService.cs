using System.Collections.Generic;
using System.Threading.Tasks;
using AutoWashPro.BLL.DTOs;

namespace AutoWashPro.BLL.Services
{
    public interface IWalletService
    {
        Task<WalletResponseDTO> GetWalletInfoAsync(int userId);
        Task<TopUpResponseDTO> CreateTopUpLinkAsync(int userId, TopUpRequestDTO request);
        Task ProcessPaymentWebhookAsync(WebhookTopUpDTO webhookData);
        Task<List<TransactionResponseDTO>> GetTransactionsAsync(int userId);
        Task<List<PointHistoryResponseDTO>> GetPointsHistoryAsync(int userId);
        Task DeductPointsFIFOAsync(int userId, int pointsToDeduct, string reason);
    }
}

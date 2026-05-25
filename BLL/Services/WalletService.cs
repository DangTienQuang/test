using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoWashPro.BLL.Constants;
using AutoWashPro.BLL.DTOs;
using AutoWashPro.DAL.Data;
using AutoWashPro.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using PayOS;
using PayOS.Models.V2.PaymentRequests;
using Microsoft.Extensions.Logging;
using AutoWashPro.BLL.Exceptions;

namespace AutoWashPro.BLL.Services
{
    public class WalletService : IWalletService
    {
        private readonly AutoWashDbContext _context;
        private readonly PayOSClient _payOSClient;
        private readonly ILogger<WalletService> _logger;

        public WalletService(AutoWashDbContext context, PayOSClient payOSClient, ILogger<WalletService> logger)
        {
            _context = context;
            _payOSClient = payOSClient;
            _logger = logger;
        }

        public async Task<WalletResponseDTO> GetWalletInfoAsync(int userId)
        {
            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
            if (wallet == null)
            {
                wallet = new Wallet { UserId = userId, Balance = 0, Status = "Active" };
                _context.Wallets.Add(wallet);
                await _context.SaveChangesAsync();
            }

            var profile = await _context.CustomerProfiles.FirstOrDefaultAsync(cp => cp.UserId == userId);

            return new WalletResponseDTO
            {
                Balance = wallet.Balance,
                TotalPoints = profile?.TotalPoint ?? 0,
                PromotionPoints = profile?.PromotionPoint ?? 0
            };
        }

        public async Task<TopUpResponseDTO> CreateTopUpLinkAsync(int userId, TopUpRequestDTO request)
        {
            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
            if (wallet == null)
            {
                wallet = new Wallet { UserId = userId, Balance = 0, Status = "Active" };
                _context.Wallets.Add(wallet);
                await _context.SaveChangesAsync();
            }

            var orderCode = DateTimeOffset.Now.ToUnixTimeSeconds();

            var transaction = new Transaction
            {
                WalletId = wallet.WalletId,
                Amount = request.Amount,
                TransactionType = "Topup",
                Description = "Yêu cầu nạp tiền",
                OrderCode = orderCode.ToString(),
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            var paymentRequest = new CreatePaymentLinkRequest
            {
                OrderCode = orderCode,
                Amount = (int)request.Amount,
                Description = $"Topup wallet",
                CancelUrl = request.CancelUrl,
                ReturnUrl = request.ReturnUrl
            };

            var createPaymentResult = await _payOSClient.PaymentRequests.CreateAsync(paymentRequest);

            return new TopUpResponseDTO
            {
                PaymentUrl = createPaymentResult.CheckoutUrl,
                OrderCode = orderCode.ToString()
            };
        }

        public async Task ProcessPaymentWebhookAsync(WebhookTopUpDTO webhookData)
        {
            if (webhookData.Code != "00" || webhookData.Data == null)
            {
                _logger.LogWarning("Webhook báo lỗi hoặc không có dữ liệu. Code: {Code}", webhookData.Code);
                return;
            }

            var data = webhookData.Data;
            var orderCodeStr = data.OrderCode.ToString();

            using var dbTransaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            try
            {
                var transaction = await _context.Transactions
                    .Include(t => t.Wallet)
                    .FirstOrDefaultAsync(t => t.OrderCode == orderCodeStr && t.TransactionType == "Topup");

                if (transaction == null)
                {
                    _logger.LogWarning("Không tìm thấy giao dịch với OrderCode: {OrderCode}", data.OrderCode);
                    return;
                }

                if (transaction.Status == "Completed")
                {
                    _logger.LogInformation("Giao dịch {OrderCode} đã được xử lý trước đó.", data.OrderCode);
                    return;
                }

                transaction.Status = "Completed";
                transaction.Description = $"Nạp tiền thành công (Mã: {data.OrderCode})";
                transaction.Amount = data.Amount; // Ensure amount matches webhook data

                transaction.Wallet.Balance += data.Amount;

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                _logger.LogInformation("Cập nhật số dư thành công cho Wallet {WalletId}. Số tiền: {Amount}", transaction.WalletId, data.Amount);
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi khi xử lý webhook thanh toán cho OrderCode: {OrderCode}", data.OrderCode);
                throw;
            }
        }

        public async Task<List<TransactionResponseDTO>> GetTransactionsAsync(int userId)
        {
            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
            if (wallet == null) return new List<TransactionResponseDTO>();

            return await _context.Transactions
                .Where(t => t.WalletId == wallet.WalletId)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new TransactionResponseDTO
                {
                    TransactionId = t.TransactionId,
                    Amount = t.Amount,
                    TransactionType = t.TransactionType,
                    Description = t.Description,
                    CreatedAt = t.CreatedAt
                }).ToListAsync();
        }

        public async Task<List<PointHistoryResponseDTO>> GetPointsHistoryAsync(int userId)
        {
            return await _context.PointLedgers
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.TransactionDate)
                .Select(p => new PointHistoryResponseDTO
                {
                    LedgerId = p.LedgerId,
                    PointsAdded = p.PointsAdded,
                    PointsDeducted = p.PointsDeducted,
                    Reason = p.Reason,
                    TransactionDate = p.TransactionDate
                }).ToListAsync();
        }

        public async Task DeductSpendablePointsAsync(int userId, int pointsToDeduct, string reason)
        {
            if (pointsToDeduct <= 0) return;

            var profile = await _context.CustomerProfiles.FirstOrDefaultAsync(cp => cp.UserId == userId);
            if (profile == null) throw new NotFoundException("Không tìm thấy hồ sơ khách hàng.");

            if (profile.TotalPoint < pointsToDeduct)
                throw new BadRequestException($"Không đủ điểm khả dụng. Bạn có {profile.TotalPoint} điểm.");

            profile.TotalPoint -= pointsToDeduct;

            _context.PointLedgers.Add(new PointLedger
            {
                UserId = userId,
                PointsDeducted = pointsToDeduct,
                Reason = reason,
                TransactionDate = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }

        public async Task RefundSpendablePointsAsync(int userId, int pointsToRefund, string reason, int? referenceBookingId = null)
        {
            if (pointsToRefund <= 0) return;

            var profile = await _context.CustomerProfiles.FirstOrDefaultAsync(cp => cp.UserId == userId);
            if (profile == null) throw new NotFoundException("Không tìm thấy hồ sơ khách hàng.");

            profile.TotalPoint += pointsToRefund;

            _context.PointLedgers.Add(new PointLedger
            {
                UserId = userId,
                PointsAdded = pointsToRefund,
                Reason = reason,
                ReferenceBookingId = referenceBookingId,
                TransactionDate = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }

        public async Task<int> AwardCompletionPointsAsync(int userId, int pointsEarned, int bookingId)
        {
            if (pointsEarned <= 0) return 0;

            var profile = await _context.CustomerProfiles.FirstOrDefaultAsync(cp => cp.UserId == userId);
            if (profile == null) throw new NotFoundException("Không tìm thấy hồ sơ khách hàng.");

            profile.TotalPoint += pointsEarned;
            profile.PromotionPoint += pointsEarned;

            _context.PointLedgers.Add(new PointLedger
            {
                UserId = userId,
                PointsAdded = pointsEarned,
                Reason = $"{PointConstants.CompletionReasonPrefix} #{bookingId}",
                ExpiryDate = DateTime.UtcNow.AddMonths(12),
                ReferenceBookingId = bookingId,
                TransactionDate = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return pointsEarned;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoWashPro.BLL.DTOs;
using AutoWashPro.DAL.Data;
using AutoWashPro.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using PayOS;
using PayOS.Models.V2.PaymentRequests;
using Microsoft.Extensions.Logging;

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

            var now = DateTime.UtcNow;
            var totalAdded = await _context.PointLedgers
                .Where(p => p.UserId == userId && p.PointsAdded > 0 && (p.ExpiryDate == null || p.ExpiryDate > now))
                .SumAsync(p => p.PointsAdded);

            var totalDeducted = await _context.PointLedgers
                .Where(p => p.UserId == userId && p.PointsDeducted > 0)
                .SumAsync(p => p.PointsDeducted);

            var availablePoints = Math.Max(0, totalAdded - totalDeducted);

            return new WalletResponseDTO
            {
                Balance = wallet.Balance,
                TotalPoints = availablePoints
            };
        }

        public async Task<TopUpResponseDTO> CreateTopUpLinkAsync(int userId, TopUpRequestDTO request)
        {
            var orderCode = DateTimeOffset.Now.ToUnixTimeSeconds();
            var paymentRequest = new CreatePaymentLinkRequest
            {
                OrderCode = orderCode,
                Amount = (int)request.Amount,
                Description = $"Topup wallet {userId}",
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
            var alreadyProcessed = await _context.Transactions
                .AnyAsync(t => t.Description.Contains($"(Mã: {orderCodeStr})") && t.TransactionType == "Topup");
            
            if (alreadyProcessed) 
            {
                _logger.LogWarning("Giao dịch {OrderCode} đã được xử lý trước đó.", data.OrderCode);
                return;
            }

            int userId = 0;
            var desc = data.Description ?? "";
            var match = System.Text.RegularExpressions.Regex.Match(desc, @"\d+$");
            if (match.Success)
            {
                int.TryParse(match.Value, out userId);
            }

            if (userId > 0)
            {
                var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
                if (wallet == null)
                {
                    wallet = new Wallet { UserId = userId, Balance = 0, Status = "Active" };
                    _context.Wallets.Add(wallet);
                }

                wallet.Balance += data.Amount;
                
                var transaction = new Transaction
                {
                    WalletId = wallet.WalletId,
                    Amount = data.Amount,
                    TransactionType = "Topup",
                    Description = $"Nạp tiền thành công (Mã: {data.OrderCode})",
                    CreatedAt = DateTime.UtcNow
                };
                
                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Cập nhật số dư thành công cho User {UserId}. Số tiền: {Amount}", userId, data.Amount);
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

        public async Task DeductPointsFIFOAsync(int userId, int pointsToDeduct, string reason)
        {
            if (pointsToDeduct <= 0) return;

            var now = DateTime.UtcNow;

            var activePoints = await _context.PointLedgers
                .Where(p => p.UserId == userId && p.PointsAdded > 0 && (p.ExpiryDate == null || p.ExpiryDate > now))
                .OrderBy(p => p.ExpiryDate)
                .ToListAsync();

            var totalDeductedSoFar = await _context.PointLedgers
                .Where(p => p.UserId == userId && p.PointsDeducted > 0)
                .SumAsync(p => p.PointsDeducted);

            var totalAdded = activePoints.Sum(p => p.PointsAdded);
            var availablePoints = totalAdded - totalDeductedSoFar;

            if (availablePoints < pointsToDeduct)
                throw new Exception($"Không đủ điểm khả dụng. Bạn có {availablePoints} điểm (không tính điểm đã hết hạn).");

            var ledger = new PointLedger
            {
                UserId = userId,
                PointsDeducted = pointsToDeduct,
                Reason = reason,
                TransactionDate = now
            };

            _context.PointLedgers.Add(ledger);
            await _context.SaveChangesAsync();
        }
    }
}

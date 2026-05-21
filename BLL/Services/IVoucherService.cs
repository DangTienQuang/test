using System.Collections.Generic;
using System.Threading.Tasks;
using AutoWashPro.BLL.DTOs;

namespace AutoWashPro.BLL.Services
{
    public interface IVoucherService
    {
        Task<List<VoucherResponseDTO>> GetMyVouchersAsync(int userId);
        Task RedeemVoucherAsync(int userId, int voucherId);
    }
}

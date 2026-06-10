using System.Threading.Tasks;
using AutoWashPro.BLL.DTOs;

namespace AutoWashPro.BLL.Services
{
    public interface IAuthService
    {
        Task<RegisterPendingResponseDTO> RegisterAsync(RegisterDTO request);
        Task<AuthResponseDTO> VerifyOtpAsync(VerifyOtpDTO request);
        Task<AuthResponseDTO> LoginAsync(LoginDTO request);
        Task<AuthResponseDTO> RefreshTokenAsync(RefreshTokenDTO request);
        Task<bool> ChangePasswordAsync(int userId, ChangePasswordDTO request);
        Task<RegisterPendingResponseDTO> ResendOtpAsync(ResendOtpDTO request);
    }
}

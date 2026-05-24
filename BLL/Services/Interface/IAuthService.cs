using System.Threading.Tasks;
using AutoWashPro.BLL.DTOs;

namespace AutoWashPro.BLL.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDTO> RegisterAsync(RegisterDTO request);
        Task<AuthResponseDTO> LoginAsync(LoginDTO request);
        Task<AuthResponseDTO> RefreshTokenAsync(RefreshTokenDTO request);
        Task<bool> ChangePasswordAsync(int userId, ChangePasswordDTO request);
    }
}
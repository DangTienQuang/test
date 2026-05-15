using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoWashPro.BLL.DTOs;
using System.Threading.Tasks;

namespace AutoWashPro.BLL.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDTO> RegisterAsync(RegisterDTO request);
        Task<AuthResponseDTO> LoginAsync(LoginDTO request);
    }
}

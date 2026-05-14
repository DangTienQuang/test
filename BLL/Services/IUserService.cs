using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoWashPro.BLL.DTOs;
using System.Threading.Tasks;

namespace AutoWashPro.BLL.Services
{
    public interface IUserService
    {
        Task<UserProfileDTO> GetProfileAsync(int userId);
        Task<UserProfileDTO> UpdateProfileAsync(int userId, UpdateProfileDTO request);
        Task<PaginatedResponseDTO<UserProfileDTO>> GetAllUsersAsync(int pageIndex, int pageSize, string? searchName, string? searchPhone, string? searchPlate, int? tierId, string? status);
        Task<UserProfileDTO> GetUserByIdAsync(int userId);
        Task<bool> ChangeUserStatusAsync(int userId, string status);
    }
}
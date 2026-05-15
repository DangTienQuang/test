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
        Task<bool> AddVehicleAsync(int userId, CreateVehicleDTO request);
    }
}
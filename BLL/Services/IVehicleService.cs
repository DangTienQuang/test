using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoWashPro.BLL.DTOs;

namespace AutoWashPro.BLL.Services
{
    public interface IVehicleService
    {
        Task<List<VehicleDTO>> GetVehiclesAsync(int userId);
        Task<bool> AddVehicleAsync(int userId, CreateVehicleDTO request);
        Task<bool> UpdateVehicleAsync(int userId, string licensePlate, UpdateVehicleDTO request);
        Task<bool> DeleteVehicleAsync(int userId, string licensePlate);
    }
}

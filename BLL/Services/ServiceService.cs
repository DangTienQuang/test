using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoWashPro.BLL.DTOs;
using AutoWashPro.DAL.Data;
using AutoWashPro.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace AutoWashPro.BLL.Services
{
    public class ServiceService : IServiceService
    {
        private readonly AutoWashDbContext _context;

        public ServiceService(AutoWashDbContext context)
        {
            _context = context;
        }

        public async Task<List<ServiceDTO>> GetActiveServicesAsync()
        {
            var services = await _context.Services
                .AsNoTracking() // ⚡ Bolt: Prevent unnecessary entity tracking for read-only projection
                .Include(s => s.ServicePrices)
                    .ThenInclude(sp => sp.VehicleType)
                .Where(s => s.IsActive) 
                .ToListAsync();

            return services.Select(s => MapToDTO(s)).ToList();
        }

        public async Task<List<ServiceDTO>> GetAllServicesAsync()
        {
            var services = await _context.Services
                .AsNoTracking() // ⚡ Bolt: Prevent unnecessary entity tracking for read-only projection
                .Include(s => s.ServicePrices)
                    .ThenInclude(sp => sp.VehicleType)
                .ToListAsync();

            return services.Select(s => MapToDTO(s)).ToList();
        }

        public async Task<ServiceDTO> GetServiceByIdAsync(int id)
        {
            var service = await _context.Services
                .AsNoTracking() // ⚡ Bolt: Prevent unnecessary entity tracking for read-only projection
                .Include(s => s.ServicePrices)
                    .ThenInclude(sp => sp.VehicleType)
                .FirstOrDefaultAsync(s => s.ServiceId == id);

            if (service == null) throw new Exception("Không tìm thấy dịch vụ.");
            return MapToDTO(service);
        }

        public async Task<ServiceDTO> CreateServiceAsync(CreateOrUpdateServiceDTO request)
        {
            var vehicleTypeIds = request.Prices.Select(p => p.VehicleTypeId).Distinct().ToList();
            var existingTypesCount = await _context.VehicleTypes.CountAsync(vt => vehicleTypeIds.Contains(vt.Id));
            if (existingTypesCount != vehicleTypeIds.Count) throw new Exception("Một hoặc nhiều loại xe không hợp lệ.");

            var service = new Service
            {
                ServiceName = request.ServiceName,
                Description = request.Description,
                IsActive = true,
                ServicePrices = request.Prices.Select(p => new ServicePrice
                {
                    VehicleTypeId = p.VehicleTypeId,
                    Price = p.Price,
                    DurationMinutes = p.DurationMinutes
                }).ToList()
            };

            _context.Services.Add(service);
            await _context.SaveChangesAsync();

            return await GetServiceByIdAsync(service.ServiceId);
        }

        public async Task<bool> UpdateServiceAsync(int id, CreateOrUpdateServiceDTO request)
        {
            var service = await _context.Services
                .Include(s => s.ServicePrices)
                .FirstOrDefaultAsync(s => s.ServiceId == id);

            if (service == null) throw new Exception("Không tìm thấy dịch vụ.");

            var vehicleTypeIds = request.Prices.Select(p => p.VehicleTypeId).Distinct().ToList();
            var existingTypesCount = await _context.VehicleTypes.CountAsync(vt => vehicleTypeIds.Contains(vt.Id));
            if (existingTypesCount != vehicleTypeIds.Count) throw new Exception("Một hoặc nhiều loại xe không hợp lệ.");

            service.ServiceName = request.ServiceName;
            service.Description = request.Description;

            _context.ServicePrices.RemoveRange(service.ServicePrices);

            service.ServicePrices = request.Prices.Select(p => new ServicePrice
            {
                VehicleTypeId = p.VehicleTypeId,
                Price = p.Price,
                DurationMinutes = p.DurationMinutes
            }).ToList();

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteServiceAsync(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service == null) throw new Exception("Không tìm thấy dịch vụ.");
            service.IsActive = !service.IsActive;
            await _context.SaveChangesAsync();
            return true;
        }

        private static ServiceDTO MapToDTO(Service service)
        {
            return new ServiceDTO
            {
                ServiceId = service.ServiceId,
                ServiceName = service.ServiceName,
                Description = service.Description,
                IsActive = service.IsActive,
                Prices = service.ServicePrices.Select(sp => new ServicePriceDTO
                {
                    VehicleTypeId = sp.VehicleTypeId,
                    VehicleTypeName = sp.VehicleType?.Name ?? "N/A",
                    Price = sp.Price,
                    DurationMinutes = sp.DurationMinutes
                }).ToList()
            };
        }
    }
}
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

        public async Task<List<ServiceResponseDTO>> GetServicesAsync()
        {
            return await _context.Services
                .Select(s => new ServiceResponseDTO
                {
                    ServiceId = s.ServiceId,
                    ServiceName = s.ServiceName,
                    BasePrice = s.BasePrice,
                    DurationMinutes = s.DurationMinutes,
                    Description = s.Description
                }).ToListAsync();
        }

        public async Task<ServiceResponseDTO> GetServiceByIdAsync(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service == null) throw new Exception("Service not found.");

            return new ServiceResponseDTO
            {
                ServiceId = service.ServiceId,
                ServiceName = service.ServiceName,
                BasePrice = service.BasePrice,
                DurationMinutes = service.DurationMinutes,
                Description = service.Description
            };
        }

        public async Task<ServiceResponseDTO> CreateServiceAsync(CreateServiceDTO request)
        {
            var service = new Service
            {
                ServiceName = request.ServiceName,
                BasePrice = request.BasePrice,
                DurationMinutes = request.DurationMinutes,
                Description = request.Description
            };

            _context.Services.Add(service);
            await _context.SaveChangesAsync();

            return new ServiceResponseDTO
            {
                ServiceId = service.ServiceId,
                ServiceName = service.ServiceName,
                BasePrice = service.BasePrice,
                DurationMinutes = service.DurationMinutes,
                Description = service.Description
            };
        }

        public async Task<ServiceResponseDTO> UpdateServiceAsync(int id, UpdateServiceDTO request)
        {
            var service = await _context.Services.FindAsync(id);
            if (service == null) throw new Exception("Service not found.");

            service.ServiceName = request.ServiceName;
            service.BasePrice = request.BasePrice;
            service.DurationMinutes = request.DurationMinutes;
            service.Description = request.Description;

            await _context.SaveChangesAsync();

            return new ServiceResponseDTO
            {
                ServiceId = service.ServiceId,
                ServiceName = service.ServiceName,
                BasePrice = service.BasePrice,
                DurationMinutes = service.DurationMinutes,
                Description = service.Description
            };
        }

        public async Task<bool> DeleteServiceAsync(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service == null) throw new Exception("Service not found.");

            // Check if service is used in any bookings before deleting
            var hasBookings = await _context.Bookings.AnyAsync(b => b.ServiceId == id);
            if (hasBookings) throw new Exception("Cannot delete a service that has existing bookings.");

            _context.Services.Remove(service);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}

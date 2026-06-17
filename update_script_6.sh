cat << 'INNER_EOF' > BLL/Services/CarModelService.cs
using AutoWashPro.BLL.DTOs;
using AutoWashPro.DAL.Data;
using AutoWashPro.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoWashPro.BLL.Exceptions;

namespace AutoWashPro.BLL.Services
{
    public class CarModelService : ICarModelService
    {
        private readonly AutoWashDbContext _context;
        private readonly ILogger<CarModelService> _logger;

        public CarModelService(AutoWashDbContext context, ILogger<CarModelService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<CarModelDTO>> GetActiveCarModelsAsync()
        {
            return await _context.CarModels
                .Where(c => c.IsActive && c.Status == "Approved")
                .OrderBy(c => c.Brand).ThenBy(c => c.Name)
                .Select(c => new CarModelDTO
                {
                    Id = c.Id,
                    Brand = c.Brand,
                    Name = c.Name,
                    Status = c.Status,
                    RequestedByUserId = c.RequestedByUserId,
                    VehicleTypeId = c.VehicleTypeId
                })
                .ToListAsync();
        }

        public async Task<bool> CreateCarModelAsync(CreateCarModelDTO request)
        {
            var newModel = new CarModel
            {
                Brand = request.Brand,
                Name = request.Name,
                Status = "Approved",
                VehicleTypeId = request.VehicleTypeId
            };
            _context.CarModels.Add(newModel);
            var result = await _context.SaveChangesAsync();
            return result > 0;
        }

        public async Task<bool> UpdateCarModelAsync(int id, UpdateCarModelDTO request)
        {
            var model = await _context.CarModels.FindAsync(id);
            if (model == null) return false;

            model.Brand = request.Brand;
            model.Name = request.Name;
            model.IsActive = request.IsActive;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteCarModelAsync(int id)
        {
            var model = await _context.CarModels.FindAsync(id);
            if (model == null) return false;

            model.IsActive = false;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> RequestNewCarModelAsync(int userId, RequestCarModelDTO request)
        {
            var newModel = new CarModel
            {
                Brand = request.Brand.Trim(),
                Name = request.Name.Trim(),
                Status = "Pending",
                IsActive = true,
                RequestedByUserId = userId,
                VehicleTypeId = request.VehicleTypeId
            };

            _context.CarModels.Add(newModel);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"[Staff Notification] New vehicle model '{newModel.Brand} {newModel.Name}' requires verification (ID: {newModel.Id}).");

            return newModel.Id;
        }

        public async Task<List<CarModelDTO>> GetPendingCarModelsAsync()
        {
            return await _context.CarModels
                .Where(c => c.IsActive && c.Status == "Pending")
                .OrderByDescending(c => c.Id)
                .Select(c => new CarModelDTO
                {
                    Id = c.Id,
                    Brand = c.Brand,
                    Name = c.Name,
                    Status = c.Status,
                    RequestedByUserId = c.RequestedByUserId,
                    VehicleTypeId = c.VehicleTypeId
                })
                .ToListAsync();
        }

        public async Task<bool> ApproveCarModelAsync(int id, ApproveCarModelDTO request)
        {
            var model = await _context.CarModels.FindAsync(id);
            if (model == null) throw new NotFoundException("Không tìm thấy dòng xe.");
            if (model.Status != "Pending") throw new BadRequestException("Chỉ có thể phê duyệt dòng xe đang chờ duyệt.");

            var vehicleTypeExists = await _context.VehicleTypes.AnyAsync(vt => vt.Id == request.VehicleTypeId);
            if (!vehicleTypeExists) throw new BadRequestException("Loại xe không hợp lệ.");

            model.Status = "Approved";
            model.VehicleTypeId = request.VehicleTypeId;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RejectCarModelAsync(int id)
        {
            var model = await _context.CarModels.FindAsync(id);
            if (model == null) throw new NotFoundException("Không tìm thấy dòng xe.");
            if (model.Status != "Pending") throw new BadRequestException("Chỉ có thể từ chối dòng xe đang chờ duyệt.");

            model.Status = "Rejected";
            model.IsActive = false;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
INNER_EOF

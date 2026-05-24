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
    public class VehicleTypeService : IVehicleTypeService
    {
        private readonly AutoWashDbContext _context;

        public VehicleTypeService(AutoWashDbContext context)
        {
            _context = context;
        }

        public async Task<List<VehicleTypeDTO>> GetAllAsync()
        {
            return await _context.VehicleTypes
                .AsNoTracking()
                .Select(t => new VehicleTypeDTO
                {
                    Id = t.Id,
                    Name = t.Name,
                    Description = t.Description
                }).ToListAsync();
        }

        public async Task<VehicleTypeDTO> CreateAsync(CreateVehicleTypeDTO request)
        {
            var typeExists = await _context.VehicleTypes.AnyAsync(t => t.Name.ToLower() == request.Name.ToLower());
            if (typeExists) throw new Exception("Loại xe này đã tồn tại.");

            var type = new VehicleType
            {
                Name = request.Name,
                Description = request.Description
            };

            _context.VehicleTypes.Add(type);
            await _context.SaveChangesAsync();

            return new VehicleTypeDTO { Id = type.Id, Name = type.Name, Description = type.Description };
        }

        public async Task<bool> UpdateAsync(int id, CreateVehicleTypeDTO request)
        {
            var type = await _context.VehicleTypes.FindAsync(id);
            if (type == null) throw new Exception("Không tìm thấy loại xe.");

            var typeExists = await _context.VehicleTypes.AnyAsync(t => t.Id != id && t.Name.ToLower() == request.Name.ToLower());
            if (typeExists) throw new Exception("Tên loại xe đã bị trùng.");

            type.Name = request.Name;
            type.Description = request.Description;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var type = await _context.VehicleTypes.Include(t => t.Vehicles).FirstOrDefaultAsync(t => t.Id == id);
            if (type == null) throw new Exception("Không tìm thấy loại xe.");

            if (type.Vehicles.Any()) throw new Exception("Không thể xóa loại xe này vì đã có phương tiện của khách hàng đang sử dụng.");

            _context.VehicleTypes.Remove(type);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
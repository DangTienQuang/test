using AutoWashPro.BLL.DTOs;
using AutoWashPro.DAL.Data;
using AutoWashPro.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoWashPro.BLL.Services
{
    public class CarModelService : ICarModelService
    {
        private readonly AutoWashDbContext _context;

        public CarModelService(AutoWashDbContext context)
        {
            _context = context;
        }

        public async Task<List<CarModelDTO>> GetActiveCarModelsAsync()
        {
            return await _context.CarModels
                .Where(c => c.IsActive)
                .OrderBy(c => c.Brand).ThenBy(c => c.Name)
                .Select(c => new CarModelDTO { Id = c.Id, Brand = c.Brand, Name = c.Name })
                .ToListAsync();
        }

        public async Task<bool> CreateCarModelAsync(CreateCarModelDTO request)
        {
            var newModel = new CarModel { Brand = request.Brand, Name = request.Name };
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
    }
}

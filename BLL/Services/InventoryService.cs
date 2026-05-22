using AutoWashPro.DAL.Data;
using AutoWashPro.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace AutoWashPro.BLL.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly AutoWashDbContext _context;

        public InventoryService(AutoWashDbContext context)
        {
            _context = context;
        }

        public async Task DeductInventoryForServiceAsync(int serviceId)
        {
            var requirements = await _context.ServiceInventoryRequirements
                .Where(r => r.ServiceId == serviceId)
                .Include(r => r.InventoryItem)
                .ToListAsync();

            if (!requirements.Any()) return;

            foreach (var req in requirements)
            {
                var item = req.InventoryItem;
                if (item != null)
                {
                    item.Quantity -= req.QuantityRequired;
                    if (item.Quantity < 0) item.Quantity = 0;
                    item.LastUpdated = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task<List<InventoryItem>> GetInventoryStatusAsync()
        {
            return await _context.InventoryItems.ToListAsync();
        }

        public async Task AddInventoryItemAsync(InventoryItem item)
        {
            _context.InventoryItems.Add(item);
            await _context.SaveChangesAsync();
        }
    }
}

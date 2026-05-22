using System.Collections.Generic;
using System.Threading.Tasks;
using AutoWashPro.DAL.Entities;

namespace AutoWashPro.BLL.Services
{
    public interface IInventoryService
    {
        Task DeductInventoryForServiceAsync(int serviceId);
        Task<List<InventoryItem>> GetInventoryStatusAsync();
        Task AddInventoryItemAsync(InventoryItem item);
    }
}

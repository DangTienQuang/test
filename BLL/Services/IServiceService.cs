using System.Collections.Generic;
using System.Threading.Tasks;
using AutoWashPro.BLL.DTOs;

namespace AutoWashPro.BLL.Services
{
    public interface IServiceService
    {
        Task<List<ServiceResponseDTO>> GetServicesAsync();
        Task<ServiceResponseDTO> GetServiceByIdAsync(int id);
        Task<ServiceResponseDTO> CreateServiceAsync(CreateServiceDTO request);
        Task<ServiceResponseDTO> UpdateServiceAsync(int id, UpdateServiceDTO request);
        Task<bool> DeleteServiceAsync(int id);
    }
}

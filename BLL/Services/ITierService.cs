using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoWashPro.BLL.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoWashPro.BLL.Services
{
    public interface ITierService
    {
        Task<TierResponseDTO> CreateTierAsync(CreateTierDTO request);
        Task<List<TierResponseDTO>> GetAllTiersAsync();
    }
}
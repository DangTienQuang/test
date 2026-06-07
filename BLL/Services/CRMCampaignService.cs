using System.Collections.Generic;
using System.Threading.Tasks;
using AutoWashPro.BLL.DTOs;

namespace AutoWashPro.BLL.Services
{
    public class CRMCampaignService : ICRMCampaignService
    {
        private readonly IVoucherCampaignService _voucherCampaignService;

        public CRMCampaignService(IVoucherCampaignService voucherCampaignService)
        {
            _voucherCampaignService = voucherCampaignService;
        }

        public async Task<List<VoucherCampaignProcessResultDTO>> ProcessDailyCampaignsAsync()
        {
            return await _voucherCampaignService.ProcessDailyCampaignsAsync();
        }
    }
}

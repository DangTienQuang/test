using System.Threading.Tasks;

namespace AutoWashPro.BLL.Services
{
    public interface ICRMCampaignService
    {
        Task RunBirthdayCampaignAsync();
        Task RunWinbackCampaignAsync();
    }
}

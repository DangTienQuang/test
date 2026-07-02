using System.Threading.Tasks;

namespace AutoWashPro.BLL.Services.Interface
{
    public interface IWeatherService
    {
        Task<bool> IsRainingNowAsync();
    }
}
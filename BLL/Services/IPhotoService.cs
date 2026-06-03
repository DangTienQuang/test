using Microsoft.AspNetCore.Http;

namespace BLL.Services
{
    public interface IPhotoService
    {
        Task<string> UploadImageAsync(IFormFile file);
    }
}
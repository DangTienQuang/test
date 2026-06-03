namespace BLL.Services
{
    public interface IPhotoService
    {
        Task<string> UploadImageAsync(System.IO.Stream fileStream, string fileName);
    }
}
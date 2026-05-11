namespace PixPro.Services.Projects.Application.Services;

public interface IStorageService
{
    Task<StorageUploadResult> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task DeleteAsync(string publicId, CancellationToken cancellationToken = default);
}

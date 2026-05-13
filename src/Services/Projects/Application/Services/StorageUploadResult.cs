namespace PixPro.Services.Projects.Application.Services;

public sealed record StorageUploadResult(
    string PublicId,
    string Url,
    string SecureUrl,
    long Bytes,
    string Format,
    int Width,
    int Height
);

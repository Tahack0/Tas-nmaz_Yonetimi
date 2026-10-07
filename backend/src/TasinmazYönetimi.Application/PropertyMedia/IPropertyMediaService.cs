using TasinmazYönetimi.Domain.Enums;

namespace TasinmazYönetimi.Application.PropertyMedia;

public record PropertyMediaResponse(
    Guid Id, string FileName, string ContentType, long FileSize, DateTime CreatedAt);

public record PropertyMediaFile(Stream Content, string ContentType);

// Dosya klasörü Program.cs üzerinden yapılandırılır.
public sealed class PropertyMediaOptions
{
    public string RootDirectory { get; set; } = string.Empty;
    public const long MaxFileSize = 100 * 1024 * 1024;
}

public interface IPropertyMediaService
{
    Task<PropertyMediaResponse?> UploadAsync(Guid userId, Guid propertyId,
        string fileName, Stream content, long length, CancellationToken cancellationToken);
    Task<List<PropertyMediaResponse>?> ListAsync(Guid userId, UserRole role,
        Guid propertyId, CancellationToken cancellationToken);
    Task<PropertyMediaFile?> OpenAsync(Guid userId, UserRole role,
        Guid propertyId, Guid mediaId, CancellationToken cancellationToken);
}

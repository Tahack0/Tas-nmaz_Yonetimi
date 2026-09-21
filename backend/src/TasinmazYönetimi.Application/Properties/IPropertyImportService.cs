namespace TasinmazYönetimi.Application.Properties;

public interface IPropertyImportService
{
    Task<PropertyImportResponse> ImportAsync(
        Guid userId,
        Stream fileStream,
        CancellationToken cancellationToken);
}
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TasinmazYönetimi.Application.Auditing;
using TasinmazYönetimi.Application.PropertyMedia;
using TasinmazYönetimi.Domain.Enums;
using TasinmazYönetimi.Infrastructure.Persistence;
using MediaEntity = TasinmazYönetimi.Domain.Entities.PropertyMedia;

namespace TasinmazYönetimi.Infrastructure.PropertyMedia;

public sealed class PropertyMediaService : IPropertyMediaService
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditLogService _audit;
    private readonly string _directory;

    public PropertyMediaService(ApplicationDbContext db, IAuditLogService audit,
        IOptions<PropertyMediaOptions> options)
    {
        _db = db;
        _audit = audit;
        _directory = options.Value.RootDirectory;
    }

    public async Task<PropertyMediaResponse?> UploadAsync(Guid userId, Guid propertyId,
        string fileName, Stream content, long length, CancellationToken cancellationToken)
    {
        // Başka kullanıcının taşınmazına fotoğraf eklenmesini engeller.
        if (!await CanReadAsync(userId, UserRole.User, propertyId, cancellationToken)) return null;
        if (length <= 0 || length > PropertyMediaOptions.MaxFileSize)
            throw new ArgumentException("Fotoğraf boş olamaz ve 100 MB sınırını aşamaz.");

        string extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (extension is not (".jpg" or ".jpeg" or ".png"))
            throw new ArgumentException("Yalnızca JPEG veya PNG fotoğraf yüklenebilir.");

        // Uzantıya ek olarak dosyanın JPEG/PNG imzasını kontrol eder.
        byte[] header = new byte[8];
        int headerLength = await content.ReadAtLeastAsync(header, 8, false, cancellationToken);
        bool png = headerLength == 8 && header.SequenceEqual(
            new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        bool jpeg = headerLength >= 3 && header[0] == 255 && header[1] == 216 && header[2] == 255;
        if (extension == ".png" ? !png : !jpeg)
            throw new ArgumentException("Dosyanın içeriği JPEG/PNG biçimiyle eşleşmiyor.");

        var media = new MediaEntity
        {
            Id = Guid.NewGuid(), PropertyId = propertyId,
            FileName = Path.GetFileName(fileName.Replace('\\', '/')),
            ContentType = png ? "image/png" : "image/jpeg",
            FileSize = length, CreatedAt = DateTime.UtcNow
        };
        if (media.FileName.Length > 255)
            throw new ArgumentException("Dosya adı 255 karakteri geçemez.");
        // Kullanıcının verdiği adı fiziksel dosya yolu olarak kullanmaz.
        media.FilePath = media.Id.ToString("N") + extension;
        Directory.CreateDirectory(_directory);
        string path = Path.Combine(_directory, media.FilePath);
        try
        {
            media.FileSize = await WriteFileAsync(
                path, content, header.AsMemory(0, headerLength), cancellationToken);
            _db.PropertyMedia.Add(media);
            _audit.Add(userId, LogStatus.Successful, OperationType.PropertyUpdated,
                $"Taşınmaza fotoğraf eklendi: {propertyId}");
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Veritabanına kaydedilemeyen dosyayı klasörde bırakmaz.
            if (File.Exists(path)) File.Delete(path);
            throw;
        }
        return new(media.Id, media.FileName, media.ContentType, media.FileSize, media.CreatedAt);
    }

    private static async Task<long> WriteFileAsync(string path, Stream content,
        ReadOnlyMemory<byte> header, CancellationToken cancellationToken)
    {
        await using var target = new FileStream(path, FileMode.CreateNew,
            FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous);

        await target.WriteAsync(header, cancellationToken);
        byte[] buffer = new byte[81920];
        long written = header.Length;
        int read;
        while ((read = await content.ReadAsync(buffer, cancellationToken)) > 0)
        {
            written += read;
            if (written > PropertyMediaOptions.MaxFileSize)
                throw new ArgumentException("Fotoğraf 100 MB sınırını aşamaz.");
            await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        return written;
    }

    public async Task<List<PropertyMediaResponse>?> ListAsync(Guid userId, UserRole role,
        Guid propertyId, CancellationToken cancellationToken)
    {
        if (!await CanReadAsync(userId, role, propertyId, cancellationToken)) return null;
        return await _db.PropertyMedia.AsNoTracking()
            .Where(media => media.PropertyId == propertyId)
            .OrderByDescending(media => media.CreatedAt)
            .Select(media => new PropertyMediaResponse(media.Id, media.FileName,
                media.ContentType, media.FileSize, media.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<PropertyMediaFile?> OpenAsync(Guid userId, UserRole role,
        Guid propertyId, Guid mediaId, CancellationToken cancellationToken)
    {
        if (!await CanReadAsync(userId, role, propertyId, cancellationToken)) return null;
        var media = await _db.PropertyMedia.AsNoTracking().FirstOrDefaultAsync(
            item => item.Id == mediaId && item.PropertyId == propertyId, cancellationToken);
        if (media is null) return null;
        // Yalnız bu servis tarafından oluşturulan dosya adlarını açar.
        string extension = media.ContentType == "image/png" ? ".png" : ".jpg";
        string name = media.FilePath;
        if (name != media.Id.ToString("N") + extension &&
            !(media.ContentType == "image/jpeg" && name == media.Id.ToString("N") + ".jpeg"))
            return null;
        string path = Path.Combine(_directory, name);
        try
        {
            return new(new FileStream(path, FileMode.Open, FileAccess.Read,
                FileShare.Read, 81920, FileOptions.Asynchronous), media.ContentType);
        }
        catch (FileNotFoundException) { return null; }
        catch (DirectoryNotFoundException) { return null; }
    }

    // Admin tüm kayıtları, User yalnız kendi kayıtlarını görüntüler.
    private Task<bool> CanReadAsync(Guid userId, UserRole role, Guid propertyId,
        CancellationToken cancellationToken) => _db.Properties.AsNoTracking().AnyAsync(
            property => property.Id == propertyId &&
                (role == UserRole.Admin || property.UserId == userId), cancellationToken);
}

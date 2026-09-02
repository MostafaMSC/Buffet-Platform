using BuffetDiscovery.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace BuffetDiscovery.Infrastructure.Services;

public class LocalFileStorageService(IOptions<FileStorageOptions> options) : IFileStorageService
{
    public async Task<string> SaveAsync(Stream content, string originalFileName, CancellationToken ct)
    {
        var uploadsRoot = options.Value.UploadsRootPath;
        Directory.CreateDirectory(uploadsRoot);

        var ext = Path.GetExtension(originalFileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid():N}{ext}";
        var filePath = Path.Combine(uploadsRoot, fileName);

        await using (var fileStream = File.Create(filePath))
        {
            await content.CopyToAsync(fileStream, ct);
        }

        return $"/uploads/{fileName}";
    }
}

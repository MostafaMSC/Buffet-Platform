namespace BuffetDiscovery.Infrastructure.Services;

/// The absolute filesystem path uploaded files are written under. Supplied by the host
/// (Api project's Program.cs, from IWebHostEnvironment.WebRootPath) so Infrastructure
/// doesn't need to depend on ASP.NET Core hosting itself.
public class FileStorageOptions
{
    public string UploadsRootPath { get; set; } = string.Empty;
}

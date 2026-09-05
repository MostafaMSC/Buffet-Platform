namespace BuffetDiscovery.Application.Common.Interfaces;

public interface IFileStorageService
{
    /// Saves the given content under a generated file name preserving the extension of
    /// originalFileName, and returns a URL path (e.g. "/uploads/xxxx.jpg") clients can use.
    Task<string> SaveAsync(Stream content, string originalFileName, CancellationToken ct);
}

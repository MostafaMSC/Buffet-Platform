using BuffetDiscovery.Application.Common.Dtos;
using BuffetDiscovery.Application.Common.Interfaces;
using FluentValidation;
using MediatR;

namespace BuffetDiscovery.Application.Features.Uploads;

public record UploadFileCommand(Stream Content, string FileName, long Length) : IRequest<UploadResultDto>;

public class UploadFileCommandValidator : AbstractValidator<UploadFileCommand>
{
    private static readonly HashSet<string> ImageExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private static readonly HashSet<string> VideoExtensions = [".mp4", ".webm", ".mov"];
    public const long MaxImageSizeBytes = 8 * 1024 * 1024;
    public const long MaxVideoSizeBytes = 50 * 1024 * 1024;

    public UploadFileCommandValidator()
    {
        RuleFor(x => x.Length).GreaterThan(0).WithMessage("Empty file.");

        RuleFor(x => x.FileName)
            .Must(name => ImageExtensions.Contains(Extension(name)) || VideoExtensions.Contains(Extension(name)))
            .WithMessage("Unsupported file type. Use JPG, PNG or WEBP for photos, or MP4, WEBM or MOV for video.");

        RuleFor(x => x)
            .Must(x => x.Length <= (VideoExtensions.Contains(Extension(x.FileName)) ? MaxVideoSizeBytes : MaxImageSizeBytes))
            .WithMessage(x => VideoExtensions.Contains(Extension(x.FileName))
                ? "Video too large (max 50MB)."
                : "File too large (max 8MB).")
            .WithName("File");
    }

    private static string Extension(string fileName) => Path.GetExtension(fileName).ToLowerInvariant();
}

public class UploadFileCommandHandler(IFileStorageService fileStorage) : IRequestHandler<UploadFileCommand, UploadResultDto>
{
    public async Task<UploadResultDto> Handle(UploadFileCommand request, CancellationToken ct)
    {
        var url = await fileStorage.SaveAsync(request.Content, request.FileName, ct);
        return new UploadResultDto(url);
    }
}

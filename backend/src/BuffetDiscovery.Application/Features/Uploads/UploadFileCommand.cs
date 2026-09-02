using BuffetDiscovery.Application.Common.Dtos;
using BuffetDiscovery.Application.Common.Interfaces;
using FluentValidation;
using MediatR;

namespace BuffetDiscovery.Application.Features.Uploads;

public record UploadFileCommand(Stream Content, string FileName, long Length) : IRequest<UploadResultDto>;

public class UploadFileCommandValidator : AbstractValidator<UploadFileCommand>
{
    private static readonly HashSet<string> AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private const long MaxFileSizeBytes = 8 * 1024 * 1024;

    public UploadFileCommandValidator()
    {
        RuleFor(x => x.Length).GreaterThan(0).WithMessage("Empty file.");
        RuleFor(x => x.Length).LessThanOrEqualTo(MaxFileSizeBytes).WithMessage("File too large (max 8MB).");
        RuleFor(x => x.FileName)
            .Must(name => AllowedExtensions.Contains(Path.GetExtension(name).ToLowerInvariant()))
            .WithMessage("Unsupported file type. Use JPG, PNG or WEBP.");
    }
}

public class UploadFileCommandHandler(IFileStorageService fileStorage) : IRequestHandler<UploadFileCommand, UploadResultDto>
{
    public async Task<UploadResultDto> Handle(UploadFileCommand request, CancellationToken ct)
    {
        var url = await fileStorage.SaveAsync(request.Content, request.FileName, ct);
        return new UploadResultDto(url);
    }
}

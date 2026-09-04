namespace BuffetDiscovery.Application.Common.Exceptions;

/// See <see cref="ConflictException"/> for what <paramref name="code"/> and
/// <paramref name="errorParams"/> are for.
public class NotFoundException(string message, string? code = null, object? errorParams = null) : Exception(message)
{
    public string? Code { get; } = code;
    public object? Params { get; } = errorParams;
}

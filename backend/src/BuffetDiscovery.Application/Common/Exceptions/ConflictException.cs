namespace BuffetDiscovery.Application.Common.Exceptions;

/// <param name="message">English fallback — shown in logs, Swagger, and to any caller that
/// doesn't recognize <paramref name="code"/>.</param>
/// <param name="code">Stable identifier a client can map to its own localized copy, e.g.
/// "booking_min_guests". Omitted only for exceptions no customer-facing surface throws.</param>
/// <param name="errorParams">Values to interpolate into that localized copy, e.g. { min = 4 }.</param>
public class ConflictException(string message, string? code = null, object? errorParams = null) : Exception(message)
{
    public string? Code { get; } = code;
    public object? Params { get; } = errorParams;
}

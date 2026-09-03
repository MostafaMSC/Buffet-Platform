namespace BuffetDiscovery.Domain.Entities;

/// Top of the location hierarchy: Country → City → Area. The platform launched in Baghdad
/// but the model is deliberately not shaped around a single city or country.
public class Country
{
    public int Id { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;

    /// ISO 3166-1 alpha-2, e.g. "IQ".
    public string Code { get; set; } = string.Empty;

    public string CurrencyCode { get; set; } = "IQD";
    public int SortOrder { get; set; }

    public List<City> Cities { get; set; } = [];
}

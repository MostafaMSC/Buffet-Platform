namespace BuffetDiscovery.Domain.Entities;

/// A course or station within a service's menu — "Appetizers", "Live Stations",
/// "Desserts" for a buffet; "Starter", "Main", "Dessert" for a set menu.
public class MenuSection
{
    public int Id { get; set; }

    public int ServiceId { get; set; }
    public Service? Service { get; set; }

    public string Name { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public List<MenuItem> Items { get; set; } = [];
}

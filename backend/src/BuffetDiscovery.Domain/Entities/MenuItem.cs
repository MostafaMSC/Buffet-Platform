namespace BuffetDiscovery.Domain.Entities;

public class MenuItem
{
    public int Id { get; set; }

    public int MenuSectionId { get; set; }
    public MenuSection? MenuSection { get; set; }

    public string Name { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;

    public string? Description { get; set; }
    public string? DescriptionAr { get; set; }

    public DietaryTags Dietary { get; set; } = DietaryTags.None;
    public int SortOrder { get; set; }
}

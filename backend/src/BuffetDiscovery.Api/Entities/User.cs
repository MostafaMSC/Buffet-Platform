namespace BuffetDiscovery.Api.Entities;

public class User
{
    public int Id { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.RestaurantOwner;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int? RestaurantId { get; set; }
    public Restaurant? Restaurant { get; set; }
}

using BuffetDiscovery.Domain.Entities;

namespace BuffetDiscovery.Application.Common.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken ct);
    Task<bool> PhoneNumberExistsAsync(string phoneNumber, CancellationToken ct);
    void Add(User user);
}

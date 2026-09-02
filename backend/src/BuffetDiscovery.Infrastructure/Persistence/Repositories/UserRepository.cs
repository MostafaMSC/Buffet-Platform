using BuffetDiscovery.Application.Common.Interfaces;
using BuffetDiscovery.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuffetDiscovery.Infrastructure.Persistence.Repositories;

public class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken ct) =>
        db.Users.Include(u => u.Restaurant).FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber, ct);

    public Task<bool> PhoneNumberExistsAsync(string phoneNumber, CancellationToken ct) =>
        db.Users.AnyAsync(u => u.PhoneNumber == phoneNumber, ct);

    public void Add(User user) => db.Users.Add(user);
}

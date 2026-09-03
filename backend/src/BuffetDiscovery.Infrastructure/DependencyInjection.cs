using BuffetDiscovery.Application.Common.Interfaces;
using BuffetDiscovery.Infrastructure.Persistence;
using BuffetDiscovery.Infrastructure.Persistence.Repositories;
using BuffetDiscovery.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BuffetDiscovery.Infrastructure;

public static class DependencyInjection
{
    /// uploadsRootPath: absolute filesystem path for uploaded files, e.g. the Api project's
    /// IWebHostEnvironment.WebRootPath + "/uploads".
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, string uploadsRootPath)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAreaRepository, AreaRepository>();
        services.AddScoped<IRestaurantRepository, RestaurantRepository>();
        services.AddScoped<IOfferingRepository, OfferingRepository>();
        services.AddScoped<IAvailabilityRepository, AvailabilityRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITimeSlotRepository, TimeSlotRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IWaitlistRepository, WaitlistRepository>();
        services.AddScoped<IRestaurantSettingsRepository, RestaurantSettingsRepository>();

        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<INotificationService, NotificationService>();

        services.Configure<FileStorageOptions>(o => o.UploadsRootPath = uploadsRootPath);
        services.AddScoped<IFileStorageService, LocalFileStorageService>();

        return services;
    }
}

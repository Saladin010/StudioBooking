using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StudioBooking.Infrastructure.Persistence;

namespace StudioBooking.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<StudioBookingDbContext>((serviceProvider, options) =>
        {
            var config = serviceProvider.GetRequiredService<IConfiguration>();
            var connectionString = config.GetConnectionString("Default")
                ?? throw new InvalidOperationException("Connection string 'Default' was not found.");

            options.UseNpgsql(connectionString);
        });

        return services;
    }
}

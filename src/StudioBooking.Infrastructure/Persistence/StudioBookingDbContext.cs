using Microsoft.EntityFrameworkCore;

namespace StudioBooking.Infrastructure.Persistence;

public class StudioBookingDbContext : DbContext
{
    public StudioBookingDbContext(DbContextOptions<StudioBookingDbContext> options)
        : base(options)
    {
    }
}

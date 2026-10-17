using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using StudioBooking.Domain.Entities;
using StudioBooking.Infrastructure.Persistence;

namespace StudioBooking.Api.IntegrationTests;

public class BookingConstraintTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public BookingConstraintTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static async Task<(Room Room, User User)> SeedRoomAndUserAsync(ApplicationDbContext dbContext)
    {
        var room = new Room(Guid.NewGuid(), $"Room_{Guid.NewGuid():N}", 4, 100.00m);
        var user = new User(Guid.NewGuid(), $"customer_{Guid.NewGuid():N}@example.com", "Customer");

        dbContext.Rooms.Add(room);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        return (room, user);
    }

    [Fact]
    public async Task SaveChanges_TwoOverlappingActiveBookingsForSameRoom_ThrowsDbUpdateExceptionWith23P01()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var (room, user) = await SeedRoomAndUserAsync(dbContext);

        var start1 = new DateTimeOffset(2026, 11, 1, 10, 0, 0, TimeSpan.Zero);
        var end1 = new DateTimeOffset(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);

        var start2 = new DateTimeOffset(2026, 11, 1, 11, 0, 0, TimeSpan.Zero);
        var end2 = new DateTimeOffset(2026, 11, 1, 13, 0, 0, TimeSpan.Zero);

        var booking1 = new Booking(Guid.NewGuid(), room.Id, user.Id, start1, end1, "Active");
        dbContext.Bookings.Add(booking1);
        await dbContext.SaveChangesAsync();

        var booking2 = new Booking(Guid.NewGuid(), room.Id, user.Id, start2, end2, "Active");
        dbContext.Bookings.Add(booking2);

        // Act
        var act = async () => await dbContext.SaveChangesAsync();

        // Assert
        var exception = await act.Should().ThrowAsync<DbUpdateException>();
        var postgresException = exception.Which.InnerException as PostgresException;
        postgresException.Should().NotBeNull();
        postgresException!.SqlState.Should().Be("23P01");
    }

    [Fact]
    public async Task SaveChanges_TwoBackToBackBookingsForSameRoom_BothSucceed()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var (room, user) = await SeedRoomAndUserAsync(dbContext);

        var start1 = new DateTimeOffset(2026, 11, 2, 12, 0, 0, TimeSpan.Zero);
        var end1 = new DateTimeOffset(2026, 11, 2, 14, 0, 0, TimeSpan.Zero);

        var start2 = new DateTimeOffset(2026, 11, 2, 14, 0, 0, TimeSpan.Zero);
        var end2 = new DateTimeOffset(2026, 11, 2, 16, 0, 0, TimeSpan.Zero);

        var booking1 = new Booking(Guid.NewGuid(), room.Id, user.Id, start1, end1, "Active");
        var booking2 = new Booking(Guid.NewGuid(), room.Id, user.Id, start2, end2, "Active");

        dbContext.Bookings.Add(booking1);
        await dbContext.SaveChangesAsync();

        dbContext.Bookings.Add(booking2);

        // Act & Assert
        var act = async () => await dbContext.SaveChangesAsync();
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SaveChanges_OneCancelledAndOneActiveBookingInSameSlot_BothSucceed()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var (room, user) = await SeedRoomAndUserAsync(dbContext);

        var start = new DateTimeOffset(2026, 11, 3, 10, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2026, 11, 3, 12, 0, 0, TimeSpan.Zero);

        var bookingCancelled = new Booking(Guid.NewGuid(), room.Id, user.Id, start, end, "Cancelled");
        dbContext.Bookings.Add(bookingCancelled);
        await dbContext.SaveChangesAsync();

        var bookingActive = new Booking(Guid.NewGuid(), room.Id, user.Id, start, end, "Active");
        dbContext.Bookings.Add(bookingActive);

        // Act & Assert
        var act = async () => await dbContext.SaveChangesAsync();
        await act.Should().NotThrowAsync();
    }
}

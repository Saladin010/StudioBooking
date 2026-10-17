using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudioBooking.Api.Controllers;
using StudioBooking.Domain.Entities;
using StudioBooking.Infrastructure.Persistence;

namespace StudioBooking.Api.IntegrationTests;

public class BookingsEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public BookingsEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<(Room Room, User Customer, HttpClient Client)> SetupCustomerAndRoomAsync()
    {
        var room = new Room(Guid.NewGuid(), $"Room_{Guid.NewGuid():N}", 4, 100.00m);
        var customerEmail = $"customer_{Guid.NewGuid():N}@example.com";
        var customer = new User(Guid.NewGuid(), customerEmail, "Customer");

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.Rooms.Add(room);
            dbContext.Users.Add(customer);
            await dbContext.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        var tokenResponse = await client.PostAsJsonAsync("/api/auth/token", new TokenRequest(customerEmail));
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenResult = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>();
        tokenResult.Should().NotBeNull();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult!.Token);

        return (room, customer, client);
    }

    [Fact]
    public async Task PostBooking_WithValidDataAndCustomerToken_Returns201Created()
    {
        // Arrange
        var (room, customer, client) = await SetupCustomerAndRoomAsync();

        var startTime = new DateTimeOffset(2026, 12, 1, 14, 0, 0, TimeSpan.Zero);
        var endTime = new DateTimeOffset(2026, 12, 1, 16, 0, 0, TimeSpan.Zero);
        var request = new CreateBookingRequest(room.Id, startTime, endTime);

        // Act
        var response = await client.PostAsJsonAsync("/api/bookings", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdBooking = await response.Content.ReadFromJsonAsync<BookingResponse>();
        createdBooking.Should().NotBeNull();
        createdBooking!.Id.Should().NotBeEmpty();
        createdBooking.RoomId.Should().Be(room.Id);
        createdBooking.UserId.Should().Be(customer.Id);
        createdBooking.StartTime.Should().Be(startTime);
        createdBooking.EndTime.Should().Be(endTime);
        createdBooking.Status.Should().Be("Active");

        // Verify direct persistence in DB via DbContext
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var savedBooking = await dbContext.Bookings.FirstOrDefaultAsync(b => b.Id == createdBooking.Id);
            savedBooking.Should().NotBeNull();
            savedBooking!.RoomId.Should().Be(room.Id);
            savedBooking.UserId.Should().Be(customer.Id);
            savedBooking.StartTime.Should().Be(startTime);
            savedBooking.EndTime.Should().Be(endTime);
            savedBooking.Status.Should().Be("Active");
        }
    }

    [Fact]
    public async Task PostBooking_WhenOverlappingWithExistingActiveBooking_Returns409Conflict()
    {
        // Arrange
        var (room, customer, client) = await SetupCustomerAndRoomAsync();

        var existingStart = new DateTimeOffset(2026, 12, 2, 10, 0, 0, TimeSpan.Zero);
        var existingEnd = new DateTimeOffset(2026, 12, 2, 12, 0, 0, TimeSpan.Zero);

        // Insert existing active booking directly via DbContext
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var existingBooking = new Booking(Guid.NewGuid(), room.Id, customer.Id, existingStart, existingEnd, "Active");
            dbContext.Bookings.Add(existingBooking);
            await dbContext.SaveChangesAsync();
        }

        // Overlapping request (11:00 to 13:00 overlaps with 10:00 to 12:00)
        var requestStart = new DateTimeOffset(2026, 12, 2, 11, 0, 0, TimeSpan.Zero);
        var requestEnd = new DateTimeOffset(2026, 12, 2, 13, 0, 0, TimeSpan.Zero);
        var request = new CreateBookingRequest(room.Id, requestStart, requestEnd);

        // Act
        var response = await client.PostAsJsonAsync("/api/bookings", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CancelBooking_WhenOwnedByCustomer_Returns204NoContentAndUpdatesStatus()
    {
        // Arrange
        var (room, customer, client) = await SetupCustomerAndRoomAsync();

        var startTime = new DateTimeOffset(2026, 12, 3, 10, 0, 0, TimeSpan.Zero);
        var endTime = new DateTimeOffset(2026, 12, 3, 12, 0, 0, TimeSpan.Zero);
        var booking = new Booking(Guid.NewGuid(), room.Id, customer.Id, startTime, endTime, "Active");

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.Bookings.Add(booking);
            await dbContext.SaveChangesAsync();
        }

        // Act
        var response = await client.PatchAsync($"/api/bookings/{booking.Id}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var updatedBooking = await dbContext.Bookings.FirstOrDefaultAsync(b => b.Id == booking.Id);
            updatedBooking.Should().NotBeNull();
            updatedBooking!.Status.Should().Be("Cancelled");
        }
    }

    [Fact]
    public async Task CancelBooking_WhenOwnedByAnotherCustomer_Returns403ForbiddenAndDoesNotCancel()
    {
        // Arrange: Create owner customer and booking
        var (room, ownerCustomer, _) = await SetupCustomerAndRoomAsync();

        var startTime = new DateTimeOffset(2026, 12, 4, 10, 0, 0, TimeSpan.Zero);
        var endTime = new DateTimeOffset(2026, 12, 4, 12, 0, 0, TimeSpan.Zero);
        var booking = new Booking(Guid.NewGuid(), room.Id, ownerCustomer.Id, startTime, endTime, "Active");

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.Bookings.Add(booking);
            await dbContext.SaveChangesAsync();
        }

        // Create second customer (attacker)
        var otherCustomerEmail = $"other_customer_{Guid.NewGuid():N}@example.com";
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.Users.Add(new User(Guid.NewGuid(), otherCustomerEmail, "Customer"));
            await dbContext.SaveChangesAsync();
        }

        var otherClient = _factory.CreateClient();
        var tokenResponse = await otherClient.PostAsJsonAsync("/api/auth/token", new TokenRequest(otherCustomerEmail));
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenResult = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>();
        otherClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult!.Token);

        // Act
        var response = await otherClient.PatchAsync($"/api/bookings/{booking.Id}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var unchangedBooking = await dbContext.Bookings.FirstOrDefaultAsync(b => b.Id == booking.Id);
            unchangedBooking.Should().NotBeNull();
            unchangedBooking!.Status.Should().Be("Active");
        }
    }

    [Fact]
    public async Task CancelBooking_WhenBookingDoesNotExist_Returns404NotFound()
    {
        // Arrange
        var (_, _, client) = await SetupCustomerAndRoomAsync();
        var nonExistentBookingId = Guid.NewGuid();

        // Act
        var response = await client.PatchAsync($"/api/bookings/{nonExistentBookingId}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetMyBookings_ReturnsOnlyCurrentCustomerBookings_AndDoesNotLeakOtherCustomersBookings()
    {
        // Arrange
        var (room1, customer1, client1) = await SetupCustomerAndRoomAsync();
        var (room2, customer2, _) = await SetupCustomerAndRoomAsync();

        var booking1Customer1 = new Booking(
            Guid.NewGuid(),
            room1.Id,
            customer1.Id,
            new DateTimeOffset(2026, 12, 10, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 12, 10, 12, 0, 0, TimeSpan.Zero),
            "Active"
        );

        var booking2Customer1 = new Booking(
            Guid.NewGuid(),
            room1.Id,
            customer1.Id,
            new DateTimeOffset(2026, 12, 11, 14, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 12, 11, 16, 0, 0, TimeSpan.Zero),
            "Cancelled"
        );

        var bookingCustomer2 = new Booking(
            Guid.NewGuid(),
            room2.Id,
            customer2.Id,
            new DateTimeOffset(2026, 12, 12, 9, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 12, 12, 11, 0, 0, TimeSpan.Zero),
            "Active"
        );

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.Bookings.AddRange(booking1Customer1, booking2Customer1, bookingCustomer2);
            await dbContext.SaveChangesAsync();
        }

        // Act
        var response = await client1.GetAsync("/api/bookings/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<BookingResponse>>();
        result.Should().NotBeNull();
        result!.Should().HaveCount(2);

        // Verify only Customer 1's bookings are returned
        result.Select(b => b.Id).Should().Contain(new[] { booking1Customer1.Id, booking2Customer1.Id });
        result.Select(b => b.Id).Should().NotContain(bookingCustomer2.Id);

        // Verify all returned bookings belong to Customer 1
        result.Should().OnlyContain(b => b.UserId == customer1.Id);

        // Verify room details and statuses are mapped correctly
        var activeItem = result.Single(b => b.Id == booking1Customer1.Id);
        activeItem.RoomId.Should().Be(room1.Id);
        activeItem.RoomName.Should().Be(room1.Name);
        activeItem.StartTime.Should().Be(booking1Customer1.StartTime);
        activeItem.EndTime.Should().Be(booking1Customer1.EndTime);
        activeItem.Status.Should().Be("Active");

        var cancelledItem = result.Single(b => b.Id == booking2Customer1.Id);
        cancelledItem.RoomId.Should().Be(room1.Id);
        cancelledItem.RoomName.Should().Be(room1.Name);
        cancelledItem.StartTime.Should().Be(booking2Customer1.StartTime);
        cancelledItem.EndTime.Should().Be(booking2Customer1.EndTime);
        cancelledItem.Status.Should().Be("Cancelled");
    }

    [Fact]
    public async Task GetMyBookings_WithoutToken_Returns401Unauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/bookings/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMyBookings_WithAdminRole_Returns403Forbidden()
    {
        // Arrange
        var adminEmail = $"admin_{Guid.NewGuid():N}@example.com";
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.Users.Add(new User(Guid.NewGuid(), adminEmail, "Admin"));
            await dbContext.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        var tokenResponse = await client.PostAsJsonAsync("/api/auth/token", new TokenRequest(adminEmail));
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenResult = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult!.Token);

        // Act
        var response = await client.GetAsync("/api/bookings/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}

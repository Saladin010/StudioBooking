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

public class DisputesEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public DisputesEndpointsTests(CustomWebApplicationFactory factory)
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
    public async Task PostDispute_WhenCustomerDisputesOwnBooking_Returns201CreatedAndPersistsDispute()
    {
        // Arrange
        var (room, customer, client) = await SetupCustomerAndRoomAsync();

        var booking = new Booking(
            Guid.NewGuid(),
            room.Id,
            customer.Id,
            new DateTimeOffset(2026, 12, 20, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 12, 20, 12, 0, 0, TimeSpan.Zero),
            "Active"
        );

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.Bookings.Add(booking);
            await dbContext.SaveChangesAsync();
        }

        var request = new CreateDisputeRequest(booking.Id, "The studio equipment was damaged during the session.");

        // Act
        var response = await client.PostAsJsonAsync("/api/disputes", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdDispute = await response.Content.ReadFromJsonAsync<DisputeResponse>();
        createdDispute.Should().NotBeNull();
        createdDispute!.Id.Should().NotBeEmpty();
        createdDispute.BookingId.Should().Be(booking.Id);
        createdDispute.CustomerId.Should().Be(customer.Id);
        createdDispute.Reason.Should().Be("The studio equipment was damaged during the session.");
        createdDispute.Status.Should().Be("Pending");

        // Verify direct database persistence
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var savedDispute = await dbContext.Disputes.FirstOrDefaultAsync(d => d.Id == createdDispute.Id);
            savedDispute.Should().NotBeNull();
            savedDispute!.BookingId.Should().Be(booking.Id);
            savedDispute.CustomerId.Should().Be(customer.Id);
            savedDispute.Reason.Should().Be("The studio equipment was damaged during the session.");
            savedDispute.Status.Should().Be("Pending");
        }
    }

    [Fact]
    public async Task PostDispute_WhenCustomerDisputesAnotherCustomerBooking_Returns403Forbidden()
    {
        // Arrange: Owner customer and booking
        var (room, ownerCustomer, _) = await SetupCustomerAndRoomAsync();

        var booking = new Booking(
            Guid.NewGuid(),
            room.Id,
            ownerCustomer.Id,
            new DateTimeOffset(2026, 12, 21, 14, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 12, 21, 16, 0, 0, TimeSpan.Zero),
            "Active"
        );

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.Bookings.Add(booking);
            await dbContext.SaveChangesAsync();
        }

        // Another customer
        var (_, _, otherClient) = await SetupCustomerAndRoomAsync();

        var request = new CreateDisputeRequest(booking.Id, "Unauthorized dispute attempt.");

        // Act
        var response = await otherClient.PostAsJsonAsync("/api/disputes", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Verify dispute was NOT created in DB
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var disputeInDb = await dbContext.Disputes.FirstOrDefaultAsync(d => d.BookingId == booking.Id);
            disputeInDb.Should().BeNull();
        }
    }

    [Fact]
    public async Task PostDispute_WhenBookingDoesNotExist_Returns404NotFound()
    {
        // Arrange
        var (_, _, client) = await SetupCustomerAndRoomAsync();
        var nonExistentBookingId = Guid.NewGuid();
        var request = new CreateDisputeRequest(nonExistentBookingId, "Non-existent booking dispute.");

        // Act
        var response = await client.PostAsJsonAsync("/api/disputes", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostDispute_WithoutToken_Returns401Unauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new CreateDisputeRequest(Guid.NewGuid(), "Unauthorized attempt.");

        // Act
        var response = await client.PostAsJsonAsync("/api/disputes", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostDispute_WithAdminRole_Returns403Forbidden()
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

        var request = new CreateDisputeRequest(Guid.NewGuid(), "Admin cannot open customer disputes.");

        // Act
        var response = await client.PostAsJsonAsync("/api/disputes", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<HttpClient> SetupAdminClientAsync()
    {
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
        tokenResult.Should().NotBeNull();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult!.Token);
        return client;
    }

    [Fact]
    public async Task PatchDispute_WithAdminToken_UpdatesDisputeStatusAndReturns200Ok()
    {
        // Arrange
        var (room, customer, _) = await SetupCustomerAndRoomAsync();
        var adminClient = await SetupAdminClientAsync();

        var booking = new Booking(
            Guid.NewGuid(),
            room.Id,
            customer.Id,
            new DateTimeOffset(2026, 12, 22, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 12, 22, 12, 0, 0, TimeSpan.Zero),
            "Active"
        );

        var dispute = new Dispute(
            Guid.NewGuid(),
            booking.Id,
            customer.Id,
            "Broken microphone",
            "Pending"
        );

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.Bookings.Add(booking);
            dbContext.Disputes.Add(dispute);
            await dbContext.SaveChangesAsync();
        }

        var request = new ResolveDisputeRequest("Resolved", "Investigated and full refund processed.");

        // Act
        var response = await adminClient.PatchAsJsonAsync($"/api/admin/disputes/{dispute.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DisputeResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(dispute.Id);
        result.Status.Should().Be("Resolved");
        result.ResolutionNotes.Should().Be("Investigated and full refund processed.");

        // Verify in database
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var updatedDispute = await dbContext.Disputes.FirstOrDefaultAsync(d => d.Id == dispute.Id);
            updatedDispute.Should().NotBeNull();
            updatedDispute!.Status.Should().Be("Resolved");
            updatedDispute.ResolutionNotes.Should().Be("Investigated and full refund processed.");
        }
    }

    [Fact]
    public async Task PatchDispute_WithCustomerToken_Returns403Forbidden()
    {
        // Arrange
        var (room, customer, customerClient) = await SetupCustomerAndRoomAsync();

        var booking = new Booking(
            Guid.NewGuid(),
            room.Id,
            customer.Id,
            new DateTimeOffset(2026, 12, 23, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 12, 23, 12, 0, 0, TimeSpan.Zero),
            "Active"
        );

        var dispute = new Dispute(
            Guid.NewGuid(),
            booking.Id,
            customer.Id,
            "Customer trying to self-resolve",
            "Pending"
        );

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.Bookings.Add(booking);
            dbContext.Disputes.Add(dispute);
            await dbContext.SaveChangesAsync();
        }

        var request = new ResolveDisputeRequest("Resolved", "Self resolution attempt.");

        // Act
        var response = await customerClient.PatchAsJsonAsync($"/api/admin/disputes/{dispute.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Verify in database: status remains Pending
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var unchangedDispute = await dbContext.Disputes.FirstOrDefaultAsync(d => d.Id == dispute.Id);
            unchangedDispute.Should().NotBeNull();
            unchangedDispute!.Status.Should().Be("Pending");
        }
    }

    [Fact]
    public async Task PatchDispute_WhenDisputeDoesNotExist_Returns404NotFound()
    {
        // Arrange
        var adminClient = await SetupAdminClientAsync();
        var nonExistentId = Guid.NewGuid();
        var request = new ResolveDisputeRequest("Resolved", "No dispute exists.");

        // Act
        var response = await adminClient.PatchAsJsonAsync($"/api/admin/disputes/{nonExistentId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

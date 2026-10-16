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

public class RoomsEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public RoomsEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetRooms_WhenNoRoomsExist_Returns200OkWithEmptyList()
    {
        // Arrange: Ensure Rooms table is empty
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.Rooms.RemoveRange(dbContext.Rooms);
            await dbContext.SaveChangesAsync();
        }

        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/rooms");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var rooms = await response.Content.ReadFromJsonAsync<List<RoomResponse>>();
        rooms.Should().NotBeNull();
        rooms.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRooms_WhenRoomsExist_Returns200OkWithAllRooms()
    {
        // Arrange: Insert two rooms into the database via DbContext
        var room1 = new Room(Guid.NewGuid(), "Studio A - Control Room", 6, 120.00m);
        var room2 = new Room(Guid.NewGuid(), "Studio B - Live Room", 12, 200.50m);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.Rooms.RemoveRange(dbContext.Rooms);
            await dbContext.SaveChangesAsync();

            dbContext.Rooms.AddRange(room1, room2);
            await dbContext.SaveChangesAsync();
        }

        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/rooms");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var rooms = await response.Content.ReadFromJsonAsync<List<RoomResponse>>();
        rooms.Should().NotBeNull();
        rooms.Should().HaveCount(2);

        var retrievedRoom1 = rooms!.FirstOrDefault(r => r.Id == room1.Id);
        retrievedRoom1.Should().NotBeNull();
        retrievedRoom1!.Name.Should().Be(room1.Name);
        retrievedRoom1.Capacity.Should().Be(room1.Capacity);
        retrievedRoom1.HourlyRate.Should().Be(room1.HourlyRate);

        var retrievedRoom2 = rooms!.FirstOrDefault(r => r.Id == room2.Id);
        retrievedRoom2.Should().NotBeNull();
        retrievedRoom2!.Name.Should().Be(room2.Name);
        retrievedRoom2.Capacity.Should().Be(room2.Capacity);
        retrievedRoom2.HourlyRate.Should().Be(room2.HourlyRate);
    }

    [Fact]
    public async Task PostRoom_WithoutToken_Returns401Unauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new CreateRoomRequest("Vocal Booth", 2, 80.00m);

        // Act
        var response = await client.PostAsJsonAsync("/api/rooms", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostRoom_WithCustomerToken_Returns403Forbidden()
    {
        // Arrange: Insert Customer into database
        var customerEmail = $"room_customer_{Guid.NewGuid():N}@example.com";
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.Users.Add(new User(Guid.NewGuid(), customerEmail, "Customer"));
            await dbContext.SaveChangesAsync();
        }

        var client = _factory.CreateClient();

        // Request token for Customer
        var tokenResponse = await client.PostAsJsonAsync("/api/auth/token", new TokenRequest(customerEmail));
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenResult = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>();
        tokenResult.Should().NotBeNull();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult!.Token);
        var request = new CreateRoomRequest("Vocal Booth", 2, 80.00m);

        // Act
        var response = await client.PostAsJsonAsync("/api/rooms", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PostRoom_WithAdminTokenAndValidData_Returns201CreatedAndPersistsToDatabase()
    {
        // Arrange: Insert Admin into database
        var adminEmail = $"room_admin_{Guid.NewGuid():N}@example.com";
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.Users.Add(new User(Guid.NewGuid(), adminEmail, "Admin"));
            await dbContext.SaveChangesAsync();
        }

        var client = _factory.CreateClient();

        // Request token for Admin
        var tokenResponse = await client.PostAsJsonAsync("/api/auth/token", new TokenRequest(adminEmail));
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokenResult = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>();
        tokenResult.Should().NotBeNull();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult!.Token);
        var request = new CreateRoomRequest("Mastering Suite", 4, 180.00m);

        // Act
        var response = await client.PostAsJsonAsync("/api/rooms", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdRoom = await response.Content.ReadFromJsonAsync<RoomResponse>();
        createdRoom.Should().NotBeNull();
        createdRoom!.Id.Should().NotBeEmpty();
        createdRoom.Name.Should().Be(request.Name);
        createdRoom.Capacity.Should().Be(request.Capacity);
        createdRoom.HourlyRate.Should().Be(request.HourlyRate);

        // Verify direct persistence in DB via DbContext
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var savedRoom = await dbContext.Rooms.FirstOrDefaultAsync(r => r.Id == createdRoom.Id);
            savedRoom.Should().NotBeNull();
            savedRoom!.Name.Should().Be(request.Name);
            savedRoom.Capacity.Should().Be(request.Capacity);
            savedRoom.HourlyRate.Should().Be(request.HourlyRate);
        }
    }
}

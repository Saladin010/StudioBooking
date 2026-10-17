using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using StudioBooking.Api.Controllers;
using StudioBooking.Domain.Entities;
using StudioBooking.Infrastructure.Persistence;

namespace StudioBooking.Api.IntegrationTests;

public class ReviewsEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ReviewsEndpointsTests(CustomWebApplicationFactory factory)
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

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public async Task PostReview_WithValidRatingAndExistingRoom_Returns201CreatedAndPersistsReview(int rating)
    {
        // Arrange
        var (room, customer, client) = await SetupCustomerAndRoomAsync();
        var comment = $"Review with rating {rating}";
        var request = new CreateReviewRequest(room.Id, rating, comment);

        // Act
        var response = await client.PostAsJsonAsync("/api/reviews", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdReview = await response.Content.ReadFromJsonAsync<ReviewResponse>();
        createdReview.Should().NotBeNull();
        createdReview!.Id.Should().NotBeEmpty();
        createdReview.RoomId.Should().Be(room.Id);
        createdReview.UserId.Should().Be(customer.Id);
        createdReview.Rating.Should().Be(rating);
        createdReview.Comment.Should().Be(comment);
        createdReview.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));

        // Verify direct database persistence
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var savedReview = await dbContext.Reviews.FirstOrDefaultAsync(r => r.Id == createdReview.Id);
            savedReview.Should().NotBeNull();
            savedReview!.RoomId.Should().Be(room.Id);
            savedReview.UserId.Should().Be(customer.Id);
            savedReview.Rating.Should().Be(rating);
            savedReview.Comment.Should().Be(comment);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(6)]
    [InlineData(10)]
    public async Task PostReview_WithRatingOutOfRange_Returns400BadRequest(int invalidRating)
    {
        // Arrange
        var (room, _, client) = await SetupCustomerAndRoomAsync();
        var request = new CreateReviewRequest(room.Id, invalidRating, "Invalid rating test");

        // Act
        var response = await client.PostAsJsonAsync("/api/reviews", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostReview_WhenRoomDoesNotExist_Returns404NotFound()
    {
        // Arrange
        var (_, _, client) = await SetupCustomerAndRoomAsync();
        var nonExistentRoomId = Guid.NewGuid();
        var request = new CreateReviewRequest(nonExistentRoomId, 4, "Room does not exist");

        // Act
        var response = await client.PostAsJsonAsync("/api/reviews", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostReview_WithoutToken_Returns401Unauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new CreateReviewRequest(Guid.NewGuid(), 5, "Anonymous review");

        // Act
        var response = await client.PostAsJsonAsync("/api/reviews", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostReview_WithAdminRole_Returns403Forbidden()
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

        var request = new CreateReviewRequest(Guid.NewGuid(), 5, "Admin cannot submit reviews");

        // Act
        var response = await client.PostAsJsonAsync("/api/reviews", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DatabaseCheckConstraint_DirectInsertWithInvalidRating_ThrowsDbUpdateException()
    {
        // Arrange
        var (room, customer, _) = await SetupCustomerAndRoomAsync();
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var invalidReview = new Review(
            Guid.NewGuid(),
            room.Id,
            customer.Id,
            6, // Violates CK_Reviews_Rating_Range
            "Direct invalid insert",
            DateTimeOffset.UtcNow
        );

        dbContext.Reviews.Add(invalidReview);

        // Act
        var act = async () => await dbContext.SaveChangesAsync();

        // Assert
        var exception = await act.Should().ThrowAsync<DbUpdateException>();
        var postgresException = exception.Which.InnerException as PostgresException;
        postgresException.Should().NotBeNull();
        postgresException!.SqlState.Should().Be("23514"); // check_violation
    }
}

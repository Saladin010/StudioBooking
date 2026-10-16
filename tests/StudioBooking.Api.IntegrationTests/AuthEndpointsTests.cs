using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using StudioBooking.Api.Controllers;
using StudioBooking.Domain.Entities;
using StudioBooking.Infrastructure.Persistence;

namespace StudioBooking.Api.IntegrationTests;

public class AuthEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetMe_WithoutToken_Returns401Unauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostToken_WithNonExistentEmail_Returns401Unauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new TokenRequest("nonexistent@example.com");

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/token", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostToken_WithExistingUserInDatabase_Returns200OkAndValidTokenWithRole()
    {
        // Arrange: Insert User directly into Testcontainers database via DbContext
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var existingUser = new User
            {
                Id = Guid.NewGuid(),
                Email = $"artist_{Guid.NewGuid():N}@example.com",
                Role = "Artist"
            };
            dbContext.Users.Add(existingUser);
            await dbContext.SaveChangesAsync();

            var client = _factory.CreateClient();
            var request = new TokenRequest(existingUser.Email);

            // Act 1: Request token for existing user
            var tokenResponse = await client.PostAsJsonAsync("/api/auth/token", request);
            tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var tokenResult = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>();
            tokenResult.Should().NotBeNull();
            tokenResult!.Token.Should().NotBeNullOrWhiteSpace();

            // Act 2: Access protected /api/auth/me using Bearer token
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.Token);
            var meResponse = await client.GetAsync("/api/auth/me");

            // Assert
            meResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var meContent = await meResponse.Content.ReadFromJsonAsync<JsonElement>();
            meContent.GetProperty("email").GetString().Should().Be(existingUser.Email);
            meContent.GetProperty("role").GetString().Should().Be(existingUser.Role);
        }
    }

    [Fact]
    public async Task PostToken_WithEmptyEmail_Returns400BadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new TokenRequest(string.Empty);

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/token", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

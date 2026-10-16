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

public class AuthorizationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthorizationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetDashboard_WithoutToken_Returns401Unauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/admin/dashboard");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetDashboard_WithCustomerToken_Returns403Forbidden()
    {
        // Arrange: Insert Customer into database directly via DbContext
        var customerEmail = $"customer_{Guid.NewGuid():N}@example.com";
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var customerUser = new User
            {
                Id = Guid.NewGuid(),
                Email = customerEmail,
                Role = "Customer"
            };
            dbContext.Users.Add(customerUser);
            await dbContext.SaveChangesAsync();
        }

        var client = _factory.CreateClient();

        // Request token for Customer
        var tokenResponse = await client.PostAsJsonAsync("/api/auth/token", new TokenRequest(customerEmail));
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var tokenResult = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>();
        tokenResult.Should().NotBeNull();
        tokenResult!.Token.Should().NotBeNullOrWhiteSpace();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.Token);

        // Act
        var response = await client.GetAsync("/api/admin/dashboard");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetDashboard_WithAdminToken_Returns200Ok()
    {
        // Arrange: Insert Admin into database directly via DbContext
        var adminEmail = $"admin_{Guid.NewGuid():N}@example.com";
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var adminUser = new User
            {
                Id = Guid.NewGuid(),
                Email = adminEmail,
                Role = "Admin"
            };
            dbContext.Users.Add(adminUser);
            await dbContext.SaveChangesAsync();
        }

        var client = _factory.CreateClient();

        // Request token for Admin
        var tokenResponse = await client.PostAsJsonAsync("/api/auth/token", new TokenRequest(adminEmail));
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var tokenResult = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>();
        tokenResult.Should().NotBeNull();
        tokenResult!.Token.Should().NotBeNullOrWhiteSpace();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.Token);

        // Act
        var response = await client.GetAsync("/api/admin/dashboard");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        content.GetProperty("message").GetString().Should().Be("Admin Access Granted");
    }
}

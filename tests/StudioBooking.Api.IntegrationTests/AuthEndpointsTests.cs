using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using StudioBooking.Api.Controllers;

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
    public async Task PostToken_WithValidEmail_GeneratesValidTokenAndGetMeReturns200Ok()
    {
        // Arrange
        var client = _factory.CreateClient();
        const string expectedEmail = "artist@example.com";
        var request = new TokenRequest(expectedEmail);

        // Act 1: Request token
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
        meContent.GetProperty("email").GetString().Should().Be(expectedEmail);
    }
}

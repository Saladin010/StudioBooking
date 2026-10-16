using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace StudioBooking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public AuthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost("token")]
    public IActionResult GenerateToken([FromBody] TokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { message = "Email is required." });
        }

        var jwtSection = _configuration.GetSection("Jwt");
        var secretKey = jwtSection["SecretKey"] ?? "StudioBooking_Super_Secret_Jwt_Signing_Key_For_Development_Only_123456!";
        var issuer = jwtSection["Issuer"] ?? "StudioBooking";
        var audience = jwtSection["Audience"] ?? "StudioBookingApi";

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, request.Email),
            new Claim(JwtRegisteredClaimNames.Email, request.Email),
            new Claim(ClaimTypes.Name, request.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = creds
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var jwt = tokenHandler.WriteToken(token);

        return Ok(new TokenResponse(jwt));
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        var email = User.FindFirstValue(ClaimTypes.Email)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Email)
            ?? User.FindFirstValue(ClaimTypes.Name)
            ?? User.Identity?.Name;

        var name = User.FindFirstValue(ClaimTypes.Name) ?? email;

        return Ok(new { email, name });
    }
}

public record TokenRequest(string? Email);
public record TokenResponse(string Token);

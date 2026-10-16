using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using StudioBooking.Infrastructure;
using StudioBooking.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddInfrastructure(builder.Configuration);

var jwtSection = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSection["SecretKey"] ?? "StudioBooking_Super_Secret_Jwt_Signing_Key_For_Development_Only_123456!";
var issuer = jwtSection["Issuer"] ?? "StudioBooking";
var audience = jwtSection["Audience"] ?? "StudioBookingApi";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", async (StudioBookingDbContext dbContext) =>
{
    try
    {
        var canConnect = await dbContext.Database.CanConnectAsync();
        return canConnect
            ? Results.Ok(new { status = "Healthy", database = "Connected" })
            : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
    catch
    {
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
});

app.MapControllers();

app.Run();

public partial class Program { }

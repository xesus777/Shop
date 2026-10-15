using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Shop.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<Pr3GlebOmaContext>();

var hasher = new PasswordHasher<User>();

var users = new List<User>
{
    new() { Id = 1, Phone = "89265424450", Address = "Moscow", Login = "student", RoleId = 2 },
    new() { Id = 2, Phone = "89265424450", Address = "Moscow", Login = "student", RoleId = 1 },
    new() { Id = 3, Phone = "89265424450", Address = "Moscow", Login = "student", RoleId = 1 }
    
};

foreach (var user in users)
{
    user.Password = hasher.HashPassword(user, "123");
}



var key = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32));

var validation = new TokenValidationParameters
{
    ValidateIssuer = true,
    ValidIssuer = "JwtDemo",

    ValidateAudience = true,
    ValidAudience = "JwtDemoApi",

    ValidateIssuerSigningKey = true,
    IssuerSigningKey = key,

    ValidateLifetime = true,
    ClockSkew = TimeSpan.Zero,

    NameClaimType = "login",
    RoleClaimType = "role"
};

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = validation;
    });

builder.Services.AddAuthorization();





var app = builder.Build();

app.MapPost("/api/newOrder", async (Pr3GlebOmaContext context, Order order) => {
    
    context.Orders.Add(order);
    await context.SaveChangesAsync();
    return Results.Created($"/api/orders/{order.Id}", order);
}).RequireAuthorization(policy => policy.RequireAuthenticatedUser()); ;


app.UseAuthentication();
app.UseAuthorization();




app.MapPost("/api/auth/login", (LoginRequest request) =>
{
    var account = users.FirstOrDefault(
        a => a.Login == request.Login);

    if (account == null ||
        string.IsNullOrWhiteSpace(request.Password))
        return Results.Challenge();

    var result = hasher.VerifyHashedPassword(
        account, account.Password, request.Password);

    if (result == PasswordVerificationResult.Failed)
        return Results.Challenge();

    return Results.Ok(new
    {
        access_token = CreateToken(account),
        token_type = "Bearer"
    });
}).AllowAnonymous();


app.MapGet("/api/me", (ClaimsPrincipal user) =>
{
    return Results.Ok(new
    {
        login = user.Identity?.Name,
        role = user.FindFirst("role")?.Value
    });
}).RequireAuthorization();

app.MapGet("/api/admin", () =>
{
    return Results.Ok("Раздел администратора");
}).RequireAuthorization(policy => policy.RequireAuthenticatedUser().RequireRole("Admin"));

app.Run();

string CreateToken(User account)
{
    var claims = new[]
    {
        new Claim("sub", account.Id.ToString()),
        new Claim("login", account.Login),
        new Claim("role", account.RoleId.ToString())
    };
    
    var token = new JwtSecurityToken(
        issuer: "JwtDemo",
        audience: "JwtDemoApi",
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(15),
        signingCredentials: new SigningCredentials(
            key, SecurityAlgorithms.HmacSha256));

    return new JwtSecurityTokenHandler().WriteToken(token);
}

public record LoginRequest(string Login, string Password);


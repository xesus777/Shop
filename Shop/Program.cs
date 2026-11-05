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
    new() { Id = 1, Phone = "89265424450", DeliveryAddress = "Moscow", Login = "adm", RoleId = 1 },
    new() { Id = 2, Phone = "89265424450", DeliveryAddress = "Moscow", Login = "student", RoleId = 2 },
    new() { Id = 3, Phone = "89265424450", DeliveryAddress = "Moscow", Login = "student", RoleId = 2 }
    
};

foreach (var user in users)
{
    user.PasswordHash = hasher.HashPassword(user, "123");
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
}).RequireAuthorization(policy => policy.RequireAuthenticatedUser());


app.MapPut("/api/editCountBySize/{ClotheId}&{SizeId}&{Quantity}", async (Pr3GlebOmaContext context, int ClotheId, int SizeId, int Quantity) => {
    var order = context.OrderItems.FirstOrDefault(u=>u.ClothingId == ClotheId && u.SizeId == SizeId);
    if (order == null) return Results.NotFound(new { message = "no" });
    order.Quantity=Quantity;
    await context.SaveChangesAsync();
    return Results.Json(order);
}).RequireAuthorization(policy => policy.RequireAuthenticatedUser().RequireRole("1"));

app.MapPut("/api/editStatusByOrderId/{OrderId}&{StatusId}", async (Pr3GlebOmaContext context, int OrderId, int StatusId) => {
    var order = context.Orders.FirstOrDefault(u => u.Id == OrderId);
    if (order == null) return Results.NotFound(new { message = "no" });
    order.StatusId = StatusId;
    await context.SaveChangesAsync();
    return Results.Json(order);
}).RequireAuthorization(policy => policy.RequireAuthenticatedUser().RequireRole("1"));

app.MapPut("/api/delTovar/{id}", async (Pr3GlebOmaContext context, int id) =>
{
    Clothing? clothe = context.Clothings.FirstOrDefault(u => u.Id == id);

    if (clothe == null) return Results.NotFound(new { message = "no" });

    clothe.IsAvailable = false;
    await context.SaveChangesAsync();
    return Results.Json(clothe);
}).RequireAuthorization(policy => policy.RequireAuthenticatedUser().RequireRole("1"));

app.MapPut("/api/backTovar/{id}", async (Pr3GlebOmaContext context, int id) =>
{
    Clothing? clothe = context.Clothings.FirstOrDefault(u => u.Id == id);

    if (clothe == null) return Results.NotFound(new { message = "no" });

    clothe.IsAvailable = true;
    await context.SaveChangesAsync();
    return Results.Json(clothe);
}).RequireAuthorization(policy => policy.RequireAuthenticatedUser().RequireRole("1"));

app.MapGet("/api/orders/{id}", (Pr3GlebOmaContext context) => Results.Json(context.Orders)).RequireAuthorization(policy => policy.RequireAuthenticatedUser().RequireRole("1"));



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
        account, account.PasswordHash, request.Password);

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
}).RequireAuthorization(policy => policy.RequireAuthenticatedUser().RequireRole("1"));



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


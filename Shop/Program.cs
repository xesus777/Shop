using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Shop.Models;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<Pr3GlebOmaContext>();
var app = builder.Build();
// 
app.MapGet("/api/Clothes", (Pr3GlebOmaContext context) => Results.Json(context.Clothings));
app.MapGet("/api/Clothes/search", (string name, Pr3GlebOmaContext context) =>
{
    if (string.IsNullOrWhiteSpace(name))
    {
        return Results.BadRequest("Параметр поиска 'name' не можеи быть пустым.");
    }

    // 
    var foundClothes = context.Clothings
        .Where(c => c.IsAvailable == true && c.Name.Contains(name))
        .ToList();

    return Results.Ok(foundClothes);
});
// 3. 
app.MapPost("/api/Users/register", (User newUser, Pr3GlebOmaContext context) =>
{
    var defaultRole = context.Roles.FirstOrDefault(r => r.Name == "User");
    if (defaultRole == null) return Results.BadRequest("Поле 'User' не может быть пустым.");

    newUser.RoleId = defaultRole.Id;


    var hasher = new PasswordHasher<User>();
    newUser.PasswordHash = hasher.HashPassword(newUser, newUser.PasswordHash);

    context.Users.Add(newUser);
    context.SaveChanges();

    return Results.Ok("Пользователь зарегистрирован!");
});

app.MapGet("/api/Orders/{StatusId}", async (Pr3GlebOmaContext context, int StatusId) =>
{
    Order? order = await context.Orders.FirstOrDefaultAsync(u => u.Id == StatusId);
        if (order == null) 
        return Results.NotFound(new { message = "Заказ не найден" });
    return Results.Json(order);
}).RequireAuthorization(policy => policy.RequireAuthenticatedUser());

app.MapGet("/api/OrdersByUserId/{UserId}", async (Pr3GlebOmaContext context, int UserId) =>
{
    Order? order = await context.Orders.FirstOrDefaultAsync(u => u.Id == UserId);
    if (order == null)
        return Results.NotFound(new { message = "Заказ не найден" });
    return Results.Json(order);
}).RequireAuthorization(policy => policy.RequireAuthenticatedUser());
app.Run();
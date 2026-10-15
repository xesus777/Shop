using Microsoft.AspNetCore.Identity;
using Shop.Models;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<Pr3GlebOmaContext>();
var app = builder.Build();
// Метод вывода списка всей одежды
app.MapGet("/api/Clothes", (Pr3GlebOmaContext context) => Results.Json(context.Clothes));
app.MapGet("/api/Clothes/search", (string name, Pr3GlebOmaContext context) =>
{
    if (string.IsNullOrWhiteSpace(name))
    {
        return Results.BadRequest("Параметр поиска 'name' не может быть пустым.");
    }

    // Синхронный поиск по части названия
    var foundClothes = context.Clothes
        .Where(c => c.Available == true && c.Name.Contains(name))
        .ToList();

    return Results.Ok(foundClothes);
});
// 3. Метод регистрации нового пользователя
app.MapPost("/api/Users/register", (User newUser, Pr3GlebOmaContext context) =>
{
    var defaultRole = context.Roles.FirstOrDefault(r => r.Name == "User");
    if (defaultRole == null) return Results.BadRequest("Роль 'Пользователь' не найдена в БД.");

    newUser.RoleId = defaultRole.Id;

    
    var hasher = new PasswordHasher<User>();
    newUser.Password = hasher.HashPassword(newUser, newUser.Password);

    context.Users.Add(newUser);
    context.SaveChanges();

    return Results.Ok("Регистрация успешна!");
});


app.Run();

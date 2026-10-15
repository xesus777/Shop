using Microsoft.AspNetCore.Identity;
using Shop.Models;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<Pr3GlebOmaContext>();
var app = builder.Build();

app.MapGet("/api/Clothes", (Pr3GlebOmaContext context) => Results.Json(context.Clothes));

app.Run();

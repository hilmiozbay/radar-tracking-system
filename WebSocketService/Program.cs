using Microsoft.EntityFrameworkCore;
using MongoDB.EntityFrameworkCore.Extensions;
using WebSocketService.DbContextClass;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// CORS ayarları ekliyorum
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// MongoDB bağlantı ayarları - configuration'dan al
var mongoConnectionString = builder.Configuration.GetConnectionString("MongoDB") ?? "mongodb://localhost:27017";
var mongoDatabaseName = builder.Configuration.GetConnectionString("DatabaseName") ?? "radar";

// DbContext'i ekle
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMongoDB(mongoConnectionString, mongoDatabaseName));

var app = builder.Build();

// CORS'u aktif et
app.UseCors();

// WebSocket options
var webSocketOptions = new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromMinutes(2)
};

app.UseWebSockets(webSocketOptions);

app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();

Console.WriteLine("WebSocketService başlatıldı. Kafka'dan track verilerini dinliyor ve WebSocket'e aktarıyor...");
app.Run();
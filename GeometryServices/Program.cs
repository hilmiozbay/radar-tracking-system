using GeometryServices.DbContextClass;
using GeometryServices.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Database contexts
builder.Services.AddDbContext<ApplicationDbContext>();
builder.Services.AddDbContext<AreaDbContext>();

// Kafka Producer Service
builder.Services.AddSingleton(provider => 
    new KafkaProducerService("localhost:9092"));

// Background service
builder.Services.AddHostedService<GeometryServiceManager>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

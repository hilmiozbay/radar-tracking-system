using IFFService.DbContextClass;
using IFFService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.EntityFrameworkCore.Extensions;
using TrackLibrary;

var builder = WebApplication.CreateBuilder(args);

// MongoDB bağlantı ayarları - configuration'dan al
var mongoConnectionString = builder.Configuration.GetConnectionString("MongoDB") ?? "mongodb://localhost:27017";
var mongoDatabaseName = builder.Configuration.GetConnectionString("DatabaseName") ?? "radar";

// Kafka ayarları - configuration'dan al
var kafkaBootstrapServers = builder.Configuration.GetSection("Kafka")["BootstrapServers"] ?? "localhost:9092";

// DbContext'i ekle
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMongoDB(mongoConnectionString, mongoDatabaseName));

// KafkaProducerService'i ekle
builder.Services.AddSingleton<KafkaProducerService>(sp =>
    new KafkaProducerService(kafkaBootstrapServers));

// IffManagerService'i HostedService olarak ekle
builder.Services.AddHostedService<IffManagerService>();

// Controllers ekle (opsiyonel - API endpoint'leri için)
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

// MongoDB'de collection oluştur
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.EnsureCreated();
}

Console.WriteLine("IFF Service başlatıldı. RadarService'den new_track dinliyor, update_track gönderilyor...");
app.Run();

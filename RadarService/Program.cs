using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.EntityFrameworkCore.Extensions;
using RadarService.DbContextClass;
using RadarService.Services;
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

// Kafka Producer servisini ekle
builder.Services.AddSingleton<KafkaProducerService>(sp =>
    new KafkaProducerService(kafkaBootstrapServers));

// TrackManagerService'i ekle
builder.Services.AddHostedService<TrackManagerService>();

// Controller'ları ekle
builder.Services.AddControllers();

var app = builder.Build();

// Controller route'larını ekle
app.MapControllers();

Console.WriteLine("RadarService başlatıldı. Track'ler üretiliyor ve Kafka'ya gönderiliyor...");
app.Run();

// Topics: new_track (yeni track'ler IFF'e), update_track (güncellenmiş track'ler WebSocket'e)
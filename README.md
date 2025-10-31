# Radar Tracking System

Real-time radar tracking system coded with microservices structure.

## Used technology
- Frontend: React, Leaflet
- Backend: .NET
- Microservice Communication: Apache Kafka
- Database: MongoDB
- Working Environment: Docker
  
## Servises

- **RadarService**: Create track and update location for tracks
- **IFFService**: IFF (Identify Friend or Foe) analyzing
- **WebSocketService**: Real-time UI data streaming
- **TrackLibrary**: Common data model
- **Real Time UI**: React-Leaflet map

## Quick Start

### 1. Prerequisites
- .NET 9.0 SDK
- Docker & Docker Compose
  

### 2. Starting Service

#### **Windows:**
```bash
./allservices.bat
```

#### **macOS/Linux:**

**In terminal:**
```bash
./allservices.sh
```

**Manuel starting:**
```bash
cd RadarService && dotnet run
cd IFFService && dotnet run  
cd WebSocketService && dotnet run
cd ui-service && npm start
```

### 4. Monitoring
- **Kafka UI**: http://localhost:8080
- **WebSocket Dashboard**: http://localhost:5138
- **MongoDB**: https://localhost:27017
- **Real-time UI**: https://localhost:3000

## 📊 Data Flow

```
RadarService → new_track topic → IFFService
     ↓                              ↓
update_track topic              update_track topic
     ↓                              ↓
WebSocketService ← ← ← ← ← ← ← ← ← ← ←
     ↓
Real-time UI
```

## 🔧 Configuration

Servisler `appsettings.json` dosyalarından konfigürasyon alır:

```json
{
  "Kafka": {
    "BootstrapServers": "localhost:9092"
  },
  "ConnectionStrings": {
    "MongoDB": "mongodb://localhost:27017",
    "DatabaseName": "radar"
  }
}
```

## 📝 Kafka Topics

- `new_track`: New tracks (RadarService → IFFService)
- `update_track`: IFF enriched + position updates (other services → WebSocketService)

## 🛠️ Development

```bash
# Build all projects
dotnet build

# Clean infrastructure
docker-compose down -v

# View logs (simple mode)
tail -f logs/*.log
```



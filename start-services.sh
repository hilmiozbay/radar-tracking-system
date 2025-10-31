#!/bin/bash

echo "========================================"
echo " 🛰️ Radar Tracking System Başlatılıyor"
echo "========================================"

echo "📦 Infrastructure başlatılıyor (Kafka, MongoDB)..."
docker-compose up -d
if [ $? -ne 0 ]; then
    echo "❌ Docker Compose başlanamadı! Docker Desktop'ın çalıştığını kontrol edin."
    read -p "Devam etmek için Enter'a basın..."
    exit 1
fi

echo "⏳ Infrastructure'ın hazır olmasını bekliyoruz..."
sleep 10

echo "📡 IFF Service başlatılıyor..."
osascript -e 'tell app "Terminal" to do script "cd \"'$(pwd)'\" && cd IFFService && dotnet run"' &
sleep 5

echo "🛰️ Radar Service başlatılıyor..."
osascript -e 'tell app "Terminal" to do script "cd \"'$(pwd)'\" && cd RadarService && dotnet run"' &
sleep 3

echo "🌐 WebSocket Service başlatılıyor..."
osascript -e 'tell app "Terminal" to do script "cd \"'$(pwd)'\" && cd WebSocketService && dotnet run"' &

echo "✅ Tüm servisler başlatıldı!"
echo ""
echo "📊 Monitoring URL'leri:"
echo "  • Kafka UI:        http://localhost:8080"
echo "  • WebSocket UI:    http://localhost:5138"
echo "  • MongoDB:         mongodb://localhost:27017"
echo ""
echo "⚠️  Servisleri kapatmak için: docker-compose down"
echo "⚠️  Terminal pencerelerini manuel olarak kapatabilirsiniz"
echo "========================================" 
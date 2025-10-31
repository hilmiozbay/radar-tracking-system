echo  Radar Tracking System is starting


echo Radar Service is starting...
start "Radar Service" cmd /k "cd RadarService && dotnet run"
timeout /t 3 /nobreak > nul

echo IFF Service is starting..
start "IFF Service" cmd /k "cd IFFService && dotnet run"
timeout /t 5 /nobreak > nul

echo WebSocket Service is starting...
start "WebSocket Service" cmd /k "cd WebSocketService && dotnet run"



echo Monitoring URL'leri:
echo  WebSocket UI:     http://localhost:5062
echo  MongoDB:         mongodb://localhost:27017
echo  Real-time UI      http://localhost:3000


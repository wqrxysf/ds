# scripts/start.ps1
$PROJECT_PATH = "C:\DP\DISTRIBUTED-PROGRAMMING\Valuator"
$NGINX_PATH = "C:\nginx"

Write-Host "Запуск Valuator + Nginx..."

# Запуск экземпляров
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$PROJECT_PATH'; dotnet run --urls 'http://0.0.0.0:5001'"
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$PROJECT_PATH'; dotnet run --urls 'http://0.0.0.0:5002'"
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$PROJECT_PATH'; dotnet run --urls 'http://0.0.0.0:5003'"

Start-Sleep -Seconds 5

# Запуск Nginx
Set-Location $NGINX_PATH
.\nginx.exe

Write-Host "Система запущена: http://localhost:8080"
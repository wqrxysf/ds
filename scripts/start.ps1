$ProjectPath = "C:\DP\DISTRIBUTED-PROGRAMMING"
$ValuatorPath = "$ProjectPath\Valuator"
$NginxPath = "C:\DP\DISTRIBUTED-PROGRAMMING\nginx"
$RankCalcPath = "$ProjectPath\RankCalculator"
$EventsLoggerPath = "$ProjectPath\EventsLogger"
$RedisPath = "C:\Redis"
$BaseDataDir = "C:\Redis\data"

$RedisPassword = [Environment]::GetEnvironmentVariable("Redis__Password", "User")

dotnet clean "$ValuatorPath\Valuator.csproj" --verbosity quiet
dotnet build "$ValuatorPath\Valuator.csproj" --configuration Debug --verbosity quiet

dotnet clean "$RankCalcPath\RankCalculator.csproj" --verbosity quiet
dotnet build "$RankCalcPath\RankCalculator.csproj" --configuration Debug --verbosity quiet

New-Item -ItemType Directory -Path "$BaseDataDir\main" -Force | Out-Null
New-Item -ItemType Directory -Path "$BaseDataDir\ru" -Force | Out-Null
New-Item -ItemType Directory -Path "$BaseDataDir\eu" -Force | Out-Null
New-Item -ItemType Directory -Path "$BaseDataDir\asia" -Force | Out-Null

Write-Host "Запуск Redis экземпляров"

function Start-RedisInstance {
    param($Port, $Name, $DataSubDir)
    
    $dataDir = Join-Path $BaseDataDir $DataSubDir
    
    $args = "--port $Port --appendonly yes --dir `"$dataDir`" --requirepass $RedisPassword"
    
    Start-Process -FilePath "$RedisPath\redis-server.exe" -ArgumentList $args -WindowStyle Hidden
}

Start-RedisInstance -Port 6379 -Name "MAIN" -DataSubDir "main"
Start-RedisInstance -Port 6380 -Name "RU"   -DataSubDir "ru"
Start-RedisInstance -Port 6381 -Name "EU"   -DataSubDir "eu"
Start-RedisInstance -Port 6382 -Name "ASIA" -DataSubDir "asia"

Start-Sleep -Seconds 2

Write-Host "Запуск экземпляра на порту 5001"
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$ValuatorPath'; dotnet run --no-build --urls 'http://127.0.0.1:5001'"

Write-Host "Запуск экземпляра на порту 5002"
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$ValuatorPath'; dotnet run --no-build --urls 'http://127.0.0.1:5002'"

Write-Host "Запуск RankCalculator 1"
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd `"$RankCalcPath`"; dotnet run --no-build"

Write-Host "Запуск RankCalculator 2"
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd `"$RankCalcPath`"; dotnet run --no-build"

Write-Host "Запуск EventsLogger 1"
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd `"$EventsLoggerPath`"; dotnet run --no-build"

Write-Host "Запуск EventsLogger 2"
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd `"$EventsLoggerPath`"; dotnet run --no-build"

Start-Sleep -Seconds 5

Write-Host "Запуск Nginx на порту 8080"
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$NginxPath'; .\nginx.exe"

Write-Host "`n Система запущена!" -ForegroundColor Green
Write-Host "Откройте в браузере: http://localhost:8080" -ForegroundColor Cyan
Write-Host "Для остановки выполните: .\scripts\stop.ps1" -ForegroundColor Yellow
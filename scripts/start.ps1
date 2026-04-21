$ProjectPath = "C:\DP\DISTRIBUTED-PROGRAMMING"
$ValuatorPath = "$ProjectPath\Valuator"
$NginxPath = "C:\DP\DISTRIBUTED-PROGRAMMING\nginx"
$RankCalcPath = "$ProjectPath\RankCalculator"
$EventsLoggerPath = "$ProjectPath\EventsLogger"

dotnet clean "$ValuatorPath\Valuator.csproj" --verbosity quiet
dotnet clean "$RankCalcPath\RankCalculator.csproj" --verbosity quiet
dotnet clean "$EventsLoggerPath\EventsLogger.csproj" --verbosity quiet

dotnet build "$ValuatorPath\Valuator.csproj" --configuration Debug --verbosity quiet
dotnet build "$RankCalcPath\RankCalculator.csproj" --configuration Debug --verbosity quiet
dotnet build "$EventsLoggerPath\EventsLogger.csproj" --configuration Debug --verbosity quiet

Write-Host "Запуск экземпляра на порту 5001"
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$ValuatorPath'; dotnet run --urls 'http://0.0.0.0:5001'"

Write-Host "Запуск экземпляра на порту 5002"
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$ValuatorPath'; dotnet run --urls 'http://0.0.0.0:5002'"

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
Write-Host "Остановка системы" -ForegroundColor Red

Write-Host "Остановка Nginx..."
$NginxPath = "C:\DP\DISTRIBUTED-PROGRAMMING\nginx"
Stop-Process -Name "nginx" -Force -ErrorAction SilentlyContinue

Write-Host "Остановка экземпляров приложения"
Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Stop-Process -Force

Write-Host "`n Система остановлена!" -ForegroundColor Green
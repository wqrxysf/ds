Write-Host "Остановка системы" -ForegroundColor Red

Write-Host "Остановка Nginx..."
$NginxPath = "C:\DP\DISTRIBUTED-PROGRAMMING\nginx"
Stop-Process -Name "nginx" -Force -ErrorAction SilentlyContinue

Write-Host "Остановка экземпляров приложения"
Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Stop-Process -Force

$currentPID = $PID
Get-Process powershell | Where-Object { $_.Id -ne $currentPID } | Stop-Process -Force -ErrorAction SilentlyContinue

Write-Host "`n Система остановлена!" -ForegroundColor Green

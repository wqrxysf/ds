Write-Host "Остановка системы" -ForegroundColor Red

Write-Host "Остановка экземпляров приложения"
Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Stop-Process -Force

$currentPID = $PID
Get-Process powershell | Where-Object { $_.Id -ne $currentPID } | Stop-Process -Force -ErrorAction SilentlyContinue

# 2. ТОЛЬКО ПОТОМ: Остановить Redis
Write-Host "Завершение процессов Redis..." -ForegroundColor Yellow
$redisProcesses = Get-Process redis-server -ErrorAction SilentlyContinue
if ($redisProcesses) {
    foreach ($proc in $redisProcesses) {
        taskkill /F /PID $proc.Id 2>$null
        Write-Host "  Завершен Redis (PID: $($proc.Id))"
    }
} else {
    Write-Host "  Процессы Redis не найдены"
}
Write-Host "Остановка Nginx..."
$NginxPath = "C:\DP\DISTRIBUTED-PROGRAMMING\nginx"
Stop-Process -Name "nginx" -Force -ErrorAction SilentlyContinue

Write-Host "`n Система остановлена!" -ForegroundColor Green

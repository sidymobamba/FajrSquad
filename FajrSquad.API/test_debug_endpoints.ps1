# Test script per gli endpoint debug del sistema notifiche
# Assicurati che l'applicazione sia in esecuzione su http://localhost:5000

$baseUrl = "http://localhost:5000"

Write-Host "🧪 Testing Debug Endpoints for Notification System" -ForegroundColor Green
Write-Host "=================================================" -ForegroundColor Green

# Test 1: Seed di un utente di test
Write-Host "`n1. Testing /debug/seed-user endpoint..." -ForegroundColor Yellow
try {
    $seedResponse = Invoke-RestMethod -Uri "$baseUrl/debug/seed-user" -Method POST -ContentType "application/json" -Body '{"token":"test_device_token_123","timeZone":"Africa/Dakar"}'
    Write-Host "✅ Seed user successful: $($seedResponse | ConvertTo-Json)" -ForegroundColor Green
    $userId = $seedResponse.userId
} catch {
    Write-Host "❌ Seed user failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Test 2: Verifica informazioni timezone
Write-Host "`n2. Testing /debug/when endpoint..." -ForegroundColor Yellow
try {
    $whenResponse = Invoke-RestMethod -Uri "$baseUrl/debug/when?userId=$userId" -Method GET
    Write-Host "✅ When endpoint successful: $($whenResponse | ConvertTo-Json)" -ForegroundColor Green
} catch {
    Write-Host "❌ When endpoint failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 3: Programma una notifica di test
Write-Host "`n3. Testing /debug/enqueue endpoint..." -ForegroundColor Yellow
try {
    $enqueueResponse = Invoke-RestMethod -Uri "$baseUrl/debug/enqueue" -Method POST -ContentType "application/json" -Body '{"type":"Debug","delaySeconds":5}'
    Write-Host "✅ Enqueue successful: $($enqueueResponse | ConvertTo-Json)" -ForegroundColor Green
} catch {
    Write-Host "❌ Enqueue failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 4: Verifica notifiche pending
Write-Host "`n4. Testing /debug/pending endpoint..." -ForegroundColor Yellow
try {
    $pendingResponse = Invoke-RestMethod -Uri "$baseUrl/debug/pending" -Method GET
    Write-Host "✅ Pending endpoint successful: $($pendingResponse | ConvertTo-Json)" -ForegroundColor Green
} catch {
    Write-Host "❌ Pending endpoint failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 5: Aspetta 10 secondi per l'elaborazione
Write-Host "`n5. Waiting 10 seconds for notification processing..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

# Test 6: Verifica di nuovo le notifiche pending
Write-Host "`n6. Testing /debug/pending endpoint again..." -ForegroundColor Yellow
try {
    $pendingResponse2 = Invoke-RestMethod -Uri "$baseUrl/debug/pending" -Method GET
    Write-Host "✅ Pending endpoint successful: $($pendingResponse2 | ConvertTo-Json)" -ForegroundColor Green
} catch {
    Write-Host "❌ Pending endpoint failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 7: Verifica i log delle notifiche
Write-Host "`n7. Testing /debug/logs endpoint..." -ForegroundColor Yellow
try {
    $logsResponse = Invoke-RestMethod -Uri "$baseUrl/debug/logs?userId=$userId&last=10" -Method GET
    Write-Host "✅ Logs endpoint successful: $($logsResponse | ConvertTo-Json)" -ForegroundColor Green
} catch {
    Write-Host "❌ Logs endpoint failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 8: Test push diretto
Write-Host "`n8. Testing /debug/push endpoint..." -ForegroundColor Yellow
try {
    $pushResponse = Invoke-RestMethod -Uri "$baseUrl/debug/push" -Method POST
    Write-Host "✅ Push endpoint successful: $($pushResponse | ConvertTo-Json)" -ForegroundColor Green
} catch {
    Write-Host "❌ Push endpoint failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 9: Health check
Write-Host "`n9. Testing /health/notifications endpoint..." -ForegroundColor Yellow
try {
    $healthResponse = Invoke-RestMethod -Uri "$baseUrl/health/notifications" -Method GET
    Write-Host "✅ Health check successful: $($healthResponse | ConvertTo-Json)" -ForegroundColor Green
} catch {
    Write-Host "❌ Health check failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n🎉 Debug endpoints testing completed!" -ForegroundColor Green
Write-Host "=================================================" -ForegroundColor Green

# 🧪 Guida Debug Notifiche - FajrSquad

## 🎯 Panoramica

Questa suite di strumenti di debug permette di testare immediatamente la pipeline notifiche in ambiente Development, senza dover aspettare i job schedulati o configurare FCM reale.

## ⚙️ Configurazione

### ForceWindow in Development

In `appsettings.Development.json`:

```json
{
  "Notifications": {
    "ForceWindow": true,
    "WindowMinutes": 10,
    "MorningTime": "06:30",
    "EveningTime": "21:30",
    "MorningToleranceMinutes": 5,
    "EveningToleranceMinutes": 5
  }
}
```

**ForceWindow=true** fa sì che i job Morning/Evening schedulino notifiche anche fuori orario in Development.

## 🛠️ Endpoint Debug

Tutti gli endpoint sono disponibili solo in **Development** e sotto `/debug/`:

**Nota**: Tutti gli endpoint di debug richiedono autenticazione JWT e usano automaticamente l'utente dal token.

### 1. **POST** `/debug/seed-user`
Crea/aggiorna un utente di test con DeviceToken valido.

**Body (opzionale):**
```json
{
  "token": "IL_TUO_TOKEN_FCM_REALE",
  "timeZone": "Africa/Dakar"
}
```

**Risposta:**
```json
{
  "userId": "01993104-fa14-769e-8d89-f929073af43e",
  "token": "FAKE_TOKEN_FOR_TESTING",
  "timezone": "Africa/Dakar",
  "preferences": {
    "morning": true,
    "evening": true,
    "fajrMissed": true,
    "escalation": true,
    "hadithDaily": true,
    "motivationDaily": true,
    "eventsNew": true,
    "eventsReminder": true
  }
}
```

### 2. **POST** `/debug/enqueue`
Schedula una notifica per esecuzione immediata (usa automaticamente l'utente dal token JWT).

**Body:**
```json
{
  "type": "Debug",
  "delaySeconds": 60
}
```

**Risposta:**
```json
{
  "scheduledNotificationId": 123,
  "userId": "01993104-fa14-769e-8d89-f929073af43e",
  "type": "Debug",
  "executeAt": "2025-09-24T10:30:00Z",
  "uniqueKey": "Debug:01993104-fa14-769e-8d89-f929073af43e:20250924103000"
}
```

### 3. **POST** `/debug/push`
Invia un push diretto all'utente corrente (dal token JWT).

**Body:** Nessuno (usa automaticamente l'utente dal token)

**Risposta:**
```json
{
  "messageId": "550e8400-e29b-41d4-a716-446655440000"
}
```

### 4. **GET** `/debug/when`
Mostra calcoli timezone e preferenze per l'utente corrente (dal token JWT).

**Risposta:**
```json
{
  "nowUtc": "2025-09-24T10:30:00Z",
  "deviceTimeZone": "Africa/Dakar",
  "nowLocal": "2025-09-24T10:30:00+00:00",
  "morningTarget": "06:30:00",
  "eveningTarget": "21:30:00",
  "force": true,
  "preferences": {
    "morning": true,
    "evening": true,
    "fajrMissed": true,
    "escalation": true,
    "hadithDaily": true,
    "motivationDaily": true,
    "eventsNew": true,
    "eventsReminder": true
  }
}
```

### 5. **GET** `/debug/pending`
Mostra notifiche in coda per esecuzione.

**Risposta:**
```json
{
  "count": 2,
  "pending": [
    {
      "id": 123,
      "userId": "01993104-fa14-769e-8d89-f929073af43e",
      "type": "Debug",
      "executeAt": "2025-09-24T10:29:00Z",
      "uniqueKey": "Debug:01993104-fa14-769e-8d89-f929073af43e:20250924102900",
      "dataJson": "{\"UserId\":\"01993104-fa14-769e-8d89-f929073af43e\",\"Debug\":true}",
      "delaySeconds": -60
    }
  ]
}
```

### 6. **GET** `/debug/logs?last=50`
Mostra log delle notifiche inviate per l'utente corrente (dal token JWT).

**Risposta:**
```json
{
  "count": 3,
  "logs": [
    {
      "id": 456,
      "userId": "01993104-fa14-769e-8d89-f929073af43e",
      "type": "Debug",
      "result": "Sent",
      "providerMessageId": "550e8400-e29b-41d4-a716-446655440000",
      "error": null,
      "sentAt": "2025-09-24T10:30:00Z",
      "retried": 0
    }
  ]
}
```

### 7. **POST** `/api/notifications/debug/send`
Invia una notifica di test personalizzata (usa automaticamente l'utente dal token JWT).

**Body:**
```json
{
  "title": "Test Notification",
  "body": "This is a test notification",
  "data": {
    "customKey": "customValue"
  },
  "priority": "High"
}
```

**Risposta:**
```json
{
  "message": "Notifica di test inviata con successo",
  "messageId": "550e8400-e29b-41d4-a716-446655440000",
  "sentTo": "01993104-fa14-769e-8d89-f929073af43e"
}
```

## 🧪 Test End-to-End

### Flusso Completo di Test

1. **Seed utente di test:**
```bash
curl -X POST http://localhost:5000/debug/seed-user \
  -H "Content-Type: application/json" \
  -d '{"token":"IL_TUO_TOKEN","timeZone":"Africa/Dakar"}'
```

2. **Schedula notifica (1 secondo di ritardo):**
```bash
curl -X POST http://localhost:5000/debug/enqueue \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{"type":"Debug","delaySeconds":1}'
```

3. **Aspetta 3 secondi** per l'elaborazione

4. **Verifica che non ci siano notifiche pending:**
```bash
curl http://localhost:5000/debug/pending \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

5. **Controlla i log:**
```bash
curl "http://localhost:5000/debug/logs?last=10" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

6. **Test push diretto:**
```bash
curl -X POST http://localhost:5000/debug/push \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

7. **Test notifica personalizzata:**
```bash
curl -X POST http://localhost:5000/api/notifications/debug/send \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{"title":"Test Custom","body":"Custom test notification","priority":"High"}'
```

### Risultati Attesi

- ✅ **Seed**: Utente creato con preferenze ON e timezone valido
- ✅ **Enqueue**: Notifica schedulata con ID > 0
- ✅ **Pending**: 0 notifiche pending dopo elaborazione
- ✅ **Logs**: Almeno 1 entry con `result: "Sent"`
- ✅ **Push**: `messageId` non nullo
- ✅ **Fake FCM**: Notifica ricevuta nel fake sender

## 🔧 FCM Fake Sender

In Development, viene usato automaticamente un **FakeFcmNotificationSender** che:

- ✅ Simula l'invio FCM senza chiamate reali
- ✅ Salva le notifiche in una `ConcurrentBag` per testing
- ✅ Genera `messageId` fake per verifica
- ✅ Logga tutto per debugging

### Accesso alle Notifiche Fake

```csharp
// Nei test
var fakeSender = serviceProvider.GetRequiredService<INotificationSender>() as FakeFcmNotificationSender;
var sentNotifications = fakeSender.GetSentNotifications();
```

## 📊 Logging e Monitoring

### Log Attesi

```
info: FajrSquad.API.Jobs.MorningReminderJob[0]
      MorningCheck user=01993104-fa14-769e-8d89-f929073af43e nowLocal=14:30:00 target=06:30:00 tolerance=5m force=True send=True

info: FajrSquad.API.Jobs.ProcessScheduledNotificationsJob[0]
      ProcessScheduledNotificationsJob completed. Processed 1 notifications, 0 failed

info: FajrSquad.Infrastructure.Services.FakeFcmNotificationSender[0]
      FAKE FCM: Sending notification to token FAKE_TOKEN_FOR_TESTING with messageId 550e8400-e29b-41d4-a716-446655440000
```

### Health Check

Endpoint `/health/notifications` mostra metriche del sistema:

```json
{
  "status": "Healthy",
  "totalSent": 15,
  "totalFailed": 0,
  "pendingCount": 0,
  "lastProcessed": "2025-09-24T10:30:00Z"
}
```

## 🚀 Avvio Rapido

1. **Avvia l'app in Development:**
```bash
cd FajrSquad.API
dotnet run
```

2. **Testa il seed:**
```bash
curl -X POST http://localhost:5000/debug/seed-user
```

3. **Verifica ForceWindow:**
```bash
curl "http://localhost:5000/debug/when" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

4. **Test completo:**
```bash
# Esegui il flusso completo sopra
```

## ⚠️ Note Importanti

- **Solo Development**: Tutti gli endpoint debug sono disabilitati in Production
- **Fake FCM**: In Development usa sempre il fake sender
- **ForceWindow**: Permette test immediati senza aspettare orari specifici
- **Cleanup**: I dati di test vengono creati con prefissi identificabili
- **Sicurezza**: Endpoint protetti da ambiente Development

## 🎯 Checklist di Accettazione

- [ ] ForceWindow on in Development e letta dai job
- [ ] Endpoint `/debug/seed-user`, `/debug/enqueue`, `/debug/push`, `/debug/when`, `/debug/pending`, `/debug/logs` funzionanti
- [ ] Seed crea utente, preferenze ON, device con TZ valida
- [ ] Enqueue + ProcessScheduledNotificationsJob → Processed > 0
- [ ] Push diretto → messageId restituito
- [ ] NotificationLogs popolato
- [ ] Test di integrazione con FCM fake verde
- [ ] README sezione "Test end-to-end adesso"

---

**🎉 Ora puoi testare l'intera pipeline notifiche in pochi secondi!**

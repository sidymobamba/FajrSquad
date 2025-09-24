# Sistema di Notifiche Push - FajrSquad

## Panoramica

Il sistema di notifiche push di FajrSquad è progettato per inviare notifiche personalizzate e localizzate agli utenti su dispositivi Android e iOS. Il sistema utilizza Firebase Cloud Messaging (FCM) per la consegna delle notifiche e Quartz.NET per la programmazione dei job.

## Architettura

### Componenti Principali

1. **FcmNotificationSender**: Servizio per l'invio delle notifiche tramite FCM
2. **NotificationScheduler**: Servizio per la programmazione delle notifiche
3. **MessageBuilder**: Servizio per la costruzione dei messaggi localizzati
4. **Quartz Jobs**: Job per l'elaborazione delle notifiche programmate
5. **Database**: Entità per la gestione delle notifiche e delle preferenze utente

### Flussi di Notifica

#### R1. Promemoria Mattutino
- **Trigger**: Job `MorningReminderJob` ogni 10 minuti
- **Condizione**: Orario locale dell'utente tra 06:30 ± 5 minuti
- **Contenuto**: Adhkar e du'a per la mattina
- **Preferenza**: `UserNotificationPreference.Morning`

#### R2. Promemoria Serale
- **Trigger**: Job `EveningReminderJob` ogni 10 minuti
- **Condizione**: Orario locale dell'utente tra 21:30 ± 5 minuti
- **Contenuto**: Wudu, adhkar sabah & layl
- **Preferenza**: `UserNotificationPreference.Evening`

#### R3. Motivazione Fajr Mancata
- **Trigger**: Job `FajrMissedCheckInJob` alle 08:30
- **Condizione**: Utente non ha fatto check-in per Fajr
- **Contenuto**: Messaggio motivazionale
- **Preferenza**: `UserNotificationPreference.FajrMissed`

#### R3b. Escalation Reminder
- **Trigger**: Job `FajrMissedCheckInJob` alle 08:30
- **Condizione**: Utente ha perso Fajr per 1+ giorni consecutivi
- **Contenuto**: Promemoria aggiuntivo
- **Preferenza**: `UserNotificationPreference.Escalation`

#### R4. Admin Alert
- **Trigger**: Job `FajrMissedCheckInJob` alle 08:30
- **Condizione**: Utente ha perso Fajr per 3+ giorni consecutivi
- **Contenuto**: Allerta per amministratori
- **Preferenza**: Sempre inviata

#### R5. Hadith Quotidiano
- **Trigger**: Job `DailyHadithJob` alle 08:00
- **Condizione**: Una volta al giorno per utente
- **Contenuto**: Hadith casuale
- **Preferenza**: `UserNotificationPreference.HadithDaily`

#### R6. Motivazione Quotidiana
- **Trigger**: Job `DailyMotivationJob` alle 08:05
- **Condizione**: Una volta al giorno per utente
- **Contenuto**: Messaggio motivazionale casuale
- **Preferenza**: `UserNotificationPreference.MotivationDaily`

#### R7. Notifiche Eventi
- **Trigger**: Immediato alla creazione di un evento
- **Condizione**: Nuovo evento creato
- **Contenuto**: Dettagli dell'evento
- **Preferenza**: `UserNotificationPreference.EventsNew`

#### R8. Promemoria Eventi
- **Trigger**: Job `EventReminderSweepJob` ogni 15 minuti
- **Condizione**: 1 giorno prima dell'evento
- **Contenuto**: Promemoria dell'evento
- **Preferenza**: `UserNotificationPreference.EventsReminder`

## Configurazione

### appsettings.json

```json
{
  "Notifications": {
    "UseFakeSender": false,
    "ForceWindow": false,
    "WindowMinutes": 10,
    "MorningTime": "06:30",
    "EveningTime": "21:30",
    "MorningToleranceMinutes": 5,
    "EveningToleranceMinutes": 5,
    "FCM": {
      "ProjectId": "your-firebase-project-id",
      "DefaultTtl": 7200,
      "HighPriorityTtl": 3600,
      "LowPriorityTtl": 14400
    }
  }
}
```

### Variabili d'Ambiente

```bash
# Firebase
FIREBASE_PROJECT_ID=your-project-id
FIREBASE_PRIVATE_KEY_ID=your-private-key-id
FIREBASE_PRIVATE_KEY="-----BEGIN PRIVATE KEY-----\n...\n-----END PRIVATE KEY-----\n"
FIREBASE_CLIENT_EMAIL=firebase-adminsdk-xxx@your-project.iam.gserviceaccount.com
FIREBASE_CLIENT_ID=your-client-id
FIREBASE_AUTH_URI=https://accounts.google.com/o/oauth2/auth
FIREBASE_TOKEN_URI=https://oauth2.googleapis.com/token

# Database
CONNECTION_STRING=Host=localhost;Database=fajrsquad;Username=postgres;Password=password
```

## Entità Database

### ScheduledNotification
```csharp
public class ScheduledNotification : BaseEntity
{
    public int Id { get; set; }
    public Guid? UserId { get; set; }
    public string Type { get; set; } = string.Empty;
    public DateTimeOffset ExecuteAt { get; set; }
    public string DataJson { get; set; } = "{}";
    public string Status { get; set; } = "Pending";
    public string? UniqueKey { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public int Retries { get; set; } = 0;
    public int MaxRetries { get; set; } = 3;
    public DateTimeOffset? NextRetryAt { get; set; }
}
```

### UserNotificationPreference
```csharp
public class UserNotificationPreference : BaseEntity
{
    public Guid UserId { get; set; }
    public bool Morning { get; set; } = true;
    public bool Evening { get; set; } = true;
    public bool FajrMissed { get; set; } = true;
    public bool Escalation { get; set; } = true;
    public bool HadithDaily { get; set; } = true;
    public bool MotivationDaily { get; set; } = true;
    public bool EventsNew { get; set; } = true;
    public bool EventsReminder { get; set; } = true;
    public TimeSpan? QuietHoursStart { get; set; }
    public TimeSpan? QuietHoursEnd { get; set; }
}
```

### DeviceToken
```csharp
public class DeviceToken : BaseEntity
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public string Platform { get; set; } = "Android";
    public string Language { get; set; } = "it";
    public string TimeZone { get; set; } = "Africa/Dakar";
    public string? AppVersion { get; set; }
    public bool IsActive { get; set; } = true;
}
```

## API Endpoints

### Debug Endpoints (Solo Development)

#### POST /debug/seed-user
Crea un utente di test con preferenze e device token.

```bash
curl -X POST http://localhost:5000/debug/seed-user \
  -H "Content-Type: application/json" \
  -d '{"token":"test_token","timeZone":"Africa/Dakar"}'
```

#### POST /debug/enqueue
Programma una notifica di test.

```bash
curl -X POST http://localhost:5000/debug/enqueue \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{"type":"Debug","delaySeconds":60}'
```

#### POST /debug/push
Invia una notifica push diretta.

```bash
curl -X POST http://localhost:5000/debug/push \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

#### GET /debug/when?userId=...
Mostra informazioni sui fusi orari e preferenze.

```bash
curl "http://localhost:5000/debug/when?userId=USER_ID" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

#### GET /debug/pending
Mostra le notifiche in coda.

```bash
curl http://localhost:5000/debug/pending \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

#### GET /debug/logs?userId=...&last=50
Mostra i log delle notifiche.

```bash
curl "http://localhost:5000/debug/logs?userId=USER_ID&last=50" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### Admin Endpoints

#### GET /admin/notifications/health
Stato del sistema di notifiche.

#### GET /admin/notifications/stats
Statistiche delle notifiche.

#### GET /admin/notifications/logs
Log delle notifiche.

#### GET /admin/metrics/by-type
Metriche per tipo di notifica.

#### GET /admin/metrics/top-errors
Errori più comuni.

#### GET /admin/metrics/daily-volume
Volume giornaliero delle notifiche.

## Job Quartz

### MorningReminderJob
- **Cron**: `0 */10 * * * ?` (ogni 10 minuti)
- **Scopo**: Verifica e programma promemoria mattutini

### EveningReminderJob
- **Cron**: `0 */10 * * * ?` (ogni 10 minuti)
- **Scopo**: Verifica e programma promemoria serali

### FajrMissedCheckInJob
- **Cron**: `0 30 8 * * ?` (08:30 quotidiano)
- **Scopo**: Gestisce notifiche per Fajr mancati

### DailyHadithJob
- **Cron**: `0 0 8 * * ?` (08:00 quotidiano)
- **Scopo**: Programma hadith quotidiani

### DailyMotivationJob
- **Cron**: `0 5 8 * * ?` (08:05 quotidiano)
- **Scopo**: Programma motivazioni quotidiane

### EventReminderSweepJob
- **Cron**: `0 */15 * * * ?` (ogni 15 minuti)
- **Scopo**: Verifica e programma promemoria eventi

### ProcessScheduledNotificationsJob
- **Cron**: `0 */5 * * * ?` (ogni 5 minuti)
- **Scopo**: Elabora notifiche programmate

### NotificationCleanupJob
- **Cron**: `0 0 2 * * ?` (02:00 quotidiano)
- **Scopo**: Pulisce log vecchi

## Localizzazione

Il sistema supporta le seguenti lingue:
- **Italiano (it)**: Lingua predefinita
- **Francese (fr)**: Fallback
- **Inglese (en)**: Fallback finale

I template dei messaggi sono definiti in `MessageBuilder` e supportano la formattazione con parametri.

## Gestione Errori

### Retry Logic
- **Tentativi**: 3 per notifica
- **Backoff**: Esponenziale (2^retry minuti)
- **Stati**: Pending → Processing → Succeeded/Failed

### Stati Notifiche
- **Pending**: In attesa di elaborazione
- **Processing**: In corso di elaborazione
- **Succeeded**: Elaborata con successo
- **Failed**: Fallita dopo tutti i tentativi
- **SkippedNoActiveDevice**: Nessun device token attivo
- **SkippedQuietHours**: Durante ore silenziose
- **SkippedUserPreference**: Disabilitata dalle preferenze

## Monitoraggio

### Health Checks
- **Endpoint**: `/health/notifications`
- **Controlli**: Notifiche bloccate, tasso di fallimento, utenti attivi

### Logging
- **Livello**: Information per operazioni normali
- **Livello**: Warning per situazioni anomale
- **Livello**: Error per errori critici

### Metriche
- **Volume**: Notifiche per giorno/tipo
- **Success Rate**: Tasso di successo per tipo
- **Errori**: Top errori e frequenza

## Testing

### Test di Integrazione
```bash
dotnet test FajrSquad.API.IntegrationTests
```

### Test Manuali
1. Usa gli endpoint debug per creare utenti di test
2. Programma notifiche con `/debug/enqueue`
3. Verifica l'elaborazione con `/debug/pending`
4. Controlla i log con `/debug/logs`

## Deployment

### Produzione
1. Imposta `Notifications:UseFakeSender` a `false`
2. Configura le credenziali Firebase
3. Verifica la connessione al database
4. Monitora i log per errori

### Sviluppo
1. Usa `Notifications:UseFakeSender: true`
2. Abilita `Notifications:ForceWindow: true` per test immediati
3. Usa gli endpoint debug per testing

## Troubleshooting

### Problemi Comuni

#### Notifiche non inviate
1. Verifica le credenziali Firebase
2. Controlla i device token attivi
3. Verifica le preferenze utente
4. Controlla i log per errori

#### Timezone non corretto
1. Verifica il formato IANA del timezone
2. Controlla la normalizzazione in `TimezoneService`
3. Verifica le conversioni UTC/local

#### Job non eseguiti
1. Verifica la configurazione Quartz
2. Controlla i log del scheduler
3. Verifica la connessione al database

### Log Importanti
- `MorningCheck user={UserId} nowLocal={NowLocal} target={Target} tolerance={Tolerance}m force={Force} send={ShouldSend}`
- `EveningCheck user={UserId} nowLocal={NowLocal} target={Target} tolerance={Tolerance}m force={Force} send={ShouldSend}`
- `Successfully processed notification {Id} of type {Type}`
- `Failed to process notification {Id} of type {Type} after {Retries} retries: {Error}`

## Sicurezza

### Autenticazione
- Tutti gli endpoint debug richiedono JWT
- Gli endpoint admin richiedono ruolo Admin
- I device token sono associati agli utenti

### Privacy
- Le notifiche rispettano le ore silenziose
- Le preferenze utente sono sempre rispettate
- I log non contengono dati sensibili

### Rate Limiting
- Implementato a livello di FCM
- TTL configurabile per tipo di notifica
- Collapse key per evitare duplicati

## Roadmap

### Funzionalità Future
- [ ] Notifiche in-app
- [ ] Template personalizzabili
- [ ] A/B testing per messaggi
- [ ] Analytics avanzate
- [ ] Supporto per più canali (SMS, Email)

### Miglioramenti
- [ ] Cache per template
- [ ] Batch processing ottimizzato
- [ ] Retry policy configurabile
- [ ] Monitoring avanzato
- [ ] Auto-scaling per picchi di traffico

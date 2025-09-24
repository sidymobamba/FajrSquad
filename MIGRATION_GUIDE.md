# Guida alle Migrazioni - Sistema Notifiche

## Panoramica

Questa guida descrive come applicare le migrazioni del database per il sistema di notifiche di FajrSquad.

## Prerequisiti

1. **PostgreSQL** installato e configurato
2. **.NET 8 SDK** installato
3. **Credenziali database** configurate

## Configurazione Database

### 1. Stringa di Connessione

Aggiungi la stringa di connessione in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=fajrsquad;Username=postgres;Password=password"
  }
}
```

### 2. Variabili d'Ambiente (Raccomandato)

```bash
export CONNECTION_STRING="Host=localhost;Database=fajrsquad;Username=postgres;Password=password"
```

## Applicazione Migrazioni

### 1. Naviga alla Directory del Progetto

```bash
cd FajrSquad.API
```

### 2. Verifica lo Stato delle Migrazioni

```bash
dotnet ef migrations list
```

### 3. Applica le Migrazioni

```bash
dotnet ef database update
```

### 4. Verifica l'Applicazione

```bash
dotnet ef database update --verbose
```

## Nuove Tabelle

### ScheduledNotifications
```sql
CREATE TABLE "ScheduledNotifications" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" UUID,
    "Type" VARCHAR(100) NOT NULL,
    "ExecuteAt" TIMESTAMP WITH TIME ZONE NOT NULL,
    "DataJson" TEXT NOT NULL DEFAULT '{}',
    "Status" VARCHAR(20) NOT NULL DEFAULT 'Pending',
    "UniqueKey" VARCHAR(200),
    "ProcessedAt" TIMESTAMP WITH TIME ZONE,
    "ErrorMessage" VARCHAR(1000),
    "Retries" INTEGER NOT NULL DEFAULT 0,
    "MaxRetries" INTEGER NOT NULL DEFAULT 3,
    "NextRetryAt" TIMESTAMP WITH TIME ZONE,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT FALSE
);
```

### UserNotificationPreferences
```sql
CREATE TABLE "UserNotificationPreferences" (
    "UserId" UUID PRIMARY KEY,
    "Morning" BOOLEAN NOT NULL DEFAULT TRUE,
    "Evening" BOOLEAN NOT NULL DEFAULT TRUE,
    "FajrMissed" BOOLEAN NOT NULL DEFAULT TRUE,
    "Escalation" BOOLEAN NOT NULL DEFAULT TRUE,
    "HadithDaily" BOOLEAN NOT NULL DEFAULT TRUE,
    "MotivationDaily" BOOLEAN NOT NULL DEFAULT TRUE,
    "EventsNew" BOOLEAN NOT NULL DEFAULT TRUE,
    "EventsReminder" BOOLEAN NOT NULL DEFAULT TRUE,
    "QuietHoursStart" TIME,
    "QuietHoursEnd" TIME,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT FALSE
);
```

### DeviceTokens
```sql
CREATE TABLE "DeviceTokens" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" UUID NOT NULL,
    "Token" VARCHAR(512) NOT NULL,
    "Platform" VARCHAR(20) NOT NULL DEFAULT 'Android',
    "Language" VARCHAR(10) NOT NULL DEFAULT 'it',
    "TimeZone" VARCHAR(100) NOT NULL DEFAULT 'Africa/Dakar',
    "AppVersion" VARCHAR(40),
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT FALSE,
    CONSTRAINT "CK_DeviceTokens_TimeZone_Length" CHECK (LENGTH("TimeZone") >= 3 AND "TimeZone" != 'string')
);
```

### NotificationLogs
```sql
CREATE TABLE "NotificationLogs" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" UUID,
    "Type" VARCHAR(100) NOT NULL,
    "Result" VARCHAR(50) NOT NULL,
    "Error" TEXT,
    "ProviderMessageId" VARCHAR(200),
    "PayloadJson" TEXT,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);
```

## Indici

### Indici per Performance
```sql
-- ScheduledNotifications
CREATE INDEX "IX_ScheduledNotifications_Status_ExecuteAt" ON "ScheduledNotifications" ("Status", "ExecuteAt");
CREATE INDEX "IX_ScheduledNotifications_Status_NextRetryAt" ON "ScheduledNotifications" ("Status", "NextRetryAt") WHERE "NextRetryAt" IS NOT NULL;
CREATE INDEX "IX_ScheduledNotifications_UserId_Type_Status" ON "ScheduledNotifications" ("UserId", "Type", "Status");

-- DeviceTokens
CREATE UNIQUE INDEX "IX_DeviceTokens_UserId_Token" ON "DeviceTokens" ("UserId", "Token");
CREATE INDEX "IX_DeviceTokens_UserId" ON "DeviceTokens" ("UserId");
CREATE INDEX "IX_DeviceTokens_IsActive" ON "DeviceTokens" ("IsActive");

-- UserNotificationPreferences
CREATE UNIQUE INDEX "IX_UserNotificationPreferences_UserId" ON "UserNotificationPreferences" ("UserId");

-- NotificationLogs
CREATE INDEX "IX_NotificationLogs_UserId_CreatedAt" ON "NotificationLogs" ("UserId", "CreatedAt");
CREATE INDEX "IX_NotificationLogs_Type_CreatedAt" ON "NotificationLogs" ("Type", "CreatedAt");
```

## Rollback

### 1. Rollback a una Migrazione Specifica

```bash
dotnet ef database update <MigrationName>
```

### 2. Rollback Completo

```bash
dotnet ef database update 0
```

### 3. Rimozione Migrazione

```bash
dotnet ef migrations remove
```

## Verifica Post-Migrazione

### 1. Controlla le Tabelle

```sql
-- Verifica che le tabelle esistano
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public' 
AND table_name IN ('ScheduledNotifications', 'UserNotificationPreferences', 'DeviceTokens', 'NotificationLogs');
```

### 2. Verifica gli Indici

```sql
-- Verifica che gli indici esistano
SELECT indexname, tablename 
FROM pg_indexes 
WHERE tablename IN ('ScheduledNotifications', 'UserNotificationPreferences', 'DeviceTokens', 'NotificationLogs');
```

### 3. Test di Connessione

```bash
# Testa la connessione al database
dotnet ef database update --dry-run
```

## Problemi Comuni

### 1. Errore di Connessione

**Problema**: `Unable to connect to database`

**Soluzione**:
- Verifica che PostgreSQL sia in esecuzione
- Controlla la stringa di connessione
- Verifica le credenziali

### 2. Permessi Insufficienti

**Problema**: `Permission denied`

**Soluzione**:
- Assicurati che l'utente abbia i permessi per creare tabelle
- Verifica i permessi sul database

### 3. Migrazione Fallita

**Problema**: `Migration failed`

**Soluzione**:
- Controlla i log per errori specifici
- Verifica che non ci siano conflitti di schema
- Considera un rollback e riprova

### 4. Timeout

**Problema**: `Command timeout`

**Soluzione**:
- Aumenta il timeout della connessione
- Verifica le performance del database
- Considera l'esecuzione in batch per grandi dataset

## Backup e Ripristino

### 1. Backup Pre-Migrazione

```bash
# Backup completo del database
pg_dump -h localhost -U postgres -d fajrsquad > backup_pre_migration.sql
```

### 2. Ripristino

```bash
# Ripristina il backup
psql -h localhost -U postgres -d fajrsquad < backup_pre_migration.sql
```

## Monitoraggio

### 1. Verifica Stato Migrazioni

```sql
-- Controlla lo stato delle migrazioni
SELECT * FROM "__EFMigrationsHistory" ORDER BY "MigrationId";
```

### 2. Monitoraggio Performance

```sql
-- Verifica le dimensioni delle tabelle
SELECT 
    schemaname,
    tablename,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) as size
FROM pg_tables 
WHERE schemaname = 'public' 
AND tablename IN ('ScheduledNotifications', 'UserNotificationPreferences', 'DeviceTokens', 'NotificationLogs');
```

## Best Practices

### 1. Prima della Migrazione
- Fai sempre un backup del database
- Testa le migrazioni in un ambiente di sviluppo
- Verifica che tutti i servizi siano fermati

### 2. Durante la Migrazione
- Monitora i log per errori
- Non interrompere il processo
- Verifica lo spazio su disco

### 3. Dopo la Migrazione
- Testa l'applicazione
- Verifica le performance
- Monitora i log per errori

## Supporto

Per problemi con le migrazioni:

1. Controlla i log dell'applicazione
2. Verifica la documentazione EF Core
3. Consulta i log del database PostgreSQL
4. Contatta il team di sviluppo

## Riferimenti

- [EF Core Migrations](https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- [FajrSquad Notification System](NOTIFICATION_SYSTEM_README.md)

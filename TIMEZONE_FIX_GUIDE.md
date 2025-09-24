# 🔧 Guida per Risolvere l'Errore Timezone

## 🚨 Problema Identificato

L'errore che stai vedendo:
```
System.TimeZoneNotFoundException: The time zone ID 'string' was not found on the local computer.
```

**Causa**: Il valore `TimeZone` nel database è impostato come la stringa letterale `'string'` invece del valore effettivo del timezone (es. `'Africa/Dakar'`, `'Europe/Rome'`, etc.).

## ✅ Soluzioni Implementate

### 1. **Gestione Errori Robusta nei Job**
Ho aggiunto una gestione degli errori nei job `MorningReminderJob` e `EveningReminderJob` che:
- ✅ Rileva timezone invalidi (`'string'`, vuoti, o troppo corti)
- ✅ Salta gli utenti con timezone invalidi invece di crashare
- ✅ Usa `Africa/Dakar` come fallback per timezone non trovati
- ✅ Logga warning dettagliati per debugging

### 2. **Endpoint Admin per Sistemare i Dati**
Ho creato nuovi endpoint in `/api/admin/`:

#### **GET** `/api/admin/timezone-stats`
Mostra statistiche sui timezone nel database:
```json
{
  "timezoneStats": [
    { "timezone": "Africa/Dakar", "count": 5 },
    { "timezone": "string", "count": 3 },
    { "timezone": "Europe/Rome", "count": 2 }
  ],
  "invalidTimezoneCount": 3,
  "totalDeviceTokens": 10
}
```

#### **POST** `/api/admin/fix-timezone-data`
Sistema automaticamente tutti i timezone invalidi basandosi sul paese dell'utente:
- 🇮🇹 Italy → `Europe/Rome`
- 🇫🇷 France → `Europe/Paris`
- 🇸🇳 Senegal → `Africa/Dakar`
- 🇲🇦 Morocco → `Africa/Casablanca`
- etc.

#### **POST** `/api/admin/test-timezone`
Testa il timezone del tuo utente corrente per verificare se è valido.

## 🚀 Come Risolvere

### Opzione 1: Usa l'Endpoint Admin (Raccomandato)
1. **Fai login** nell'app
2. **Chiama** `POST /api/admin/fix-timezone-data` per sistemare tutti i timezone invalidi
3. **Verifica** con `GET /api/admin/timezone-stats` che i timezone siano stati sistemati

### Opzione 2: Fix Manuale nel Database
Se preferisci sistemare manualmente:

```sql
-- Aggiorna tutti i timezone invalidi
UPDATE "DeviceTokens" 
SET "TimeZone" = 'Africa/Dakar', "UpdatedAt" = CURRENT_TIMESTAMP
WHERE "TimeZone" = 'string' OR "TimeZone" IS NULL OR LENGTH("TimeZone") < 3;
```

### Opzione 3: Fix per Utente Specifico
Se vuoi sistemare solo il tuo utente:

```sql
-- Sostituisci 'YOUR_USER_ID' con il tuo ID utente
UPDATE "DeviceTokens" 
SET "TimeZone" = 'Africa/Dakar', "UpdatedAt" = CURRENT_TIMESTAMP
WHERE "UserId" = 'YOUR_USER_ID' AND ("TimeZone" = 'string' OR "TimeZone" IS NULL);
```

## 🔍 Verifica della Soluzione

Dopo aver applicato la fix:

1. **Controlla i log** - Non dovresti più vedere errori `TimeZoneNotFoundException`
2. **Testa l'endpoint** - `POST /api/admin/test-timezone` dovrebbe restituire `"isValid": true`
3. **Verifica le statistiche** - `GET /api/admin/timezone-stats` dovrebbe mostrare `"invalidTimezoneCount": 0`

## 📝 Note Tecniche

- **Root Cause**: Il problema si è verificato durante la migrazione del database quando i valori di default non sono stati impostati correttamente
- **Prevenzione**: I nuovi job hanno una gestione degli errori robusta per evitare crash futuri
- **Fallback**: Se un timezone non è valido, il sistema usa `Africa/Dakar` come default
- **Logging**: Tutti i timezone invalidi vengono loggati per debugging

## 🎯 Risultato Atteso

Dopo la fix:
- ✅ I job di notifica funzioneranno senza errori
- ✅ Gli utenti riceveranno notifiche negli orari corretti
- ✅ Il sistema sarà più robusto contro timezone invalidi
- ✅ I log saranno puliti e informativi

---

**💡 Suggerimento**: Usa l'endpoint `POST /api/admin/fix-timezone-data` per una soluzione rapida e automatica!

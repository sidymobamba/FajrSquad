# 🎯 Prayer Times GPS-First Implementation - Complete

## ✅ Implementazioni Completate

### 1. **Retry & Circuit Breaker (Polly)**
- ✅ Retry con exponential backoff (2 tentativi: 2s, 4s)
- ✅ Circuit breaker (3 errori → 30s di break)
- ✅ Applicato su HttpClient "PrayerTimes" per AlAdhan API

**File**: `FajrSquad.API/Program.cs`
```csharp
builder.Services.AddHttpClient("PrayerTimes", client => { ... })
    .AddPolicyHandler(Polly.Policy
        .HandleResult<HttpResponseMessage>(r => (int)r.StatusCode >= 500 || ...)
        .WaitAndRetryAsync(retryCount: 2, ...))
    .AddPolicyHandler(Polly.Policy
        .HandleResult<HttpResponseMessage>(r => (int)r.StatusCode >= 500)
        .CircuitBreakerAsync(...));
```

### 2. **Cache Strategy**
- ✅ **Prayer Times**: 30 minuti (today/week)
- ✅ **Reverse Geocoding**: 24 ore (già implementato)

**File**: `FajrSquad.Infrastructure/Services/PrayerTimes/PrayerTimesService.cs`
```csharp
private const int CacheMinutes = 30; // Cache prayer times for 30 minutes
```

### 3. **Guard-Rails (Graceful Degradation)**
- ✅ Se AlAdhan down → ritorna **200 OK** con:
  - `source: "coords"` ✅
  - `location` pieno (city, country, timezone) ✅
  - `prayers: null` ✅
  - `error: "UPSTREAM_UNAVAILABLE"` ✅

**File**: `FajrSquad.API/Controllers/PrayerTimesController.cs`
```csharp
if (raw == null)
{
    var gracefulResp = new PrayerTimesResponse(
        Source: "coords",
        Location: new LocationDto(...), // Location sempre popolato
        Prayers: null, // Upstream unavailable
        Error: "UPSTREAM_UNAVAILABLE");
    return Ok(gracefulResp); // 200, non 502
}
```

### 4. **Timezone Normalization**
- ✅ **Sempre normalizzato a IANA** prima di salvare in cache
- ✅ **Country-based normalization**: Italy → Europe/Rome (non Europe/Berlin)
- ✅ Applicato sia per `today` che per `week` (tutti i giorni)

**File**: `FajrSquad.Infrastructure/Services/PrayerTimes/TzHelper.cs`
```csharp
public static string NormalizeTimezoneByCountry(string? timezone, string? country)
{
    if (country?.Equals("Italy", StringComparison.OrdinalIgnoreCase) == true)
        return "Europe/Rome";
    // ... altri paesi
    return ToIana(timezone);
}
```

### 5. **Test d'Integrazione**
- ✅ Test per `today` con coords valide → verifica city, country, timezone non vuoti
- ✅ Test per `week` → verifica `days.length === days richiesti` e tutte le timezone = Europe/Rome
- ✅ Test per graceful degradation (AlAdhan down)

**File**: `FajrSquad.Tests/PrayerTimesIntegrationTests.cs`

---

## 📱 Frontend: Uso Semplice (Capacitor)

### Installazione
```bash
npm install @capacitor/geolocation
```

### Implementazione

```typescript
import { Geolocation } from '@capacitor/geolocation';

// 1. Leggi la posizione (solo lat/lng, niente reverse sul client)
const getCurrentPosition = async () => {
  try {
    const pos = await Geolocation.getCurrentPosition({
      enableHighAccuracy: true,
      timeout: 10000
    });
    
    return {
      lat: pos.coords.latitude,
      lng: pos.coords.longitude
    };
  } catch (error) {
    console.error('Geolocation error:', error);
    return null;
  }
};

// 2. Chiama il backend (solo lat/lng, il backend fa tutto)
const fetchPrayerTimes = async (lat: number, lng: number) => {
  const today = await api.get(`/api/PrayerTimes/today`, {
    params: {
      latitude: lat,
      longitude: lng,
      method: 3,
      school: 0
    }
  });
  
  const week = await api.get(`/api/PrayerTimes/week`, {
    params: {
      latitude: lat,
      longitude: lng,
      method: 3,
      school: 0,
      offset: 0,
      days: 7
    }
  });
  
  return { today: today.data, week: week.data };
};

// 3. UI: mostra location e gestisci errori
const PrayerTimesScreen = () => {
  const [prayerData, setPrayerData] = useState(null);
  const [location, setLocation] = useState(null);
  
  useEffect(() => {
    const loadData = async () => {
      const coords = await getCurrentPosition();
      if (!coords) return;
      
      const data = await fetchPrayerTimes(coords.lat, coords.lng);
      setPrayerData(data);
      
      // Location sempre disponibile (anche se prayers è null)
      const label = `${data.today.location.city}, ${data.today.location.country}`;
      setLocation(label);
    };
    
    loadData();
  }, []);
  
  // Gestisci UPSTREAM_UNAVAILABLE
  if (prayerData?.today?.error === 'UPSTREAM_UNAVAILABLE') {
    return (
      <View>
        <Text>{location}</Text>
        <Banner>
          Servizio orari temporaneamente non disponibile
        </Banner>
      </View>
    );
  }
  
  return (
    <View>
      <Text>{location}</Text>
      <PrayerTimesList prayers={prayerData?.today?.prayers} />
    </View>
  );
};
```

---

## 🔧 UX Micro-Regole (Frontend)

### 1. **Cache Client (5 minuti)**
```typescript
const CACHE_TTL = 5 * 60 * 1000; // 5 minuti
let cachedData = null;
let cacheTimestamp = 0;

const getCachedOrFetch = async (lat: number, lng: number) => {
  const now = Date.now();
  if (cachedData && (now - cacheTimestamp) < CACHE_TTL) {
    return cachedData; // Usa cache
  }
  
  const data = await fetchPrayerTimes(lat, lng);
  cachedData = data;
  cacheTimestamp = now;
  return data;
};
```

### 2. **Throttle Geolocalizzazione (max 1 ogni 5 minuti)**
```typescript
let lastGeolocationTime = 0;
let lastCoords = null;
const GEOLOCATION_THROTTLE = 5 * 60 * 1000; // 5 minuti
const DISTANCE_THRESHOLD = 5000; // 5 km

const shouldRefreshLocation = (newCoords: { lat: number, lng: number }) => {
  const now = Date.now();
  
  // Throttle: max 1 ogni 5 minuti
  if (now - lastGeolocationTime < GEOLOCATION_THROTTLE) {
    return false;
  }
  
  // Refresh se distanza > 5 km
  if (lastCoords) {
    const distance = calculateDistance(lastCoords, newCoords);
    if (distance < DISTANCE_THRESHOLD) {
      return false;
    }
  }
  
  lastGeolocationTime = now;
  lastCoords = newCoords;
  return true;
};
```

### 3. **Gestione Errori**
```typescript
// Se prayers === null ⇒ mostra banner, ma mantieni city/country/timezone
if (response.prayers === null) {
  showBanner('Servizio orari temporaneamente non disponibile');
  // Location è sempre disponibile
  displayLocation(response.location.city, response.location.country);
}
```

---

## ✅ Checklist Finale

### Backend
- [x] Retry & circuit breaker su AlAdhan (Polly)
- [x] Cache: 30' per today/week, 24h per reverse
- [x] Guard-rails: 200 con location pieno e prayers:null + error
- [x] Normalizza sempre timezone a IANA prima di cache
- [x] Test d'integrazione: coords valide → city/country/timezone non vuoti
- [x] Test d'integrazione: week → days.length === days richiesti e timezone = Europe/Rome

### Frontend (da implementare)
- [ ] Cache client 5' (solo per evitare flicker)
- [ ] Throttle: max 1 geolocalizzazione ogni 5'
- [ ] Refresh se distanza > 5 km
- [ ] Banner se prayers === null

---

## 🎯 Risultato Atteso

### Response Today (Success)
```json
{
  "source": "coords",
  "location": {
    "city": "Brescia",
    "country": "Italy",
    "timezone": "Europe/Rome"
  },
  "coords": {
    "lat": 45.5416,
    "lng": 10.2118,
    "precision": "p4"
  },
  "prayers": {
    "Fajr": "05:30",
    "Sunrise": "07:15",
    ...
  },
  "nextPrayerName": "Dhuhr",
  "nextPrayerTime": "12:30"
}
```

### Response Today (AlAdhan Down)
```json
{
  "source": "coords",
  "location": {
    "city": "Brescia",
    "country": "Italy",
    "timezone": "Europe/Rome"
  },
  "coords": {
    "lat": 45.5416,
    "lng": 10.2118,
    "precision": "p4"
  },
  "prayers": null,
  "error": "UPSTREAM_UNAVAILABLE"
}
```

---

## 🚀 Prossimi Passi

1. **Testare** i guard-rails simulando AlAdhan down
2. **Implementare** cache e throttle sul frontend
3. **Monitorare** circuit breaker e retry nei log
4. **Aggiungere** metriche per uptime AlAdhan


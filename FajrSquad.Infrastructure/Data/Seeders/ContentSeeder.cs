using FajrSquad.Core.Entities;
using FajrSquad.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FajrSquad.Infrastructure.Data.Seeders
{
    public class ContentSeeder
    {
        private readonly FajrDbContext _context;
        private readonly ILogger<ContentSeeder> _logger;

        public ContentSeeder(FajrDbContext context, ILogger<ContentSeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            await SeedHadithsAsync();
            await SeedMotivationsAsync();
            await SeedAdhkarAsync();
            await SeedEventsAsync();
        }

        private async Task SeedHadithsAsync()
        {
            if (await _context.Hadiths.AnyAsync())
            {
                _logger.LogInformation("Hadiths already seeded, skipping");
                return;
            }

            var hadiths = new List<Hadith>
            {
                new Hadith
                {
                    Id = Guid.NewGuid(),
                    Text = "Chi prega Fajr in congregazione è come se avesse pregato tutta la notte",
                    TextArabic = "مَنْ صَلَّى الصُّبْحَ فِي جَمَاعَةٍ فَكَأَنَّمَا صَلَّى اللَّيْلَ كُلَّهُ",
                    Source = "Sahih Muslim",
                    Category = "Prayer",
                    Theme = "Prayer",
                    IsActive = true,
                    Priority = 1,
                    Language = "it"
                },
                new Hadith
                {
                    Id = Guid.NewGuid(),
                    Text = "In Paradiso c'è una porta chiamata Rayyan, attraverso la quale entreranno coloro che digiunano nel Giorno del Giudizio",
                    TextArabic = "إِنَّ فِي الْجَنَّةِ بَابًا يُقَالُ لَهُ الرَّيَّانُ، يَدْخُلُ مِنْهُ الصَّائِمُونَ يَوْمَ الْقِيَامَةِ",
                    Source = "Sahih Bukhari",
                    Category = "Fasting",
                    Theme = "Fasting",
                    IsActive = true,
                    Priority = 2,
                    Language = "it"
                },
                new Hadith
                {
                    Id = Guid.NewGuid(),
                    Text = "Chi crede in Allah e nel Giorno del Giudizio, dica il bene o taccia",
                    TextArabic = "مَنْ كَانَ يُؤْمِنُ بِاللَّهِ وَالْيَوْمِ الْآخِرِ فَلْيَقُلْ خَيْرًا أَوْ لِيَصْمُتْ",
                    Source = "Sahih Bukhari",
                    Category = "Speech",
                    Theme = "Speech",
                    IsActive = true,
                    Priority = 3,
                    Language = "it"
                },
                new Hadith
                {
                    Id = Guid.NewGuid(),
                    Text = "La modestia è un ramo della fede",
                    TextArabic = "الْحَيَاءُ شُعْبَةٌ مِنَ الْإِيمَانِ",
                    Source = "Sahih Bukhari",
                    Category = "Character",
                    Theme = "Character",
                    IsActive = true,
                    Priority = 4,
                    Language = "it"
                },
                new Hadith
                {
                    Id = Guid.NewGuid(),
                    Text = "Chi non ha misericordia per le persone, Allah non avrà misericordia per lui",
                    TextArabic = "مَنْ لَا يَرْحَمُ النَّاسَ لَا يَرْحَمُهُ اللَّهُ",
                    Source = "Sahih Bukhari",
                    Category = "Mercy",
                    Theme = "Mercy",
                    IsActive = true,
                    Priority = 5,
                    Language = "it"
                }
            };

            _context.Hadiths.AddRange(hadiths);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} hadiths", hadiths.Count);
        }

        private async Task SeedMotivationsAsync()
        {
            if (await _context.Motivations.AnyAsync())
            {
                _logger.LogInformation("Motivations already seeded, skipping");
                return;
            }

            var motivations = new List<Motivation>
            {
                new Motivation
                {
                    Id = Guid.NewGuid(),
                    Text = "Inizia la tua giornata con Fajr e Allah ti benedirà in tutto ciò che fai",
                    Type = "fajr",
                    Theme = "Morning",
                    IsActive = true,
                    Priority = 1,
                    Language = "it"
                },
                new Motivation
                {
                    Id = Guid.NewGuid(),
                    Text = "Il Profeta (SAW) disse: 'La preghiera è la luce del credente' - lascia che Fajr illumini la tua giornata",
                    Type = "fajr",
                    Theme = "Morning",
                    IsActive = true,
                    Priority = 2,
                    Language = "it"
                },
                new Motivation
                {
                    Id = Guid.NewGuid(),
                    Text = "Ogni nuovo giorno è un'opportunità per avvicinarti ad Allah. Inizia con Fajr!",
                    Type = "fajr",
                    Theme = "Morning",
                    IsActive = true,
                    Priority = 3,
                    Language = "it"
                },
                new Motivation
                {
                    Id = Guid.NewGuid(),
                    Text = "Non perdere il tuo Fajr! È la preghiera più preziosa della giornata",
                    Type = "fajr",
                    Theme = "Late",
                    IsActive = true,
                    Priority = 1,
                    Language = "it"
                },
                new Motivation
                {
                    Id = Guid.NewGuid(),
                    Text = "Allah ti ama e vuole il meglio per te. Non dimenticare di pregare Fajr!",
                    Type = "fajr",
                    Theme = "Late",
                    IsActive = true,
                    Priority = 2,
                    Language = "it"
                },
                new Motivation
                {
                    Id = Guid.NewGuid(),
                    Text = "La pazienza e la perseveranza sono le chiavi del successo. Continua a pregare Fajr!",
                    Type = "general",
                    Theme = "Motivation",
                    IsActive = true,
                    Priority = 1,
                    Language = "it"
                },
                new Motivation
                {
                    Id = Guid.NewGuid(),
                    Text = "Ricorda: ogni sforzo per avvicinarti ad Allah viene ricompensato",
                    Type = "general",
                    Theme = "Motivation",
                    IsActive = true,
                    Priority = 2,
                    Language = "it"
                }
            };

            _context.Motivations.AddRange(motivations);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} motivations", motivations.Count);
        }

        private async Task SeedAdhkarAsync()
        {
            if (await _context.Reminders.AnyAsync())
            {
                _logger.LogInformation("Adhkar already seeded, skipping");
                return;
            }

            var adhkar = new List<Reminder>
            {
                new Reminder
                {
                    Id = Guid.NewGuid(),
                    Title = "Adhkar del Mattino",
                    Message = "Gloria ad Allah e lode a Lui, Gloria ad Allah l'Altissimo\nسُبْحَانَ اللَّهِ وَبِحَمْدِهِ سُبْحَانَ اللَّهِ الْعَظِيمِ",
                    Category = "Morning",
                    Type = "Dhikr",
                    IsActive = true,
                    Priority = 1,
                    Language = "it"
                },
                new Reminder
                {
                    Id = Guid.NewGuid(),
                    Title = "Adhkar della Sera",
                    Message = "Non c'è divinità eccetto Allah, l'Unico, senza partner. A Lui appartiene la sovranità e a Lui la lode, ed Egli è onnipotente\nلَا إِلَهَ إِلَّا اللَّهُ وَحْدَهُ لَا شَرِيكَ لَهُ، لَهُ الْمُلْكُ وَلَهُ الْحَمْدُ وَهُوَ عَلَى كُلِّ شَيْءٍ قَدِيرٌ",
                    Category = "Evening",
                    Type = "Dhikr",
                    IsActive = true,
                    Priority = 1,
                    Language = "it"
                },
                new Reminder
                {
                    Id = Guid.NewGuid(),
                    Title = "Dua Prima di Uscire",
                    Message = "Nel nome di Allah, mi affido ad Allah, e non c'è potere né forza se non con Allah\nبِسْمِ اللَّهِ تَوَكَّلْتُ عَلَى اللَّهِ وَلَا حَوْلَ وَلَا قُوَّةَ إِلَّا بِاللَّهِ",
                    Category = "Morning",
                    Type = "Dua",
                    IsActive = true,
                    Priority = 2,
                    Language = "it"
                },
                new Reminder
                {
                    Id = Guid.NewGuid(),
                    Title = "Dua Prima di Dormire",
                    Message = "Nel Tuo nome, o mio Signore, depongo il mio fianco, e con Te lo sollevo. Se trattieni la mia anima, abbi misericordia di essa, e se la lasci andare, proteggila come proteggi i Tuoi servi giusti\nبِاسْمِكَ رَبِّي وَضَعْتُ جَنْبِي، وَبِكَ أَرْفَعُهُ، فَإِنْ أَمْسَكْتَ نَفْسِي فَارْحَمْهَا، وَإِنْ أَرْسَلْتَهَا فَاحْفَظْهَا بِمَا تَحْفَظُ بِهِ عِبَادَكَ الصَّالِحِينَ",
                    Category = "Evening",
                    Type = "Dua",
                    IsActive = true,
                    Priority = 2,
                    Language = "it"
                },
                new Reminder
                {
                    Id = Guid.NewGuid(),
                    Title = "Dua per il Wudu",
                    Message = "Testimonio che non c'è divinità eccetto Allah, l'Unico, senza partner, e testimonio che Muhammad è Suo servo e messaggero\nأَشْهَدُ أَنْ لَا إِلَهَ إِلَّا اللَّهُ وَحْدَهُ لَا شَرِيكَ لَهُ، وَأَشْهَدُ أَنَّ مُحَمَّدًا عَبْدُهُ وَرَسُولُهُ",
                    Category = "Evening",
                    Type = "Dua",
                    IsActive = true,
                    Priority = 3,
                    Language = "it"
                }
            };

            _context.Reminders.AddRange(adhkar);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} adhkar", adhkar.Count);
        }

        private async Task SeedEventsAsync()
        {
            if (await _context.Events.AnyAsync())
            {
                _logger.LogInformation("Events already seeded, skipping");
                return;
            }

            var events = new List<Event>
            {
                new Event
                {
                    Id = Guid.NewGuid(),
                    Title = "Lezione di Corano - Surah Al-Fatiha",
                    Description = "Lezione settimanale di recitazione e comprensione del Corano",
                    Location = "Centro Islamico di Dakar",
                    StartDate = DateTime.UtcNow.AddDays(7).AddHours(19), // Next week at 7 PM
                    EndDate = DateTime.UtcNow.AddDays(7).AddHours(21),
                    Organizer = "Centro Islamico di Dakar",
                    IsActive = true
                },
                new Event
                {
                    Id = Guid.NewGuid(),
                    Title = "Webinar: I Pilastri dell'Islam",
                    Description = "Seminario online sui cinque pilastri dell'Islam",
                    Location = "Online",
                    StartDate = DateTime.UtcNow.AddDays(14).AddHours(20), // In 2 weeks at 8 PM
                    EndDate = DateTime.UtcNow.AddDays(14).AddHours(22),
                    Organizer = "FajrSquad",
                    IsActive = true
                },
                new Event
                {
                    Id = Guid.NewGuid(),
                    Title = "Giornata di Pulizia della Moschea",
                    Description = "Attività di volontariato per mantenere pulita la moschea",
                    Location = "Grande Moschea di Dakar",
                    StartDate = DateTime.UtcNow.AddDays(3).AddHours(9), // In 3 days at 9 AM
                    EndDate = DateTime.UtcNow.AddDays(3).AddHours(12),
                    Organizer = "Grande Moschea di Dakar",
                    IsActive = true
                }
            };

            _context.Events.AddRange(events);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} events", events.Count);
        }
    }
}

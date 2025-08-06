using FajrSquad.Core.Entities;
using FajrSquad.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FajrSquad.Infrastructure.Data
{
    public static class IslamicDataSeeder
    {
        public static async Task SeedAsync(FajrDbContext context)
        {
            // Seed Hadiths
            await SeedHadithsAsync(context);
            
            // Seed Motivations
            await SeedMotivationsAsync(context);
            
            // Seed Islamic Reminders
            await SeedRemindersAsync(context);
            
            await context.SaveChangesAsync();
        }

        private static async Task SeedHadithsAsync(FajrDbContext context)
        {
            if (await context.Hadiths.AnyAsync()) return;

            var hadiths = new List<Hadith>
            {
                new Hadith
                {
                    Text = "Celui qui prie le Fajr est sous la protection d'Allah.",
                    TextArabic = "مَنْ صَلَّى الْفَجْرَ فَهُوَ فِي ذِمَّةِ اللَّهِ",
                    Source = "Muslim",
                    Category = "morning",
                    Theme = "fajr",
                    Priority = 1,
                    Language = "fr"
                },
                new Hadith
                {
                    Text = "La prière du Fajr vaut mieux que ce monde et tout ce qu'il contient.",
                    TextArabic = "رَكْعَتَا الْفَجْرِ خَيْرٌ مِنَ الدُّنْيَا وَمَا فِيهَا",
                    Source = "Muslim",
                    Category = "morning",
                    Theme = "fajr",
                    Priority = 1,
                    Language = "fr"
                },
                new Hadith
                {
                    Text = "Celui qui accomplit la prière de l'aube (Fajr) en groupe, c'est comme s'il avait prié toute la nuit.",
                    TextArabic = "مَنْ صَلَّى الْفَجْرَ فِي جَمَاعَةٍ فَكَأَنَّمَا صَلَّى اللَّيْلَ كُلَّهُ",
                    Source = "Muslim",
                    Category = "morning",
                    Theme = "congregation",
                    Priority = 1,
                    Language = "fr"
                },
                new Hadith
                {
                    Text = "Invoquez beaucoup Allah au lever et au coucher du soleil.",
                    TextArabic = "أَكْثِرُوا مِنْ ذِكْرِ اللَّهِ عِنْدَ طُلُوعِ الشَّمْسِ وَعِنْدَ غُرُوبِهَا",
                    Source = "Abu Dawud",
                    Category = "evening",
                    Theme = "dhikr",
                    Priority = 2,
                    Language = "fr"
                }
            };

            context.Hadiths.AddRange(hadiths);
        }

        private static async Task SeedMotivationsAsync(FajrDbContext context)
        {
            if (await context.Motivations.AnyAsync()) return;

            var motivations = new List<Motivation>
            {
                new Motivation
                {
                    Text = "Chaque Fajr est une nouvelle chance de se rapprocher d'Allah. Ne la manque pas !",
                    Type = "fajr",
                    Theme = "encouragement",
                    Priority = 1,
                    Language = "fr"
                },
                new Motivation
                {
                    Text = "La nuit est faite pour le repos, mais l'âme trouve sa paix dans la prière du Fajr.",
                    Type = "night",
                    Theme = "spiritual",
                    Priority = 1,
                    Language = "fr"
                },
                new Motivation
                {
                    Text = "Avant de dormir, rappelle-toi d'Allah. Il veillera sur ton sommeil.",
                    Type = "night",
                    Theme = "sleep",
                    Priority = 1,
                    Language = "fr"
                },
                new Motivation
                {
                    Text = "Le matin commence par la prière, la journée se termine par la gratitude.",
                    Type = "morning",
                    Theme = "gratitude",
                    Priority = 2,
                    Language = "fr"
                }
            };

            context.Motivations.AddRange(motivations);
        }

        private static async Task SeedRemindersAsync(FajrDbContext context)
        {
            if (await context.Reminders.AnyAsync()) return;

            var currentYear = DateTime.UtcNow.Year;
            var reminders = new List<Reminder>
            {
                // Sleep Reminders
                new Reminder
                {
                    Title = "Préparation au sommeil",
                    Message = "Récite Ayat al-Kursi, les 3 dernières sourates du Coran, et fais tes invocations avant de dormir.",
                    Type = "sleep",
                    Category = "pre_sleep",
                    ScheduledTime = new TimeSpan(22, 0, 0),
                    IsRecurring = true,
                    RecurrencePattern = "daily",
                    Priority = 1,
                    Language = "fr"
                },
                new Reminder
                {
                    Title = "Ablutions avant le sommeil",
                    Message = "Il est recommandé de faire ses ablutions avant de se coucher, comme le faisait le Prophète ﷺ.",
                    Type = "sleep",
                    Category = "pre_sleep",
                    ScheduledTime = new TimeSpan(22, 30, 0),
                    IsRecurring = true,
                    RecurrencePattern = "daily",
                    Priority = 2,
                    Language = "fr"
                },

                // Fajr Reminders
                new Reminder
                {
                    Title = "Réveil pour Fajr",
                    Message = "Il est temps de se lever pour la prière du Fajr. Qu'Allah bénisse ton réveil !",
                    Type = "fajr",
                    Category = "fajr_reminder",
                    ScheduledTime = new TimeSpan(4, 30, 0),
                    IsRecurring = true,
                    RecurrencePattern = "daily",
                    Priority = 1,
                    Language = "fr"
                },

                // Fasting Reminders
                new Reminder
                {
                    Title = "Jeûne du lundi",
                    Message = "Le Prophète ﷺ jeûnait le lundi et le jeudi. C'est un jeûne recommandé.",
                    Type = "fasting",
                    Category = "sunnah_fasting",
                    IsRecurring = true,
                    RecurrencePattern = "weekly",
                    Priority = 2,
                    Language = "fr"
                },
                new Reminder
                {
                    Title = "Jeûne du jeudi",
                    Message = "Le Prophète ﷺ jeûnait le lundi et le jeudi. C'est un jeûne recommandé.",
                    Type = "fasting",
                    Category = "sunnah_fasting",
                    IsRecurring = true,
                    RecurrencePattern = "weekly",
                    Priority = 2,
                    Language = "fr"
                },
                new Reminder
                {
                    Title = "Jeûne des jours blancs",
                    Message = "Les 13, 14 et 15 de chaque mois lunaire sont des jours recommandés pour jeûner.",
                    Type = "fasting",
                    Category = "white_days",
                    IsRecurring = true,
                    RecurrencePattern = "monthly",
                    Priority = 2,
                    Language = "fr"
                },

                // Islamic Holidays 2025 (approximatives - à ajuster selon le calendrier lunaire)
                new Reminder
                {
                    Title = "Ramadan 2025",
                    Message = "Le mois béni de Ramadan commence. Qu'Allah nous accorde la force de bien le vivre !",
                    Type = "islamic_holiday",
                    Category = "ramadan",
                    ScheduledDate = new DateTime(currentYear, 3, 1), // Approximatif
                    HijriDate = "1 Ramadan 1446",
                    IsHijriCalendar = true,
                    Priority = 1,
                    Language = "fr",
                    AdditionalInfo = "Mois de jeûne, de prière et de spiritualité"
                },
                new Reminder
                {
                    Title = "Laylat al-Qadr",
                    Message = "Recherchez la Nuit du Destin dans les 10 dernières nuits de Ramadan.",
                    Type = "islamic_holiday",
                    Category = "laylat_qadr",
                    ScheduledDate = new DateTime(currentYear, 3, 27), // Approximatif
                    HijriDate = "27 Ramadan 1446",
                    IsHijriCalendar = true,
                    Priority = 1,
                    Language = "fr",
                    AdditionalInfo = "Nuit meilleure que mille mois"
                },
                new Reminder
                {
                    Title = "Aïd al-Fitr",
                    Message = "Aïd Mubarak ! Que cette fête soit source de joie et de bénédictions.",
                    Type = "islamic_holiday",
                    Category = "eid",
                    ScheduledDate = new DateTime(currentYear, 3, 31), // Approximatif
                    HijriDate = "1 Shawwal 1446",
                    IsHijriCalendar = true,
                    Priority = 1,
                    Language = "fr",
                    AdditionalInfo = "Fête de la rupture du jeûne"
                },
                new Reminder
                {
                    Title = "Aïd al-Adha",
                    Message = "Aïd Mubarak ! Que vos sacrifices soient acceptés par Allah.",
                    Type = "islamic_holiday",
                    Category = "eid",
                    ScheduledDate = new DateTime(currentYear, 6, 7), // Approximatif
                    HijriDate = "10 Dhul Hijjah 1446",
                    IsHijriCalendar = true,
                    Priority = 1,
                    Language = "fr",
                    AdditionalInfo = "Fête du sacrifice"
                },
                new Reminder
                {
                    Title = "Muharram - Nouvel An Islamique",
                    Message = "Bonne année islamique ! Que cette nouvelle année soit pleine de bénédictions.",
                    Type = "islamic_holiday",
                    Category = "new_year",
                    ScheduledDate = new DateTime(currentYear, 7, 7), // Approximatif
                    HijriDate = "1 Muharram 1447",
                    IsHijriCalendar = true,
                    Priority = 2,
                    Language = "fr"
                },
                new Reminder
                {
                    Title = "Jour d'Ashura",
                    Message = "Jeûnez le jour d'Ashura, Allah effacera les péchés de l'année précédente.",
                    Type = "islamic_holiday",
                    Category = "ashura",
                    ScheduledDate = new DateTime(currentYear, 7, 16), // Approximatif
                    HijriDate = "10 Muharram 1447",
                    IsHijriCalendar = true,
                    Priority = 1,
                    Language = "fr",
                    AdditionalInfo = "Jeûne recommandé"
                }
            };

            context.Reminders.AddRange(reminders);
        }
    }
}
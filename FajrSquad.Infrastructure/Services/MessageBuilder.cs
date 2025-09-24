using FajrSquad.Core.Entities;
using FajrSquad.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.Json;

namespace FajrSquad.Infrastructure.Services
{
    public class MessageBuilder : IMessageBuilder
    {
        private readonly FajrDbContext _db;
        private readonly ILogger<MessageBuilder> _logger;
        private readonly Dictionary<string, Dictionary<string, MessageTemplate>> _templates;

        public MessageBuilder(FajrDbContext db, ILogger<MessageBuilder> logger)
        {
            _db = db;
            _logger = logger;
            _templates = LoadTemplates();
        }

        public async Task<NotificationRequest> BuildMorningReminderAsync(User user, DeviceToken deviceToken)
        {
            var template = GetTemplate("morning_reminder", deviceToken.Language);
            var dua = await GetRandomDuaAsync(deviceToken.Language);
            
            return new NotificationRequest
            {
                Title = template.Title,
                Body = string.Format(template.Body, user.Name, dua),
                Data = new Dictionary<string, string>
                {
                    ["action"] = "open_app",
                    ["screen"] = "home",
                    ["type"] = "morning_reminder"
                },
                CollapseKey = template.CollapseKey,
                Priority = template.Priority,
                TtlSeconds = template.TtlSeconds
            };
        }

        public async Task<NotificationRequest> BuildEveningReminderAsync(User user, DeviceToken deviceToken)
        {
            var template = GetTemplate("evening_reminder", deviceToken.Language);
            
            return new NotificationRequest
            {
                Title = template.Title,
                Body = string.Format(template.Body, user.Name),
                Data = new Dictionary<string, string>
                {
                    ["action"] = "open_app",
                    ["screen"] = "adhkar",
                    ["type"] = "evening_reminder"
                },
                CollapseKey = template.CollapseKey,
                Priority = template.Priority,
                TtlSeconds = template.TtlSeconds
            };
        }

        public async Task<NotificationRequest> BuildFajrLateMotivationAsync(User user, DeviceToken deviceToken, TimeSpan fajrTime)
        {
            var template = GetTemplate("fajr_late_motivation", deviceToken.Language);
            var fajrTimeStr = fajrTime.ToString(@"hh\:mm");
            
            return new NotificationRequest
            {
                Title = template.Title,
                Body = string.Format(template.Body, user.Name, fajrTimeStr),
                Data = new Dictionary<string, string>
                {
                    ["action"] = "open_app",
                    ["screen"] = "fajr_checkin",
                    ["type"] = "fajr_late_motivation"
                },
                CollapseKey = template.CollapseKey,
                Priority = template.Priority,
                TtlSeconds = template.TtlSeconds
            };
        }

        public async Task<NotificationRequest> BuildEscalationReminderAsync(User user, DeviceToken deviceToken)
        {
            var template = GetTemplate("escalation_reminder", deviceToken.Language);
            
            return new NotificationRequest
            {
                Title = template.Title,
                Body = string.Format(template.Body, user.Name),
                Data = new Dictionary<string, string>
                {
                    ["action"] = "open_app",
                    ["screen"] = "fajr_checkin",
                    ["type"] = "escalation_reminder"
                },
                CollapseKey = template.CollapseKey,
                Priority = template.Priority,
                TtlSeconds = template.TtlSeconds
            };
        }

        public async Task<NotificationRequest> BuildAdminAlertAsync(User user, int consecutiveMissedDays)
        {
            var template = GetTemplate("admin_alert", "en"); // Admin alerts always in English
            
            return new NotificationRequest
            {
                Title = template.Title,
                Body = string.Format(template.Body, user.Name, user.City ?? "Unknown", consecutiveMissedDays),
                Data = new Dictionary<string, string>
                {
                    ["action"] = "open_app",
                    ["screen"] = "admin_dashboard",
                    ["type"] = "admin_alert",
                    ["userId"] = user.Id.ToString(),
                    ["daysMissed"] = consecutiveMissedDays.ToString()
                },
                CollapseKey = template.CollapseKey,
                Priority = NotificationPriority.High,
                TtlSeconds = 3600 // 1 hour for admin alerts
            };
        }

        public async Task<NotificationRequest> BuildDailyHadithAsync(Hadith hadith, User user, DeviceToken deviceToken)
        {
            var template = GetTemplate("daily_hadith", deviceToken.Language);
            var source = hadith.Source?.Length > 50 ? hadith.Source.Substring(0, 47) + "..." : hadith.Source ?? "";
            
            return new NotificationRequest
            {
                Title = template.Title,
                Body = string.Format(template.Body, hadith.Text, source),
                Data = new Dictionary<string, string>
                {
                    ["action"] = "open_app",
                    ["screen"] = "hadith",
                    ["type"] = "daily_hadith",
                    ["hadithId"] = hadith.Id.ToString()
                },
                CollapseKey = "daily_hadith",
                Priority = template.Priority,
                TtlSeconds = template.TtlSeconds
            };
        }

        public async Task<NotificationRequest> BuildDailyMotivationAsync(Motivation motivation, User user, DeviceToken deviceToken)
        {
            var template = GetTemplate("daily_motivation", deviceToken.Language);
            
            return new NotificationRequest
            {
                Title = template.Title,
                Body = string.Format(template.Body, motivation.Text),
                Data = new Dictionary<string, string>
                {
                    ["action"] = "open_app",
                    ["screen"] = "motivation",
                    ["type"] = "daily_motivation",
                    ["motivationId"] = motivation.Id.ToString()
                },
                CollapseKey = "daily_motivation",
                Priority = template.Priority,
                TtlSeconds = template.TtlSeconds
            };
        }

        public async Task<NotificationRequest> BuildEventCreatedAsync(Event eventEntity, User user, DeviceToken deviceToken)
        {
            var template = GetTemplate("event_created", deviceToken.Language);
            var eventDate = eventEntity.StartDate.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
            
            return new NotificationRequest
            {
                Title = string.Format(template.Title, eventEntity.Title),
                Body = string.Format(template.Body, eventDate, eventEntity.Location ?? "Online"),
                Data = new Dictionary<string, string>
                {
                    ["action"] = "open_app",
                    ["screen"] = "event_detail",
                    ["type"] = "event_created",
                    ["eventId"] = eventEntity.Id.ToString()
                },
                CollapseKey = template.CollapseKey,
                Priority = template.Priority,
                TtlSeconds = template.TtlSeconds
            };
        }

        public async Task<NotificationRequest> BuildEventReminderAsync(Event eventEntity, User user, DeviceToken deviceToken, string timeUntil)
        {
            var template = GetTemplate("event_reminder", deviceToken.Language);
            var eventTime = eventEntity.StartDate.ToString("HH:mm", CultureInfo.InvariantCulture);
            
            return new NotificationRequest
            {
                Title = string.Format(template.Title, timeUntil, eventEntity.Title),
                Body = string.Format(template.Body, eventTime, eventEntity.Location ?? "Online"),
                Data = new Dictionary<string, string>
                {
                    ["action"] = "open_app",
                    ["screen"] = "event_detail",
                    ["type"] = "event_reminder",
                    ["eventId"] = eventEntity.Id.ToString()
                },
                CollapseKey = $"event_reminder_{eventEntity.Id}",
                Priority = template.Priority,
                TtlSeconds = template.TtlSeconds
            };
        }

        private MessageTemplate GetTemplate(string type, string language)
        {
            if (_templates.TryGetValue(language, out var langTemplates) && 
                langTemplates.TryGetValue(type, out var template))
            {
                return template;
            }

            // Fallback to English
            if (_templates.TryGetValue("en", out var enTemplates) && 
                enTemplates.TryGetValue(type, out var enTemplate))
            {
                return enTemplate;
            }

            // Ultimate fallback
            return new MessageTemplate
            {
                Title = "FajrSquad",
                Body = "You have a new notification",
                Priority = NotificationPriority.Normal,
                TtlSeconds = 7200
            };
        }

        private async Task<string> GetRandomDuaAsync(string language)
        {
            // This could be enhanced to fetch from a database table of duas
            var duas = language switch
            {
                "it" => new[]
                {
                    "Bismillah, Allahumma inni as'aluka khayra hadha al-yawm",
                    "Allahumma barik li fi sabahi wa barik li fi masai",
                    "Subhanallahi wa bihamdihi, subhanallahil 'azim"
                },
                "fr" => new[]
                {
                    "Bismillah, Allahumma inni as'aluka khayra hadha al-yawm",
                    "Allahumma barik li fi sabahi wa barik li fi masai",
                    "Subhanallahi wa bihamdihi, subhanallahil 'azim"
                },
                _ => new[]
                {
                    "Bismillah, Allahumma inni as'aluka khayra hadha al-yawm",
                    "Allahumma barik li fi sabahi wa barik li fi masai",
                    "Subhanallahi wa bihamdihi, subhanallahil 'azim"
                }
            };

            var random = new Random();
            return duas[random.Next(duas.Length)];
        }

        private Dictionary<string, Dictionary<string, MessageTemplate>> LoadTemplates()
        {
            return new Dictionary<string, Dictionary<string, MessageTemplate>>
            {
                ["it"] = new Dictionary<string, MessageTemplate>
                {
                    ["morning_reminder"] = new MessageTemplate
                    {
                        Title = "Ricorda Allah prima di uscire",
                        Body = "Salam {0}, ricorda di fare dua prima di uscire: {1}",
                        CollapseKey = "morning_reminder",
                        Priority = NotificationPriority.Normal,
                        TtlSeconds = 3600
                    },
                    ["evening_reminder"] = new MessageTemplate
                    {
                        Title = "Prima di dormire (Sunnah)",
                        Body = "Salam {0}, ricorda di fare wudu e adhkar prima di dormire",
                        CollapseKey = "evening_reminder",
                        Priority = NotificationPriority.Normal,
                        TtlSeconds = 3600
                    },
                    ["fajr_late_motivation"] = new MessageTemplate
                    {
                        Title = "Non perdere il tuo Fajr!",
                        Body = "Salam {0}, non è troppo tardi per il Fajr! L'orario era alle {1}",
                        CollapseKey = "fajr_late",
                        Priority = NotificationPriority.High,
                        TtlSeconds = 1800
                    },
                    ["escalation_reminder"] = new MessageTemplate
                    {
                        Title = "Motivazione extra",
                        Body = "Salam {0}, ricorda che Allah ti vede sempre. Non arrenderti!",
                        CollapseKey = "escalation",
                        Priority = NotificationPriority.High,
                        TtlSeconds = 1800
                    },
                    ["daily_hadith"] = new MessageTemplate
                    {
                        Title = "Hadith del giorno",
                        Body = "{0}\n\n- {1}",
                        CollapseKey = "daily_hadith",
                        Priority = NotificationPriority.Normal,
                        TtlSeconds = 7200
                    },
                    ["daily_motivation"] = new MessageTemplate
                    {
                        Title = "Motivazione quotidiana",
                        Body = "{0}",
                        CollapseKey = "daily_motivation",
                        Priority = NotificationPriority.Normal,
                        TtlSeconds = 7200
                    },
                    ["event_created"] = new MessageTemplate
                    {
                        Title = "Nuovo evento: {0}",
                        Body = "{0} - {1}",
                        CollapseKey = "event_created",
                        Priority = NotificationPriority.Normal,
                        TtlSeconds = 7200
                    },
                    ["event_reminder"] = new MessageTemplate
                    {
                        Title = "{0}: {1}",
                        Body = "Inizia alle {0} - {1}",
                        CollapseKey = "event_reminder",
                        Priority = NotificationPriority.Normal,
                        TtlSeconds = 3600
                    }
                },
                ["fr"] = new Dictionary<string, MessageTemplate>
                {
                    ["morning_reminder"] = new MessageTemplate
                    {
                        Title = "Rappelle-toi d'Allah avant de sortir",
                        Body = "Salam {0}, rappelle-toi de faire dua avant de sortir: {1}",
                        CollapseKey = "morning_reminder",
                        Priority = NotificationPriority.Normal,
                        TtlSeconds = 3600
                    },
                    ["evening_reminder"] = new MessageTemplate
                    {
                        Title = "Avant de dormir (Sunnah)",
                        Body = "Salam {0}, rappelle-toi de faire wudu et adhkar avant de dormir",
                        CollapseKey = "evening_reminder",
                        Priority = NotificationPriority.Normal,
                        TtlSeconds = 3600
                    },
                    ["fajr_late_motivation"] = new MessageTemplate
                    {
                        Title = "Ne rate pas ton Fajr!",
                        Body = "Salam {0}, il n'est pas trop tard pour le Fajr! L'heure était {1}",
                        CollapseKey = "fajr_late",
                        Priority = NotificationPriority.High,
                        TtlSeconds = 1800
                    },
                    ["escalation_reminder"] = new MessageTemplate
                    {
                        Title = "Motivation supplémentaire",
                        Body = "Salam {0}, rappelle-toi qu'Allah te voit toujours. N'abandonne pas!",
                        CollapseKey = "escalation",
                        Priority = NotificationPriority.High,
                        TtlSeconds = 1800
                    },
                    ["daily_hadith"] = new MessageTemplate
                    {
                        Title = "Hadith du jour",
                        Body = "{0}\n\n- {1}",
                        CollapseKey = "daily_hadith",
                        Priority = NotificationPriority.Normal,
                        TtlSeconds = 7200
                    },
                    ["daily_motivation"] = new MessageTemplate
                    {
                        Title = "Motivation quotidienne",
                        Body = "{0}",
                        CollapseKey = "daily_motivation",
                        Priority = NotificationPriority.Normal,
                        TtlSeconds = 7200
                    },
                    ["event_created"] = new MessageTemplate
                    {
                        Title = "Nouvel événement: {0}",
                        Body = "{0} - {1}",
                        CollapseKey = "event_created",
                        Priority = NotificationPriority.Normal,
                        TtlSeconds = 7200
                    },
                    ["event_reminder"] = new MessageTemplate
                    {
                        Title = "{0}: {1}",
                        Body = "Commence à {0} - {1}",
                        CollapseKey = "event_reminder",
                        Priority = NotificationPriority.Normal,
                        TtlSeconds = 3600
                    }
                },
                ["en"] = new Dictionary<string, MessageTemplate>
                {
                    ["morning_reminder"] = new MessageTemplate
                    {
                        Title = "Remember Allah before leaving",
                        Body = "Salam {0}, remember to make dua before leaving: {1}",
                        CollapseKey = "morning_reminder",
                        Priority = NotificationPriority.Normal,
                        TtlSeconds = 3600
                    },
                    ["evening_reminder"] = new MessageTemplate
                    {
                        Title = "Before sleeping (Sunnah)",
                        Body = "Salam {0}, remember to do wudu and adhkar before sleeping",
                        CollapseKey = "evening_reminder",
                        Priority = NotificationPriority.Normal,
                        TtlSeconds = 3600
                    },
                    ["fajr_late_motivation"] = new MessageTemplate
                    {
                        Title = "Don't miss your Fajr!",
                        Body = "Salam {0}, it's not too late for Fajr! The time was {1}",
                        CollapseKey = "fajr_late",
                        Priority = NotificationPriority.High,
                        TtlSeconds = 1800
                    },
                    ["escalation_reminder"] = new MessageTemplate
                    {
                        Title = "Extra motivation",
                        Body = "Salam {0}, remember that Allah always sees you. Don't give up!",
                        CollapseKey = "escalation",
                        Priority = NotificationPriority.High,
                        TtlSeconds = 1800
                    },
                    ["daily_hadith"] = new MessageTemplate
                    {
                        Title = "Daily Hadith",
                        Body = "{0}\n\n- {1}",
                        CollapseKey = "daily_hadith",
                        Priority = NotificationPriority.Normal,
                        TtlSeconds = 7200
                    },
                    ["daily_motivation"] = new MessageTemplate
                    {
                        Title = "Daily motivation",
                        Body = "{0}",
                        CollapseKey = "daily_motivation",
                        Priority = NotificationPriority.Normal,
                        TtlSeconds = 7200
                    },
                    ["event_created"] = new MessageTemplate
                    {
                        Title = "New event: {0}",
                        Body = "{0} - {1}",
                        CollapseKey = "event_created",
                        Priority = NotificationPriority.Normal,
                        TtlSeconds = 7200
                    },
                    ["event_reminder"] = new MessageTemplate
                    {
                        Title = "{0}: {1}",
                        Body = "Starts at {0} - {1}",
                        CollapseKey = "event_reminder",
                        Priority = NotificationPriority.Normal,
                        TtlSeconds = 3600
                    },
                    ["admin_alert"] = new MessageTemplate
                    {
                        Title = "User in difficulty",
                        Body = "User {0} from {1} has missed Fajr for {2} consecutive days",
                        CollapseKey = "admin_alert",
                        Priority = NotificationPriority.High,
                        TtlSeconds = 3600
                    }
                }
            };
        }
    }
}

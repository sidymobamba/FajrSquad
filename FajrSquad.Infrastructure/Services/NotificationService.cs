using FajrSquad.Infrastructure.Data;
using FirebaseAdmin.Messaging;
using Microsoft.EntityFrameworkCore;

namespace FajrSquad.Infrastructure.Services
{
    public class NotificationService
    {
        private readonly FajrDbContext _db;

        public NotificationService(FajrDbContext db)
        {
            _db = db;
        }

        public async Task SendMotivationNotification(string timeOfDay)
        {
            var motivation = await _db.Motivations
                .Where(m => m.Type == timeOfDay && m.IsActive && !m.IsDeleted)
                .OrderBy(m => m.Priority)
                .ThenBy(m => Guid.NewGuid())
                .FirstOrDefaultAsync();

            if (motivation == null) return;

            var message = new Message
            {
                Notification = new Notification
                {
                    Title = "Motivazione per te",
                    Body = motivation.Text
                },
                Topic = "all"
            };

            await FirebaseMessaging.DefaultInstance.SendAsync(message);
        }

        public async Task SendHadithNotification()
        {
            var hadith = await _db.Hadiths
                .Where(h => h.Language == "fr" && h.IsActive && !h.IsDeleted)
                .OrderBy(h => h.Priority)
                .ThenBy(h => Guid.NewGuid())
                .FirstOrDefaultAsync();

            if (hadith == null) return;

            var message = new Message
            {
                Notification = new Notification
                {
                    Title = "Hadith del giorno",
                    Body = hadith.Text
                },
                Topic = "all"
            };

            await FirebaseMessaging.DefaultInstance.SendAsync(message);
        }
    }
}

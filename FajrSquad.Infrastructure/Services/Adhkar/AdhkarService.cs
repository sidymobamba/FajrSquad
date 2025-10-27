using FajrSquad.Core.DTOs.Adhkar;
using FajrSquad.Core.Entities.Adhkar;
using FajrSquad.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FajrSquad.Infrastructure.Services.Adhkar
{
    public interface IAdhkarService
    {
        Task<UserAdhkarProgress> UpsertProgressAsync(Guid userId, DateOnly dateUtc);
        Task<UserAdhkarProgress?> GetProgressAsync(Guid userId, DateOnly dateUtc);
        Task UpdateCountAsync(Guid userId, DateOnly dateUtc, string adhkarCode, int delta);
        Task CompleteWindowAsync(Guid userId, DateOnly dateUtc, string window, string setCode);
    }

    public class AdhkarService : IAdhkarService
    {
        private readonly FajrDbContext _context;

        public AdhkarService(FajrDbContext context)
        {
            _context = context;
        }

        public async Task<UserAdhkarProgress> UpsertProgressAsync(Guid userId, DateOnly dateUtc)
        {
            var progress = await _context.UserAdhkarProgress
                .FirstOrDefaultAsync(p => p.UserId == userId && p.DateUtc == dateUtc);

            if (progress == null)
            {
                progress = new UserAdhkarProgress
                {
                    UserId = userId,
                    DateUtc = dateUtc,
                    TzId = "Europe/Rome", // Default timezone
                    MorningCompleted = false,
                    EveningCompleted = false,
                    Counts = new Dictionary<string, int>()
                };
                _context.UserAdhkarProgress.Add(progress);
            }

            await _context.SaveChangesAsync();
            return progress;
        }

        public async Task<UserAdhkarProgress?> GetProgressAsync(Guid userId, DateOnly dateUtc)
        {
            return await _context.UserAdhkarProgress
                .FirstOrDefaultAsync(p => p.UserId == userId && p.DateUtc == dateUtc);
        }

        public async Task UpdateCountAsync(Guid userId, DateOnly dateUtc, string adhkarCode, int delta)
        {
            var progress = await UpsertProgressAsync(userId, dateUtc);
            
            if (progress.Counts.ContainsKey(adhkarCode))
            {
                progress.Counts[adhkarCode] = Math.Max(0, progress.Counts[adhkarCode] + delta);
            }
            else
            {
                progress.Counts[adhkarCode] = Math.Max(0, delta);
            }

            await _context.SaveChangesAsync();
        }

        public async Task CompleteWindowAsync(Guid userId, DateOnly dateUtc, string window, string setCode)
        {
            var progress = await UpsertProgressAsync(userId, dateUtc);
            var set = await _context.AdhkarSets.FirstOrDefaultAsync(s => s.Code == setCode);

            if (set == null) return;

            if (window == "morning")
            {
                progress.MorningCompleted = true;
                progress.MorningCompletedAt = DateTimeOffset.UtcNow;
                progress.MorningSetId = set.Id;
            }
            else if (window == "evening")
            {
                progress.EveningCompleted = true;
                progress.EveningCompletedAt = DateTimeOffset.UtcNow;
                progress.EveningSetId = set.Id;
            }

            await _context.SaveChangesAsync();
        }
    }
}

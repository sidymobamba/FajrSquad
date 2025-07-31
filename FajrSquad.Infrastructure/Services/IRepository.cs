using System.Linq.Expressions;
using FajrSquad.Core.Entities;

namespace FajrSquad.Infrastructure.Services
{
    public interface IRepository<T> where T : BaseEntity
    {
        Task<T?> GetByIdAsync(Guid id);
        Task<T?> GetByIdAsync(Guid id, params Expression<Func<T, object>>[] includes);
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> GetAllAsync(params Expression<Func<T, object>>[] includes);
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);
        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);
        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
        Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);
        Task<T> AddAsync(T entity);
        Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities);
        Task UpdateAsync(T entity);
        Task UpdateRangeAsync(IEnumerable<T> entities);
        Task DeleteAsync(T entity);
        Task DeleteAsync(Guid id);
        Task DeleteRangeAsync(IEnumerable<T> entities);
        Task SoftDeleteAsync(Guid id);
        Task SoftDeleteAsync(T entity);
    }

    public interface IUserRepository : IRepository<User>
    {
        Task<User?> GetByPhoneAsync(string phone);
        Task<User?> GetByEmailAsync(string email);
        Task<IEnumerable<User>> GetActiveUsersAsync();
        Task<IEnumerable<User>> GetUsersByRoleAsync(string role);
        Task<bool> PhoneExistsAsync(string phone);
        Task<bool> EmailExistsAsync(string email);
    }

    public interface IFajrCheckInRepository : IRepository<FajrCheckIn>
    {
        Task<FajrCheckIn?> GetTodayCheckInAsync(Guid userId);
        Task<IEnumerable<FajrCheckIn>> GetUserHistoryAsync(Guid userId, int take = 30);
        Task<IEnumerable<FajrCheckIn>> GetWeeklyCheckInsAsync(DateTime startDate);
        Task<IEnumerable<FajrCheckIn>> GetDailyCheckInsAsync(DateTime date);
        Task<int> GetUserStreakAsync(Guid userId);
        Task<IEnumerable<User>> GetMissedUsersAsync(DateTime date);
    }

    public interface IOtpRepository : IRepository<OtpCode>
    {
        Task<OtpCode?> GetValidOtpAsync(string phone, string code);
        Task InvalidateOtpsAsync(string phone);
        Task CleanupExpiredOtpsAsync();
    }
}
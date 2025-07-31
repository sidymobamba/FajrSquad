using FajrSquad.Core.Entities;
using FajrSquad.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FajrSquad.Infrastructure.Services
{
    public class UserRepository : Repository<User>, IUserRepository
    {
        public UserRepository(FajrDbContext context, ILogger<UserRepository> logger) 
            : base(context, logger)
        {
        }

        public async Task<User?> GetByPhoneAsync(string phone)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.Phone == phone && !u.IsDeleted);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            if (string.IsNullOrEmpty(email))
                return null;
                
            return await _dbSet.FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted);
        }

        public async Task<IEnumerable<User>> GetActiveUsersAsync()
        {
            return await _dbSet
                .Where(u => !u.IsDeleted)
                .OrderBy(u => u.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<User>> GetUsersByRoleAsync(string role)
        {
            return await _dbSet
                .Where(u => u.Role == role && !u.IsDeleted)
                .OrderBy(u => u.Name)
                .ToListAsync();
        }

        public async Task<bool> PhoneExistsAsync(string phone)
        {
            return await _dbSet.AnyAsync(u => u.Phone == phone && !u.IsDeleted);
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            if (string.IsNullOrEmpty(email))
                return false;
                
            return await _dbSet.AnyAsync(u => u.Email == email && !u.IsDeleted);
        }
    }
}
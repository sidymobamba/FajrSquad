using FajrSquad.Core.Entities;
using FajrSquad.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace FajrSquad.Infrastructure.Services
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly FajrDbContext _context;
        private readonly ILogger<UnitOfWork> _logger;
        private IDbContextTransaction? _transaction;
        private bool _disposed = false;

        // Repository instances
        private IUserRepository? _users;
        private IFajrCheckInRepository? _fajrCheckIns;
        private IOtpRepository? _otpCodes;
        private readonly Dictionary<Type, object> _repositories = new();

        public UnitOfWork(
            FajrDbContext context, 
            ILogger<UnitOfWork> logger,
            IUserRepository userRepository,
            IFajrCheckInRepository fajrCheckInRepository,
            IOtpRepository otpRepository)
        {
            _context = context;
            _logger = logger;
            _users = userRepository;
            _fajrCheckIns = fajrCheckInRepository;
            _otpCodes = otpRepository;
        }

        public IUserRepository Users => _users ??= GetRepository<IUserRepository>();
        public IFajrCheckInRepository FajrCheckIns => _fajrCheckIns ??= GetRepository<IFajrCheckInRepository>();
        public IOtpRepository OtpCodes => _otpCodes ??= GetRepository<IOtpRepository>();

        public IRepository<T> Repository<T>() where T : BaseEntity
        {
            var type = typeof(T);
            if (_repositories.ContainsKey(type))
            {
                return (IRepository<T>)_repositories[type];
            }

            var repositoryType = typeof(Repository<>).MakeGenericType(type);
            var loggerType = typeof(ILogger<>).MakeGenericType(repositoryType);
            var logger = _context.GetService(loggerType);
            
            var repository = Activator.CreateInstance(repositoryType, _context, logger);
            _repositories[type] = repository!;
            
            return (IRepository<T>)repository!;
        }

        public async Task<int> SaveChangesAsync()
        {
            try
            {
                return await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving changes to database");
                throw;
            }
        }

        public async Task BeginTransactionAsync()
        {
            if (_transaction != null)
            {
                throw new InvalidOperationException("Transaction already started");
            }

            _transaction = await _context.Database.BeginTransactionAsync();
            _logger.LogInformation("Database transaction started");
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("No transaction to commit");
            }

            try
            {
                await _transaction.CommitAsync();
                _logger.LogInformation("Database transaction committed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error committing transaction");
                await RollbackTransactionAsync();
                throw;
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction == null)
            {
                return;
            }

            try
            {
                await _transaction.RollbackAsync();
                _logger.LogInformation("Database transaction rolled back");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rolling back transaction");
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        private T GetRepository<T>()
        {
            var serviceType = typeof(T);
            var service = _context.GetService(serviceType);
            
            if (service == null)
            {
                throw new InvalidOperationException($"Service of type {serviceType.Name} is not registered");
            }
            
            return (T)service;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _transaction?.Dispose();
                _context.Dispose();
                _disposed = true;
            }
        }
    }
}
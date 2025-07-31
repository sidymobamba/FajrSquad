namespace FajrSquad.Infrastructure.Services
{
    public interface IUnitOfWork : IDisposable
    {
        IUserRepository Users { get; }
        IFajrCheckInRepository FajrCheckIns { get; }
        IOtpRepository OtpCodes { get; }
        IRepository<T> Repository<T>() where T : FajrSquad.Core.Entities.BaseEntity;
        
        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
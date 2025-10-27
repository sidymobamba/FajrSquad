using FajrSquad.Core.Entities.Adhkar;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FajrSquad.Infrastructure.Data.EntityConfigurations.Adhkar
{
    public class UserAdhkarProgressConfiguration : IEntityTypeConfiguration<UserAdhkarProgress>
    {
        public void Configure(EntityTypeBuilder<UserAdhkarProgress> e)
        {
            e.ToTable("user_adhkar_progress");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.UserId, x.DateUtc }).IsUnique();
            e.Property(x => x.Counts).HasColumnType("jsonb");
        }
    }
}

using FajrSquad.Core.Entities.Adhkar;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FajrSquad.Infrastructure.Data.EntityConfigurations
{
    public class UserAdhkarStatsConfiguration : IEntityTypeConfiguration<UserAdhkarStats>
    {
        public void Configure(EntityTypeBuilder<UserAdhkarStats> builder)
        {
            builder.ToTable("UserAdhkarStats");
            
            builder.HasKey(s => s.Id);
            
            builder.Property(s => s.UserId)
                .IsRequired();
                
            builder.Property(s => s.TotalCompleted)
                .HasDefaultValue(0);
                
            builder.Property(s => s.CurrentStreak)
                .HasDefaultValue(0);
                
            builder.Property(s => s.LongestStreak)
                .HasDefaultValue(0);
                
            builder.Property(s => s.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAddOrUpdate();

            // Unique constraint: un utente ha una sola riga di statistiche
            builder.HasIndex(s => s.UserId)
                .IsUnique()
                .HasDatabaseName("IX_UserAdhkarStats_UserId");

            // Foreign key
            builder.HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}


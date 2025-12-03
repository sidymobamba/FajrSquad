using FajrSquad.Core.Entities.Adhkar;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FajrSquad.Infrastructure.Data.EntityConfigurations
{
    public class UserAdhkarProgressConfiguration : IEntityTypeConfiguration<UserAdhkarProgress>
    {
        public void Configure(EntityTypeBuilder<UserAdhkarProgress> builder)
        {
            builder.ToTable("UserAdhkarProgress");
            
            builder.HasKey(p => p.Id);
            
            builder.Property(p => p.UserId)
                .IsRequired();
                
            builder.Property(p => p.AdhkarId)
                .IsRequired();
                
            builder.Property(p => p.Date)
                .IsRequired()
                .HasColumnType("date");
                
            builder.Property(p => p.CurrentCount)
                .HasDefaultValue(0);
                
            builder.Property(p => p.IsCompleted)
                .HasDefaultValue(false);
                
            builder.Property(p => p.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Unique constraint: un utente può avere un solo progresso per adhkar per data
            builder.HasIndex(p => new { p.UserId, p.AdhkarId, p.Date })
                .IsUnique()
                .HasDatabaseName("IX_UserAdhkarProgress_User_Adhkar_Date");
                
            builder.HasIndex(p => new { p.UserId, p.Date })
                .HasDatabaseName("IX_UserAdhkarProgress_User_Date");
                
            builder.HasIndex(p => p.AdhkarId)
                .HasDatabaseName("IX_UserAdhkarProgress_AdhkarId");

            // Foreign keys
            builder.HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            builder.HasOne(p => p.Adhkar)
                .WithMany()
                .HasForeignKey(p => p.AdhkarId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}


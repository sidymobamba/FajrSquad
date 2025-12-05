using FajrSquad.Core.Entities.Adhkar;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FajrSquad.Infrastructure.Data.EntityConfigurations
{
    public class UserAdhkarFavoriteConfiguration : IEntityTypeConfiguration<UserAdhkarFavorite>
    {
        public void Configure(EntityTypeBuilder<UserAdhkarFavorite> builder)
        {
            builder.ToTable("UserAdhkarFavorites");
            
            builder.HasKey(f => f.Id);
            
            builder.Property(f => f.UserId)
                .IsRequired();
                
            builder.Property(f => f.AdhkarId)
                .IsRequired();
                
            builder.Property(f => f.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Unique constraint: un utente può avere un adhkar nei preferiti solo una volta
            builder.HasIndex(f => new { f.UserId, f.AdhkarId })
                .IsUnique()
                .HasDatabaseName("IX_UserAdhkarFavorites_User_Adhkar");
                
            builder.HasIndex(f => f.UserId)
                .HasDatabaseName("IX_UserAdhkarFavorites_UserId");

            // Foreign keys
            builder.HasOne(f => f.User)
                .WithMany()
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            builder.HasOne(f => f.Adhkar)
                .WithMany()
                .HasForeignKey(f => f.AdhkarId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}




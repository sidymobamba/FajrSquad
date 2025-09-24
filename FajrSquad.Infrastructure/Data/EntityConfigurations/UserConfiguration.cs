using FajrSquad.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FajrSquad.Infrastructure.Data.EntityConfigurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users");
            
            builder.HasKey(u => u.Id);
            
            builder.Property(u => u.Name)
                .IsRequired()
                .HasMaxLength(100);
                
            builder.Property(u => u.Phone)
                .IsRequired()
                .HasMaxLength(20);
                
            builder.Property(u => u.Email)
                .HasMaxLength(255);
                
            builder.Property(u => u.City)
                .IsRequired()
                .HasMaxLength(50);
                
            builder.Property(u => u.PasswordHash)
                .HasMaxLength(255);
                
            builder.Property(u => u.Role)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("User");
                
            builder.Property(u => u.MotivatingBrother)
                .HasMaxLength(100);
                
            builder.Property(u => u.FajrStreak)
                .HasDefaultValue(0);
                
            builder.Property(u => u.RegisteredAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
                
            builder.Property(u => u.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
                
            builder.Property(u => u.IsDeleted)
                .HasDefaultValue(false);

            // Indexes
            builder.HasIndex(u => u.Phone)
                .IsUnique()
                .HasDatabaseName("IX_Users_Phone");
                
            builder.HasIndex(u => u.Email)
                .HasDatabaseName("IX_Users_Email");
                
            builder.HasIndex(u => u.Role)
                .HasDatabaseName("IX_Users_Role");

            // Relationships
            builder.HasMany(u => u.FajrCheckIns)
                .WithOne(c => c.User)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Query filters for soft delete
            builder.HasQueryFilter(u => !u.IsDeleted);
        }
    }
}
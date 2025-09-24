using FajrSquad.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FajrSquad.Infrastructure.Data.EntityConfigurations
{
    public class FajrCheckInConfiguration : IEntityTypeConfiguration<FajrCheckIn>
    {
        public void Configure(EntityTypeBuilder<FajrCheckIn> builder)
        {
            builder.ToTable("FajrCheckIns");
            
            builder.HasKey(f => f.Id);
            
            builder.Property(f => f.Date)
                .IsRequired()
                .HasColumnType("date");
                
            builder.Property(f => f.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);
                
            builder.Property(f => f.Notes)
                .HasMaxLength(500);
                
            builder.Property(f => f.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
                
            builder.Property(f => f.IsDeleted)
                .HasDefaultValue(false);

            // Indexes
            builder.HasIndex(f => new { f.UserId, f.Date })
                .IsUnique()
                .HasDatabaseName("IX_FajrCheckIns_UserId_Date");
                
            builder.HasIndex(f => f.Date)
                .HasDatabaseName("IX_FajrCheckIns_Date");
                
            builder.HasIndex(f => f.Status)
                .HasDatabaseName("IX_FajrCheckIns_Status");

            // Relationships
            builder.HasOne(f => f.User)
                .WithMany(u => u.FajrCheckIns)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Query filters for soft delete
            builder.HasQueryFilter(f => !f.IsDeleted);
        }
    }
}
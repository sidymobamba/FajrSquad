using FajrSquad.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FajrSquad.Infrastructure.Data.EntityConfigurations
{
    public class DeviceTokenConfiguration : IEntityTypeConfiguration<DeviceToken>
    {
        public void Configure(EntityTypeBuilder<DeviceToken> builder)
        {
            builder.HasKey(d => d.Id);

            builder.Property(d => d.UserId)
                .IsRequired();

            builder.Property(d => d.Token)
                .IsRequired()
                .HasMaxLength(512);

            builder.Property(d => d.Platform)
                .HasMaxLength(20)
                .HasDefaultValue("Android");

            builder.Property(d => d.Language)
                .HasMaxLength(10)
                .HasDefaultValue("it");

            builder.Property(d => d.TimeZone)
                .IsRequired()
                .HasMaxLength(100)
                .HasDefaultValue("Africa/Dakar");

            // Add check constraint for timezone validation
            builder.ToTable("DeviceTokens", t => t.HasCheckConstraint("CK_DeviceTokens_TimeZone_Length", 
                "LENGTH(\"TimeZone\") >= 3 AND \"TimeZone\" != 'string'"));

            builder.Property(d => d.AppVersion)
                .HasMaxLength(40);

            builder.Property(d => d.IsActive)
                .HasDefaultValue(true);

            builder.Property(d => d.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.Property(d => d.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.Property(d => d.IsDeleted)
                .HasDefaultValue(false);

            // Indexes
            builder.HasIndex(d => new { d.UserId, d.Token })
                .IsUnique()
                .HasDatabaseName("IX_DeviceTokens_UserId_Token");

            builder.HasIndex(d => d.UserId)
                .HasDatabaseName("IX_DeviceTokens_UserId");

            builder.HasIndex(d => d.IsActive)
                .HasDatabaseName("IX_DeviceTokens_IsActive");

            builder.HasIndex(d => new { d.UserId, d.IsActive })
                .HasDatabaseName("IX_DeviceTokens_UserId_IsActive");

            // Relationships
            builder.HasOne(d => d.User)
                .WithMany(u => u.DeviceTokens)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Query filters for soft delete - matching User's filter
            builder.HasQueryFilter(d => !d.IsDeleted);
        }
    }
}

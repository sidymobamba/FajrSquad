using FajrSquad.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FajrSquad.Infrastructure.Data.EntityConfigurations
{
    public class OtpCodeConfiguration : IEntityTypeConfiguration<OtpCode>
    {
        public void Configure(EntityTypeBuilder<OtpCode> builder)
        {
            builder.ToTable("OtpCodes");
            
            builder.HasKey(o => o.Id);
            
            builder.Property(o => o.Phone)
                .IsRequired()
                .HasMaxLength(20);
                
            builder.Property(o => o.Code)
                .IsRequired()
                .HasMaxLength(6);
                
            builder.Property(o => o.ExpiresAt)
                .IsRequired();
                
            builder.Property(o => o.IsUsed)
                .HasDefaultValue(false);

            builder.Property(o => o.CreatedAt)
            .HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");

            // Indexes
            builder.HasIndex(o => new { o.Phone, o.Code, o.IsUsed })
                .HasDatabaseName("IX_OtpCodes_Phone_Code_IsUsed");
                
            builder.HasIndex(o => o.ExpiresAt)
                .HasDatabaseName("IX_OtpCodes_ExpiresAt");

            // Relationships
            builder.HasOne(o => o.User)
                .WithMany()
                .HasForeignKey("Phone")
                .HasPrincipalKey(u => u.Phone)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
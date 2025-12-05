using FajrSquad.Core.Entities.Adhkar;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FajrSquad.Infrastructure.Data.EntityConfigurations
{
    public class AdhkarConfiguration : IEntityTypeConfiguration<Adhkar>
    {
        public void Configure(EntityTypeBuilder<Adhkar> builder)
        {
            builder.ToTable("Adhkar");
            
            builder.HasKey(a => a.Id);
            
            builder.Property(a => a.Arabic)
                .IsRequired()
                .HasMaxLength(2000);
                
            builder.Property(a => a.Transliteration)
                .HasMaxLength(1000);
                
            builder.Property(a => a.Translation)
                .IsRequired()
                .HasMaxLength(2000);
                
            builder.Property(a => a.Repetitions)
                .IsRequired()
                .HasDefaultValue(1);
                
            builder.Property(a => a.Source)
                .HasMaxLength(200);
                
            builder.Property(a => a.Category)
                .IsRequired()
                .HasMaxLength(50);
                
            builder.Property(a => a.Priority)
                .HasDefaultValue(1);
                
            builder.Property(a => a.IsActive)
                .HasDefaultValue(true);
                
            builder.Property(a => a.Language)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("it");

            builder.Property(a => a.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.Property(a => a.IsDeleted)
                .HasDefaultValue(false);

            // Indexes
            builder.HasIndex(a => new { a.Category, a.Language, a.IsActive })
                .HasDatabaseName("IX_Adhkar_Category_Language_IsActive");
                
            builder.HasIndex(a => a.Priority)
                .HasDatabaseName("IX_Adhkar_Priority");

            // Query filters for soft delete
            builder.HasQueryFilter(a => !a.IsDeleted);
        }
    }
}




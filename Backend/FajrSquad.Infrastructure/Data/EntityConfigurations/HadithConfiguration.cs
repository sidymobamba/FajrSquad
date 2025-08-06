using FajrSquad.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FajrSquad.Infrastructure.Data.EntityConfigurations
{
    public class HadithConfiguration : IEntityTypeConfiguration<Hadith>
    {
        public void Configure(EntityTypeBuilder<Hadith> builder)
        {
            builder.ToTable("Hadiths");
            
            builder.HasKey(h => h.Id);
            
            builder.Property(h => h.Text)
                .IsRequired()
                .HasMaxLength(2000);
                
            builder.Property(h => h.TextArabic)
                .IsRequired()
                .HasMaxLength(500);
                
            builder.Property(h => h.Source)
                .IsRequired()
                .HasMaxLength(200);
                
            builder.Property(h => h.Category)
                .IsRequired()
                .HasMaxLength(100);
                
            builder.Property(h => h.Theme)
                .HasMaxLength(100);
                
            builder.Property(h => h.Priority)
                .HasDefaultValue(1);
                
            builder.Property(h => h.IsActive)
                .HasDefaultValue(true);
                
            builder.Property(h => h.Language)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("fr");

            builder
     .Property(h => h.CreatedAt)
     .HasDefaultValueSql("CURRENT_TIMESTAMP");


            builder.Property(h => h.IsDeleted)
                .HasDefaultValue(false);

            // Indexes
            builder.HasIndex(h => new { h.Category, h.Language, h.IsActive })
                .HasDatabaseName("IX_Hadiths_Category_Language_IsActive");
                
            builder.HasIndex(h => h.Priority)
                .HasDatabaseName("IX_Hadiths_Priority");
                
            builder.HasIndex(h => h.Theme)
                .HasDatabaseName("IX_Hadiths_Theme");

            // Query filters for soft delete
            builder.HasQueryFilter(h => !h.IsDeleted);
        }
    }
}
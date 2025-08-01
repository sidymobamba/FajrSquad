using FajrSquad.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FajrSquad.Infrastructure.Data.EntityConfigurations
{
    public class MotivationConfiguration : IEntityTypeConfiguration<Motivation>
    {
        public void Configure(EntityTypeBuilder<Motivation> builder)
        {
            builder.ToTable("Motivations");
            
            builder.HasKey(m => m.Id);
            
            builder.Property(m => m.Text)
                .IsRequired()
                .HasMaxLength(1000);
                
            builder.Property(m => m.Type)
                .IsRequired()
                .HasMaxLength(100);
                
            builder.Property(m => m.Theme)
                .HasMaxLength(100);
                
            builder.Property(m => m.Priority)
                .HasDefaultValue(1);
                
            builder.Property(m => m.IsActive)
                .HasDefaultValue(true);
                
            builder.Property(m => m.Language)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("fr");
                
            builder.Property(m => m.Author)
                .HasMaxLength(200);
                
            builder.Property(m => m.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
                
            builder.Property(m => m.IsDeleted)
                .HasDefaultValue(false);

            // Indexes
            builder.HasIndex(m => new { m.Type, m.Language, m.IsActive })
                .HasDatabaseName("IX_Motivations_Type_Language_IsActive");
                
            builder.HasIndex(m => m.Priority)
                .HasDatabaseName("IX_Motivations_Priority");
                
            builder.HasIndex(m => m.Theme)
                .HasDatabaseName("IX_Motivations_Theme");

            // Query filters for soft delete
            builder.HasQueryFilter(m => !m.IsDeleted);
        }
    }
}
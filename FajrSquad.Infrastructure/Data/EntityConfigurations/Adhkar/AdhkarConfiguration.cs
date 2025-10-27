using FajrSquad.Core.Entities.Adhkar;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FajrSquad.Infrastructure.Data.EntityConfigurations.Adhkar
{
    public class AdhkarConfiguration : IEntityTypeConfiguration<FajrSquad.Core.Entities.Adhkar.Adhkar>
    {
        public void Configure(EntityTypeBuilder<FajrSquad.Core.Entities.Adhkar.Adhkar> e)
        {
            e.ToTable("adhkar");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Categories).HasColumnType("text[]");
            e.Property(x => x.ContentHash).IsRequired();
            e.HasMany(x => x.Texts).WithOne(x => x.Adhkar).HasForeignKey(x => x.AdhkarId);
            e.HasIndex(x => x.Categories).HasMethod("gin");
        }
    }
}

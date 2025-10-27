using FajrSquad.Core.Entities.Adhkar;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FajrSquad.Infrastructure.Data.EntityConfigurations.Adhkar
{
    public class AdhkarTextConfiguration : IEntityTypeConfiguration<AdhkarText>
    {
        public void Configure(EntityTypeBuilder<AdhkarText> e)
        {
            e.ToTable("adhkar_text");
            e.HasKey(x => new { x.AdhkarId, x.Lang });
            e.Property(x => x.Lang).HasMaxLength(10);
        }
    }
}

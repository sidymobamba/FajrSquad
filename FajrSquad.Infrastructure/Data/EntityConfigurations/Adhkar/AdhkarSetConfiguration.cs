using FajrSquad.Core.Entities.Adhkar;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FajrSquad.Infrastructure.Data.EntityConfigurations.Adhkar
{
    public class AdhkarSetConfiguration : IEntityTypeConfiguration<AdhkarSet>
    {
        public void Configure(EntityTypeBuilder<AdhkarSet> e)
        {
            e.ToTable("adhkar_set");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Type).IsRequired();
        }
    }
}

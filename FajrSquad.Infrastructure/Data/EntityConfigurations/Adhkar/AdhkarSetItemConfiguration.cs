using FajrSquad.Core.Entities.Adhkar;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FajrSquad.Infrastructure.Data.EntityConfigurations.Adhkar
{
    public class AdhkarSetItemConfiguration : IEntityTypeConfiguration<AdhkarSetItem>
    {
        public void Configure(EntityTypeBuilder<AdhkarSetItem> e)
        {
            e.ToTable("adhkar_set_item");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.SetId);
            e.HasOne(x => x.Set).WithMany(s => s.Items).HasForeignKey(x => x.SetId);
            e.HasOne(x => x.Adhkar).WithMany().HasForeignKey(x => x.AdhkarId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}

using FajrSquad.Core.Entities.Adhkar;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FajrSquad.Infrastructure.Data.EntityConfigurations.Adhkar
{
    public class UserAdhkarBookmarkConfiguration : IEntityTypeConfiguration<UserAdhkarBookmark>
    {
        public void Configure(EntityTypeBuilder<UserAdhkarBookmark> e)
        {
            e.ToTable("user_adhkar_bookmark");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.UserId, x.AdhkarId }).IsUnique();
        }
    }
}

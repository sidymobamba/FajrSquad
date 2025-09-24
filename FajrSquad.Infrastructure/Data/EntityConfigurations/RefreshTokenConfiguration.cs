using FajrSquad.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FajrSquad.Infrastructure.Data.EntityConfigurations
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> b)
        {
            b.ToTable("RefreshTokens");

            b.HasKey(x => x.Id);

            b.Property(x => x.Token)
                .IsRequired()
                .HasMaxLength(200);

            b.HasIndex(x => x.Token)
                .IsUnique()
                .HasDatabaseName("IX_RefreshTokens_Token");

            b.Property(x => x.UserId)
                .IsRequired();

            b.Property(x => x.Created)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            b.Property(x => x.Expires)
                .IsRequired();

            b.Property(x => x.CreatedByIp)
                .HasMaxLength(45);

            b.Property(x => x.RevokedByIp)
                .HasMaxLength(45);

            b.Property(x => x.ReplacedByToken)
                .HasMaxLength(200);

            // relazione 1-N con User
            b.HasOne(x => x.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

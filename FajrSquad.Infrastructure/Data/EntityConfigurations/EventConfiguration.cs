using FajrSquad.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FajrSquad.Infrastructure.Data.EntityConfigurations
{
    public class EventConfiguration : IEntityTypeConfiguration<Event>
    {
        public void Configure(EntityTypeBuilder<Event> builder)
        {
            builder.ToTable("Events");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(e => e.Description)
                .HasMaxLength(1000);

            builder.Property(e => e.Location)
                .HasMaxLength(255);

            builder.Property(e => e.StartDate)
                .IsRequired();

            builder.Property(e => e.Organizer)
                .HasMaxLength(200);

            builder.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // 👇 Seeding iniziale con valori UTC (statici)
            builder.HasData(
                new Event
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Title = "Incontro Fajr Squad - Milano",
                    Description = "Un momento insieme per rafforzare la fede e iniziare la giornata con Fajr in moschea.",
                    Location = "Moschea Centrale Milano",
                    StartDate = new DateTime(2025, 9, 10, 5, 30, 0, DateTimeKind.Utc),
                    EndDate = new DateTime(2025, 9, 10, 7, 0, 0, DateTimeKind.Utc),
                    Organizer = "Fajr Squad Italia",
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Event
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Title = "Escursione Spirituale",
                    Description = "Giornata di trekking e riflessione spirituale con i fratelli del gruppo.",
                    Location = "Monte Isola, Lago d’Iseo",
                    StartDate = new DateTime(2025, 9, 15, 8, 0, 0, DateTimeKind.Utc),
                    EndDate = new DateTime(2025, 9, 15, 18, 0, 0, DateTimeKind.Utc),
                    Organizer = "Fajr Squad Brescia",
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );
        }
    }
}

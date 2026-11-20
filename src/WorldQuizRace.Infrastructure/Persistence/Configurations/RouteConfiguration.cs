using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorldQuizRace.Domain.Entities;

namespace WorldQuizRace.Infrastructure.Persistence.Configurations;

internal sealed class RouteConfiguration : IEntityTypeConfiguration<Route>
{
    public void Configure(EntityTypeBuilder<Route> builder)
    {
        builder.ToTable("Routes", t =>
        {
            t.HasCheckConstraint("CK_Routes_TotalCheckpoints", $"[TotalCheckpoints] BETWEEN 1 AND {SchemaConstants.MaxRoutePosition}");
        });

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Code).IsRequired().HasMaxLength(Route.CodeMaxLength);
        builder.Property(r => r.Name).IsRequired().HasMaxLength(Route.NameMaxLength);
        builder.Property(r => r.Description).HasMaxLength(Route.DescriptionMaxLength);
        builder.Property(r => r.IsActive).HasDefaultValue(true);
        builder.HasIndex(r => r.Code).IsUnique();

        // Les checkpoints n'existent pas sans leur route.
        builder.HasMany(r => r.Checkpoints)
            .WithOne(c => c.Route)
            .HasForeignKey(c => c.RouteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

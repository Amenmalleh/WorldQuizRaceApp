using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorldQuizRace.Domain.Entities;

namespace WorldQuizRace.Infrastructure.Persistence.Configurations;

internal sealed class CheckpointConfiguration : IEntityTypeConfiguration<Checkpoint>
{
    public void Configure(EntityTypeBuilder<Checkpoint> builder)
    {
        builder.ToTable("Checkpoints", t =>
        {
            t.HasCheckConstraint("CK_Checkpoints_Position", $"[Position] BETWEEN 1 AND {SchemaConstants.MaxRoutePosition}");
            t.HasCheckConstraint("CK_Checkpoints_MapX", $"[MapX] BETWEEN 0 AND {Checkpoint.MapWidth}");
            t.HasCheckConstraint("CK_Checkpoints_MapY", $"[MapY] BETWEEN 0 AND {Checkpoint.MapHeight}");
        });

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(Checkpoint.NameMaxLength);
        builder.Property(c => c.Country).IsRequired().HasMaxLength(Checkpoint.CountryMaxLength);
        builder.Property(c => c.FunFact).IsRequired().HasMaxLength(Checkpoint.FunFactMaxLength);

        // One stop per position on a route: no silent duplicate.
        builder.HasIndex(c => new { c.RouteId, c.Position }).IsUnique();
    }
}

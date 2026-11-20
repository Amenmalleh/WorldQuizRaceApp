using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorldQuizRace.Domain.Entities;

namespace WorldQuizRace.Infrastructure.Persistence.Configurations;

internal sealed class AgeGroupConfiguration : IEntityTypeConfiguration<AgeGroup>
{
    public void Configure(EntityTypeBuilder<AgeGroup> builder)
    {
        builder.ToTable("AgeGroups", t =>
        {
            t.HasCheckConstraint("CK_AgeGroups_AgeRange",
                $"[MinimumAge] >= {Player.MinimumAge} AND [MaximumAge] <= {Player.MaximumAge} AND [MinimumAge] <= [MaximumAge]");
        });

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Name).IsRequired().HasMaxLength(AgeGroup.NameMaxLength);
        builder.HasIndex(a => a.Name).IsUnique();
        builder.HasIndex(a => a.MinimumAge).IsUnique();
    }
}

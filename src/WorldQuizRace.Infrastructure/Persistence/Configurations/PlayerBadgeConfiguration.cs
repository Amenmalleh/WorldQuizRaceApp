using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorldQuizRace.Domain.Entities;

namespace WorldQuizRace.Infrastructure.Persistence.Configurations;

internal sealed class PlayerBadgeConfiguration : IEntityTypeConfiguration<PlayerBadge>
{
    public void Configure(EntityTypeBuilder<PlayerBadge> builder)
    {
        builder.ToTable("PlayerBadges");
        builder.HasKey(pb => pb.Id);
        builder.HasIndex(pb => new { pb.PlayerId, pb.BadgeId }).IsUnique();

        builder.HasOne(pb => pb.Player)
            .WithMany(p => p.Badges)
            .HasForeignKey(pb => pb.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pb => pb.Badge)
            .WithMany(b => b.Awards)
            .HasForeignKey(pb => pb.BadgeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

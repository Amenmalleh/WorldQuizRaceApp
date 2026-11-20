using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorldQuizRace.Domain.Entities;

namespace WorldQuizRace.Infrastructure.Persistence.Configurations;

internal sealed class PlayerConfiguration : IEntityTypeConfiguration<Player>
{
    public void Configure(EntityTypeBuilder<Player> builder)
    {
        builder.ToTable("Players", t =>
        {
            t.HasCheckConstraint("CK_Players_Age", $"[Age] BETWEEN {Player.MinimumAge} AND {Player.MaximumAge}");
        });

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).IsRequired().HasMaxLength(Player.NameMaxLength);
        builder.Property(p => p.Avatar).HasMaxLength(Player.AvatarMaxLength);
        builder.Property(p => p.CreatedAt).IsRequired();

        // A referenced age group must never disappear together with its players.
        builder.HasOne(p => p.AgeGroup)
            .WithMany(a => a.Players)
            .HasForeignKey(p => p.AgeGroupId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

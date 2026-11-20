using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorldQuizRace.Domain.Entities;

namespace WorldQuizRace.Infrastructure.Persistence.Configurations;

internal sealed class MatchPlayerConfiguration : IEntityTypeConfiguration<MatchPlayer>
{
    public void Configure(EntityTypeBuilder<MatchPlayer> builder)
    {
        builder.ToTable("MatchPlayers", t =>
        {
            t.HasCheckConstraint("CK_MatchPlayers_CurrentPosition",
                $"[CurrentPosition] BETWEEN {MatchPlayer.StartPosition} AND {SchemaConstants.MaxRoutePosition}");
            t.HasCheckConstraint("CK_MatchPlayers_Counters",
                "[CorrectAnswers] >= 0 AND [WrongAnswers] >= 0 AND [Attempts] >= 0 AND [Score] >= 0 AND [Stars] >= 0 AND [Coins] >= 0");
            t.HasCheckConstraint("CK_MatchPlayers_AttemptsConsistency", "[CorrectAnswers] + [WrongAnswers] <= [Attempts]");
        });

        builder.HasKey(mp => mp.Id);
        builder.Property(mp => mp.Slot).HasConversion<string>().HasMaxLength(SchemaConstants.EnumMaxLength);
        builder.Property(mp => mp.RowVersion).IsRowVersion();

        // Un joueur ne participe qu'une fois à une partie : clé alternative ciblée par PlayerChallenge.
        builder.HasAlternateKey(mp => new { mp.MatchId, mp.PlayerId });
        // Un seul joueur A et un seul joueur B par partie.
        builder.HasIndex(mp => new { mp.MatchId, mp.Slot }).IsUnique();

        builder.HasOne(mp => mp.Player)
            .WithMany(p => p.MatchParticipations)
            .HasForeignKey(mp => mp.PlayerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(mp => mp.Route)
            .WithMany()
            .HasForeignKey(mp => mp.RouteId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

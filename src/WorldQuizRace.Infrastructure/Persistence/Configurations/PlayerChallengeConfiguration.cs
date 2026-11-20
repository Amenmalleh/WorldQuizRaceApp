using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorldQuizRace.Domain.Entities;
using WorldQuizRace.Domain.Enums;

namespace WorldQuizRace.Infrastructure.Persistence.Configurations;

internal sealed class PlayerChallengeConfiguration : IEntityTypeConfiguration<PlayerChallenge>
{
    public const string OnePendingPerPlayerIndexName = "UX_PlayerChallenges_OnePendingPerPlayer";

    public void Configure(EntityTypeBuilder<PlayerChallenge> builder)
    {
        builder.ToTable("PlayerChallenges", t =>
        {
            t.HasCheckConstraint("CK_PlayerChallenges_CheckpointPosition",
                $"[CheckpointPosition] BETWEEN 1 AND {SchemaConstants.MaxRoutePosition}");
            t.HasCheckConstraint("CK_PlayerChallenges_TurnNumber", "[TurnNumber] >= 1");
            // Une tentative en attente n'a ni résultat ni date de réponse ; une tentative close en a une.
            t.HasCheckConstraint("CK_PlayerChallenges_PendingState",
                $"([Status] = N'{AttemptStatus.Pending}' AND [IsCorrect] IS NULL AND [AnsweredAt] IS NULL) " +
                $"OR ([Status] <> N'{AttemptStatus.Pending}' AND [IsCorrect] IS NOT NULL AND [AnsweredAt] IS NOT NULL)");
        });

        builder.HasKey(pc => pc.Id);
        builder.Property(pc => pc.Status).HasConversion<string>().HasMaxLength(SchemaConstants.EnumMaxLength);
        builder.Property(pc => pc.Answer).HasMaxLength(PlayerChallenge.AnswerMaxLength);

        // FK composite vers la participation : impossible d'enregistrer une tentative pour un joueur hors de la partie.
        // Cascade : Matches -> MatchPlayers -> PlayerChallenges est l'unique chemin de cascade.
        builder.HasOne(pc => pc.MatchPlayer)
            .WithMany(mp => mp.ChallengeAttempts)
            .HasForeignKey(pc => new { pc.MatchId, pc.PlayerId })
            .HasPrincipalKey(mp => new { mp.MatchId, mp.PlayerId })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pc => pc.Challenge)
            .WithMany(c => c.Attempts)
            .HasForeignKey(pc => pc.ChallengeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(pc => new { pc.MatchId, pc.PlayerId }, "IX_PlayerChallenges_MatchId_PlayerId");

        // Au plus UN défi en attente par joueur et par partie : bloque les doubles soumissions au niveau SQL.
        builder.HasIndex(pc => new { pc.MatchId, pc.PlayerId }, OnePendingPerPlayerIndexName)
            .IsUnique()
            .HasFilter($"[Status] = N'{AttemptStatus.Pending}'");
    }
}

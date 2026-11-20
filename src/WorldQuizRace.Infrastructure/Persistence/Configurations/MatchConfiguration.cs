using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorldQuizRace.Domain.Entities;

namespace WorldQuizRace.Infrastructure.Persistence.Configurations;

internal sealed class MatchConfiguration : IEntityTypeConfiguration<Match>
{
    public void Configure(EntityTypeBuilder<Match> builder)
    {
        builder.ToTable("Matches", t =>
        {
            t.HasCheckConstraint("CK_Matches_TurnNumber", "[TurnNumber] >= 0");
            t.HasCheckConstraint("CK_Matches_EndDate", "[EndDate] IS NULL OR [EndDate] >= [StartDate]");
        });

        builder.HasKey(m => m.Id);
        builder.Property(m => m.TurnMode).HasConversion<string>().HasMaxLength(SchemaConstants.EnumMaxLength);
        builder.Property(m => m.Status).HasConversion<string>().HasMaxLength(SchemaConstants.EnumMaxLength);
        builder.Property(m => m.RowVersion).IsRowVersion();
        builder.HasIndex(m => m.Status);

        // Restrict (NO ACTION): a player cannot be deleted while a match references it,
        // which also avoids multiple Players -> Matches cascade paths rejected by SQL Server.
        builder.HasOne(m => m.CurrentTurnPlayer)
            .WithMany()
            .HasForeignKey(m => m.CurrentTurnPlayerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Winner)
            .WithMany()
            .HasForeignKey(m => m.WinnerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Deleting a match deletes its participations (and, by cascade, their attempts).
        builder.HasMany(m => m.Players)
            .WithOne(mp => mp.Match)
            .HasForeignKey(mp => mp.MatchId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

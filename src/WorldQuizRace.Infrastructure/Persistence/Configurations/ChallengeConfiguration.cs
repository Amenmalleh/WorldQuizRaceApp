using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorldQuizRace.Domain.Entities;

namespace WorldQuizRace.Infrastructure.Persistence.Configurations;

internal sealed class ChallengeConfiguration : IEntityTypeConfiguration<Challenge>
{
    public void Configure(EntityTypeBuilder<Challenge> builder)
    {
        builder.ToTable("Challenges", t =>
        {
            t.HasCheckConstraint("CK_Challenges_TimeLimit", "[TimeLimitSeconds] IS NULL OR [TimeLimitSeconds] BETWEEN 5 AND 600");
            t.HasCheckConstraint("CK_Challenges_ContentIsJson", "ISJSON([Content]) = 1");
            t.HasCheckConstraint("CK_Challenges_CorrectAnswerIsJson", "ISJSON([CorrectAnswer]) = 1");
        });

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Code).IsRequired().HasMaxLength(Challenge.CodeMaxLength);
        builder.Property(c => c.Title).IsRequired().HasMaxLength(Challenge.TitleMaxLength);
        builder.Property(c => c.Description).HasMaxLength(Challenge.DescriptionMaxLength);
        builder.Property(c => c.Type).HasConversion<string>().HasMaxLength(SchemaConstants.EnumMaxLength);
        builder.Property(c => c.Difficulty).HasConversion<string>().HasMaxLength(SchemaConstants.EnumMaxLength);
        builder.Property(c => c.Content).IsRequired();
        builder.Property(c => c.CorrectAnswer).IsRequired();
        builder.Property(c => c.Explanation).IsRequired().HasMaxLength(Challenge.ExplanationMaxLength);
        builder.Property(c => c.IsActive).HasDefaultValue(true);

        builder.HasIndex(c => c.Code).IsUnique();
        // Index de la requête de sélection : défis actifs d'une tranche et d'une difficulté.
        builder.HasIndex(c => new { c.AgeGroupId, c.Difficulty, c.IsActive });

        builder.HasOne(c => c.AgeGroup)
            .WithMany(a => a.Challenges)
            .HasForeignKey(c => c.AgeGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Category)
            .WithMany(cat => cat.Challenges)
            .HasForeignKey(c => c.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

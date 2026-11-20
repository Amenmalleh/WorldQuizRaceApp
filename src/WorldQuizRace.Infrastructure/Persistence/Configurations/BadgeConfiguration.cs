using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorldQuizRace.Domain.Entities;

namespace WorldQuizRace.Infrastructure.Persistence.Configurations;

internal sealed class BadgeConfiguration : IEntityTypeConfiguration<Badge>
{
    public void Configure(EntityTypeBuilder<Badge> builder)
    {
        builder.ToTable("Badges");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Code).IsRequired().HasMaxLength(Badge.CodeMaxLength);
        builder.Property(b => b.Name).IsRequired().HasMaxLength(Badge.NameMaxLength);
        builder.Property(b => b.Description).IsRequired().HasMaxLength(Badge.DescriptionMaxLength);
        builder.Property(b => b.Icon).IsRequired().HasMaxLength(Badge.IconMaxLength);
        builder.HasIndex(b => b.Code).IsUnique();
    }
}

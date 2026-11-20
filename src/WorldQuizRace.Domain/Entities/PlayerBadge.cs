namespace WorldQuizRace.Domain.Entities;

/// <summary>Badge earned by a player (at most once per badge).</summary>
public class PlayerBadge
{
    public int Id { get; set; }

    public int PlayerId { get; set; }

    public Player Player { get; set; } = null!;

    public int BadgeId { get; set; }

    public Badge Badge { get; set; } = null!;

    public DateTimeOffset AwardedAt { get; set; }
}

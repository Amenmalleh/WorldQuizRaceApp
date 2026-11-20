namespace WorldQuizRace.Domain.Entities;

/// <summary>
/// Player identified only by a nickname and an age: no unnecessary personal data (children audience).
/// </summary>
public class Player
{
    public const int NameMaxLength = 20;
    public const int AvatarMaxLength = 32;
    public const int MinimumAge = 5;
    public const int MaximumAge = 15;

    public int Id { get; set; }

    /// <summary>Nickname (no real name required).</summary>
    public string Name { get; set; } = string.Empty;

    public int Age { get; set; }

    public int AgeGroupId { get; set; }

    public AgeGroup AgeGroup { get; set; } = null!;

    /// <summary>Key of the chosen avatar (e.g. "fox"), optional.</summary>
    public string? Avatar { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<MatchPlayer> MatchParticipations { get; set; } = [];

    public ICollection<PlayerBadge> Badges { get; set; } = [];
}

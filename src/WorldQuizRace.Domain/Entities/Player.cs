namespace WorldQuizRace.Domain.Entities;

/// <summary>
/// Joueur identifié par un pseudo et un âge uniquement : aucune donnée personnelle superflue (public enfant).
/// </summary>
public class Player
{
    public const int NameMaxLength = 20;
    public const int AvatarMaxLength = 32;
    public const int MinimumAge = 5;
    public const int MaximumAge = 15;

    public int Id { get; set; }

    /// <summary>Pseudo (pas de nom réel exigé).</summary>
    public string Name { get; set; } = string.Empty;

    public int Age { get; set; }

    public int AgeGroupId { get; set; }

    public AgeGroup AgeGroup { get; set; } = null!;

    /// <summary>Clé de l'avatar choisi (ex. "fox"), optionnelle.</summary>
    public string? Avatar { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<MatchPlayer> MatchParticipations { get; set; } = [];

    public ICollection<PlayerBadge> Badges { get; set; } = [];
}

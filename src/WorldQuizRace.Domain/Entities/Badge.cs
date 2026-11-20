namespace WorldQuizRace.Domain.Entities;

/// <summary>Récompense déblocable (système extensible : étoiles et pièces sont portées par MatchPlayer).</summary>
public class Badge
{
    public const int CodeMaxLength = 30;
    public const int NameMaxLength = 50;
    public const int DescriptionMaxLength = 200;
    public const int IconMaxLength = 16;

    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>Emoji ou clé d'icône.</summary>
    public string Icon { get; set; } = string.Empty;

    public ICollection<PlayerBadge> Awards { get; set; } = [];
}

namespace WorldQuizRace.Domain.Entities;

/// <summary>Tranche d'âge (5–7, 8–10, 11–12, 13–15) qui détermine le catalogue de défis d'un joueur.</summary>
public class AgeGroup
{
    public const int NameMaxLength = 30;

    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int MinimumAge { get; set; }

    public int MaximumAge { get; set; }

    public ICollection<Player> Players { get; set; } = [];

    public ICollection<Challenge> Challenges { get; set; } = [];
}

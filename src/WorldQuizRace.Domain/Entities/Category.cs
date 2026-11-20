namespace WorldQuizRace.Domain.Entities;

/// <summary>Educational topic of a challenge (Mathematics, Geography…).</summary>
public class Category
{
    public const int NameMaxLength = 50;
    public const int DescriptionMaxLength = 200;

    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ICollection<Challenge> Challenges { get; set; } = [];
}

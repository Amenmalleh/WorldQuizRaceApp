namespace WorldQuizRace.Domain.Entities;

/// <summary>Stop on a route. Position 1..N; position N (IsFinal) is the shared final destination.</summary>
public class Checkpoint
{
    public const int NameMaxLength = 60;
    public const int CountryMaxLength = 60;
    public const int FunFactMaxLength = 300;

    /// <summary>Size of the map SVG viewBox (equirectangular projection).</summary>
    public const double MapWidth = 1000;
    public const double MapHeight = 500;

    public int Id { get; set; }

    public int RouteId { get; set; }

    public Route Route { get; set; } = null!;

    public int Position { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;

    /// <summary>Short child-friendly fun fact.</summary>
    public string FunFact { get; set; } = string.Empty;

    /// <summary>X coordinate in the 0..1000 viewBox.</summary>
    public double MapX { get; set; }

    /// <summary>Y coordinate in the 0..500 viewBox.</summary>
    public double MapY { get; set; }

    public bool IsFinal { get; set; }
}

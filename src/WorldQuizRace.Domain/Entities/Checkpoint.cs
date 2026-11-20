namespace WorldQuizRace.Domain.Entities;

/// <summary>Étape d'une route. Position 1..N ; la position N (IsFinal) est la destination commune.</summary>
public class Checkpoint
{
    public const int NameMaxLength = 60;
    public const int CountryMaxLength = 60;
    public const int FunFactMaxLength = 300;

    /// <summary>Dimensions du viewBox SVG de la carte (projection équirectangulaire).</summary>
    public const double MapWidth = 1000;
    public const double MapHeight = 500;

    public int Id { get; set; }

    public int RouteId { get; set; }

    public Route Route { get; set; } = null!;

    public int Position { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;

    /// <summary>Anecdote courte adaptée aux enfants.</summary>
    public string FunFact { get; set; } = string.Empty;

    /// <summary>Abscisse dans le viewBox 0..1000.</summary>
    public double MapX { get; set; }

    /// <summary>Ordonnée dans le viewBox 0..500.</summary>
    public double MapY { get; set; }

    public bool IsFinal { get; set; }
}

using WorldQuizRace.Domain.Entities;

namespace WorldQuizRace.Domain.Map;

/// <summary>
/// Equirectangular projection of geographic coordinates onto the map SVG viewBox
/// (<see cref="Checkpoint.MapWidth"/> × <see cref="Checkpoint.MapHeight"/>, origin at the top left).
/// </summary>
public static class MapProjection
{
    public static (double X, double Y) ToMap(double latitude, double longitude)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(latitude, -90);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(latitude, 90);
        ArgumentOutOfRangeException.ThrowIfLessThan(longitude, -180);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(longitude, 180);

        var x = (longitude + 180) / 360 * Checkpoint.MapWidth;
        var y = (90 - latitude) / 180 * Checkpoint.MapHeight;
        return (Math.Round(x, 1), Math.Round(y, 1));
    }
}

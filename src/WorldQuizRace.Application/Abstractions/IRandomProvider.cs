namespace WorldQuizRace.Application.Abstractions;

/// <summary>Source of randomness, injectable so that tests can be deterministic.</summary>
public interface IRandomProvider
{
    /// <summary>Returns a value in [0, maxExclusive).</summary>
    int Next(int maxExclusive);
}

/// <summary>Production implementation backed by the thread-safe shared generator.</summary>
public sealed class SystemRandomProvider : IRandomProvider
{
    public int Next(int maxExclusive) => Random.Shared.Next(maxExclusive);
}

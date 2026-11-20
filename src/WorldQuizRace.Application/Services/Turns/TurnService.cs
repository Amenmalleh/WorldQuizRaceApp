using WorldQuizRace.Domain.Entities;
using WorldQuizRace.Domain.Enums;

namespace WorldQuizRace.Application.Services.Turns;

/// <summary>Strategy describing one turn mode. Add a class to support a new <see cref="TurnMode"/>.</summary>
public interface ITurnStrategy
{
    TurnMode Mode { get; }

    bool IsPlayersTurn(Match match, int playerId);

    /// <summary>Player who plays after the current one.</summary>
    int NextPlayerId(Match match);
}

/// <summary>A → B → A → …: the turn passes after every answer, correct or not.</summary>
public sealed class AlternatingTurnStrategy : ITurnStrategy
{
    public TurnMode Mode => TurnMode.Alternating;

    public bool IsPlayersTurn(Match match, int playerId) => match.CurrentTurnPlayerId == playerId;

    public int NextPlayerId(Match match) =>
        match.Players.Single(p => p.PlayerId != match.CurrentTurnPlayerId).PlayerId;
}

public interface ITurnService
{
    bool IsPlayersTurn(Match match, int playerId);

    /// <summary>Gives the turn to the next player and increments the turn number. Requires match.Players to be loaded.</summary>
    void AdvanceTurn(Match match);
}

public sealed class TurnService : ITurnService
{
    private readonly IReadOnlyDictionary<TurnMode, ITurnStrategy> _strategies;

    public TurnService(IEnumerable<ITurnStrategy> strategies) =>
        _strategies = strategies.ToDictionary(s => s.Mode);

    public bool IsPlayersTurn(Match match, int playerId) => Strategy(match).IsPlayersTurn(match, playerId);

    public void AdvanceTurn(Match match)
    {
        if (match.Players.Count != 2)
        {
            throw new InvalidOperationException("The match players must be loaded to advance the turn.");
        }

        match.CurrentTurnPlayerId = Strategy(match).NextPlayerId(match);
        match.TurnNumber++;
    }

    private ITurnStrategy Strategy(Match match) =>
        _strategies.TryGetValue(match.TurnMode, out var strategy)
            ? strategy
            : throw new InvalidOperationException($"No turn strategy is registered for {match.TurnMode}.");
}

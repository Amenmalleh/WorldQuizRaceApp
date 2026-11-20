namespace WorldQuizRace.Application.Exceptions;

/// <summary>
/// Base class of the expected, business-level errors. The Web layer turns them into ProblemDetails
/// responses (400/404/409); they must never surface as HTTP 500.
/// The message is user-facing (French, child-friendly); <see cref="Code"/> is stable for the JavaScript client.
/// </summary>
public abstract class GameException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

/// <summary>Invalid input data (HTTP 400). <see cref="Errors"/> lists messages per field.</summary>
public sealed class GameValidationException(IReadOnlyDictionary<string, string[]> errors)
    : GameException(GameErrorCodes.ValidationFailed, "Certaines informations ne sont pas valides.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;

    public GameValidationException(string field, string error)
        : this(new Dictionary<string, string[]> { [field] = [error] })
    {
    }
}

/// <summary>Unknown match, player or attempt (HTTP 404).</summary>
public sealed class GameNotFoundException(string code, string message) : GameException(code, message);

/// <summary>A game rule forbids the action in the current state (HTTP 409).</summary>
public sealed class GameRuleException(string code, string message) : GameException(code, message);

/// <summary>Another request modified the match at the same time (HTTP 409); the client should reload the state.</summary>
public sealed class GameConcurrencyException()
    : GameException(GameErrorCodes.ConcurrentUpdate, "La partie vient d'être modifiée. Recharge l'état du jeu.");

/// <summary>Stable error codes returned to the client in ProblemDetails.extensions["code"].</summary>
public static class GameErrorCodes
{
    public const string ValidationFailed = "validation_failed";
    public const string MatchNotFound = "match_not_found";
    public const string PlayerNotFound = "player_not_found";
    public const string PlayerNotInMatch = "player_not_in_match";
    public const string AttemptNotFound = "attempt_not_found";
    public const string MatchNotInProgress = "match_not_in_progress";
    public const string MatchNotFinished = "match_not_finished";
    public const string NotYourTurn = "not_your_turn";
    public const string AttemptAlreadyAnswered = "attempt_already_answered";
    public const string StaleAttempt = "stale_attempt";
    public const string SamePlayerTwice = "same_player_twice";
    public const string PlayerAlreadyInMatch = "player_already_in_match";
    public const string RouteUnavailable = "route_unavailable";
    public const string NoChallengeAvailable = "no_challenge_available";
    public const string ConcurrentUpdate = "concurrent_update";
}

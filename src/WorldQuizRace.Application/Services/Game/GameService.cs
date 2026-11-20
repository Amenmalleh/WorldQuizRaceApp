using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WorldQuizRace.Application.Abstractions;
using WorldQuizRace.Application.Answers;
using WorldQuizRace.Application.Dtos;
using WorldQuizRace.Application.Exceptions;
using WorldQuizRace.Application.Options;
using WorldQuizRace.Application.Services.Challenges;
using WorldQuizRace.Application.Services.Matches;
using WorldQuizRace.Application.Services.Players;
using WorldQuizRace.Application.Services.Progression;
using WorldQuizRace.Application.Services.Rewards;
using WorldQuizRace.Application.Services.Turns;
using WorldQuizRace.Domain.Entities;
using WorldQuizRace.Domain.Enums;

namespace WorldQuizRace.Application.Services.Game;

/// <summary>Use cases called by the controllers. Every rule is enforced here, on the server.</summary>
public interface IGameService
{
    /// <summary>Registers both players and creates their match.</summary>
    Task<MatchStateDto> CreateMatchAsync(CreateMatchRequest request, CancellationToken cancellationToken = default);

    Task<MatchStateDto> GetStateAsync(int matchId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Challenge of the player whose turn it is. Reloading returns the same pending attempt:
    /// a player cannot reroll a challenge.
    /// </summary>
    Task<ChallengeDto> GetCurrentChallengeAsync(int matchId, int playerId, CancellationToken cancellationToken = default);

    /// <summary>Validates the answer, moves the player, rewards, passes the turn or ends the match — atomically.</summary>
    Task<AnswerResultDto> SubmitAnswerAsync(SubmitAnswerRequest request, CancellationToken cancellationToken = default);

    Task<MatchResultDto> GetResultAsync(int matchId, CancellationToken cancellationToken = default);
}

public sealed class GameService(
    IApplicationDbContext db,
    IPlayerRegistrationService registration,
    IMatchService matches,
    IChallengeSelectionService selection,
    IAnswerValidationService answers,
    ITurnService turns,
    IProgressionService progression,
    IWinnerService winners,
    IRewardService rewards,
    IMatchStateQuery stateQuery,
    IMatchResultService results,
    IOptions<GameSettings> options,
    TimeProvider time,
    ILogger<GameService> logger) : IGameService
{
    public async Task<MatchStateDto> CreateMatchAsync(CreateMatchRequest request, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // Both registrations are validated so that the screen can show every error at once.
        var errors = new Dictionary<string, string[]>();
        var playerA = await TryRegisterAsync(request.PlayerA, "playerA", errors, cancellationToken);
        var playerB = await TryRegisterAsync(request.PlayerB, "playerB", errors, cancellationToken);
        if (errors.Count > 0 || playerA is null || playerB is null)
        {
            throw new GameValidationException(errors);
        }

        var matchId = await matches.CreateMatchAsync(playerA.Id, playerB.Id, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation("Match {MatchId} created for players {PlayerA} and {PlayerB}.", matchId, playerA.Id, playerB.Id);
        return await stateQuery.GetAsync(matchId, cancellationToken);
    }

    public Task<MatchStateDto> GetStateAsync(int matchId, CancellationToken cancellationToken = default) =>
        stateQuery.GetAsync(matchId, cancellationToken);

    public async Task<ChallengeDto> GetCurrentChallengeAsync(int matchId, int playerId, CancellationToken cancellationToken = default)
    {
        var match = await LoadMatchAsync(matchId, cancellationToken);
        EnsureInProgress(match);
        var participant = match.Players.SingleOrDefault(p => p.PlayerId == playerId)
                          ?? throw new GameNotFoundException(GameErrorCodes.PlayerNotInMatch, "Ce joueur ne participe pas à cette partie.");
        EnsurePlayersTurn(match, playerId);

        var pending = await FindPendingAsync(matchId, playerId, cancellationToken);
        if (pending is not null)
        {
            return ToDto(pending);
        }

        var chosen = await selection.SelectAsync(matchId, playerId, participant.Player.AgeGroupId, participant.CurrentPosition + 1, cancellationToken);
        pending = new PlayerChallenge
        {
            MatchId = matchId,
            PlayerId = playerId,
            ChallengeId = chosen.Challenge.Id,
            Challenge = chosen.Challenge,
            CheckpointPosition = participant.CurrentPosition + 1,
            TurnNumber = match.TurnNumber,
            Status = AttemptStatus.Pending,
            ServedAt = time.GetUtcNow()
        };
        db.PlayerChallenges.Add(pending);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Two simultaneous requests: the filtered unique index kept only one pending attempt. Return it.
            db.PlayerChallenges.Entry(pending).State = EntityState.Detached;
            pending = await FindPendingAsync(matchId, playerId, cancellationToken) ?? throw new GameConcurrencyException();
        }

        return ToDto(pending);
    }

    public async Task<AnswerResultDto> SubmitAnswerAsync(SubmitAnswerRequest request, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var match = await LoadMatchAsync(request.MatchId, cancellationToken);
        var attempt = await db.PlayerChallenges
                          .Include(pc => pc.Challenge)
                          .SingleOrDefaultAsync(pc => pc.Id == request.PlayerChallengeId && pc.MatchId == match.Id, cancellationToken)
                      ?? throw new GameNotFoundException(GameErrorCodes.AttemptNotFound, "Ce défi n'existe pas dans cette partie.");

        EnsureInProgress(match);
        if (attempt.Status != AttemptStatus.Pending)
        {
            throw new GameRuleException(GameErrorCodes.AttemptAlreadyAnswered, "Tu as déjà répondu à ce défi.");
        }

        EnsurePlayersTurn(match, attempt.PlayerId);
        if (attempt.TurnNumber != match.TurnNumber)
        {
            throw new GameRuleException(GameErrorCodes.StaleAttempt, "Ce défi n'est plus celui du tour en cours.");
        }

        var settings = options.Value;
        var participant = match.Players.Single(p => p.PlayerId == attempt.PlayerId);
        var totalCheckpoints = participant.Route.TotalCheckpoints;
        var timeLimit = settings.TimeLimitFor(attempt.Challenge);
        var now = time.GetUtcNow();

        var timedOut = now > attempt.ServedAt.AddSeconds(timeLimit + settings.AnswerGracePeriodSeconds);
        var isCorrect = !timedOut && answers.Validate(attempt.Challenge.Type, attempt.Challenge.Content, attempt.Challenge.CorrectAnswer, request.Answer);

        attempt.Status = timedOut ? AttemptStatus.TimedOut : AttemptStatus.Answered;
        attempt.IsCorrect = isCorrect;
        attempt.AnsweredAt = now;
        attempt.Answer = SerializeAnswer(request.Answer);

        var moved = progression.Apply(participant, isCorrect, totalCheckpoints);
        var victory = winners.TryDeclareWinner(match, participant, totalCheckpoints, now);
        if (!victory)
        {
            turns.AdvanceTurn(match);
        }

        var reward = await rewards.ApplyAsync(new RewardContext(match, participant, attempt, isCorrect, timeLimit, totalCheckpoints), cancellationToken);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            // DbUpdateConcurrencyException (RowVersion) or a unique index: another request won the race.
            logger.LogWarning(ex, "Concurrent submission rejected for match {MatchId}, attempt {AttemptId}.", match.Id, attempt.Id);
            throw new GameConcurrencyException();
        }

        logger.LogInformation(
            "Match {MatchId}: player {PlayerId} answered attempt {AttemptId} ({Outcome}), position {From} -> {To}.",
            match.Id, participant.PlayerId, attempt.Id, timedOut ? "timed out" : isCorrect ? "correct" : "wrong", moved.PreviousPosition, moved.NewPosition);

        var state = await stateQuery.GetAsync(match.Id, cancellationToken);
        return new AnswerResultDto(attempt.Id, isCorrect, timedOut, attempt.Challenge.Explanation,
            moved.PreviousPosition, moved.NewPosition, victory, reward, state);
    }

    public Task<MatchResultDto> GetResultAsync(int matchId, CancellationToken cancellationToken = default) =>
        results.GetResultAsync(matchId, cancellationToken);

    private async Task<PlayerDto?> TryRegisterAsync(RegisterPlayerRequest? request, string prefix, Dictionary<string, string[]> errors, CancellationToken ct)
    {
        if (request is null)
        {
            errors[prefix] = ["Les informations du joueur sont obligatoires."];
            return null;
        }

        try
        {
            return await registration.RegisterAsync(request, ct);
        }
        catch (GameValidationException ex)
        {
            foreach (var (field, messages) in ex.Errors)
            {
                errors[$"{prefix}.{field}"] = messages;
            }

            return null;
        }
    }

    private async Task<Match> LoadMatchAsync(int matchId, CancellationToken ct) =>
        await db.Matches
            .Include(m => m.Players).ThenInclude(mp => mp.Player)
            .Include(m => m.Players).ThenInclude(mp => mp.Route)
            .SingleOrDefaultAsync(m => m.Id == matchId, ct)
        ?? throw new GameNotFoundException(GameErrorCodes.MatchNotFound, "Partie introuvable.");

    private Task<PlayerChallenge?> FindPendingAsync(int matchId, int playerId, CancellationToken ct) =>
        db.PlayerChallenges
            .Include(pc => pc.Challenge).ThenInclude(c => c.Category)
            .SingleOrDefaultAsync(pc => pc.MatchId == matchId && pc.PlayerId == playerId && pc.Status == AttemptStatus.Pending, ct);

    private static void EnsureInProgress(Match match)
    {
        if (match.Status != MatchStatus.InProgress)
        {
            throw new GameRuleException(GameErrorCodes.MatchNotInProgress, "La partie est terminée.");
        }
    }

    private void EnsurePlayersTurn(Match match, int playerId)
    {
        if (!turns.IsPlayersTurn(match, playerId))
        {
            throw new GameRuleException(GameErrorCodes.NotYourTurn, "Ce n'est pas ton tour : attends que l'autre joueur ait répondu.");
        }
    }

    private ChallengeDto ToDto(PlayerChallenge attempt)
    {
        var challenge = attempt.Challenge;
        var timeLimit = options.Value.TimeLimitFor(challenge);
        var expiresAt = attempt.ServedAt.AddSeconds(timeLimit);
        var remaining = (int)Math.Max(0, Math.Ceiling((expiresAt - time.GetUtcNow()).TotalSeconds));

        using var content = JsonDocument.Parse(challenge.Content);
        var publicContent = content.RootElement.Clone();
        var prompt = AnswerReader.String(publicContent, "prompt") ?? challenge.Title;

        // Only the public content is exposed: CorrectAnswer and Explanation never leave the server here.
        return new ChallengeDto(attempt.Id, attempt.MatchId, attempt.PlayerId, challenge.Type, challenge.Difficulty,
            challenge.Category.Name, challenge.Title, prompt, publicContent, attempt.CheckpointPosition,
            timeLimit, attempt.ServedAt, expiresAt, remaining);
    }

    /// <summary>Raw answer kept for history; answers longer than the column are judged but not stored.</summary>
    private static string? SerializeAnswer(JsonElement answer)
    {
        if (answer.ValueKind == JsonValueKind.Undefined)
        {
            return null;
        }

        var raw = answer.GetRawText();
        return raw.Length <= PlayerChallenge.AnswerMaxLength ? raw : null;
    }
}

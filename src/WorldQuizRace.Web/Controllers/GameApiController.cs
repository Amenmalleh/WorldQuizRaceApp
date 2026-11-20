using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using WorldQuizRace.Application.Dtos;
using WorldQuizRace.Application.Services.Game;
using WorldQuizRace.Web.Infrastructure;

namespace WorldQuizRace.Web.Controllers;

/// <summary>
/// JSON API used by the game pages. Thin by design: every rule lives in <see cref="IGameService"/>.
/// POST requests require the anti-forgery token in the X-CSRF-TOKEN header (see GET /api/game/antiforgery).
/// </summary>
[ApiController]
[Route("api/game")]
[Produces("application/json")]
[AutoValidateAntiforgeryToken]
[TypeFilter<GameExceptionFilter>]
public sealed class GameApiController(IGameService game, IAntiforgery antiforgery) : ControllerBase
{
    public sealed record AntiforgeryTokenDto(string HeaderName, string Token);

    /// <summary>Issues the anti-forgery cookie and returns the token to send in the request header.</summary>
    [HttpGet("antiforgery")]
    public ActionResult<AntiforgeryTokenDto> GetAntiforgeryToken()
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        return new AntiforgeryTokenDto(tokens.HeaderName!, tokens.RequestToken!);
    }

    /// <summary>Registers both players and creates their match.</summary>
    [HttpPost("matches")]
    [ProducesResponseType<MatchStateDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<MatchStateDto>> CreateMatch(CreateMatchRequest request, CancellationToken cancellationToken)
    {
        var state = await game.CreateMatchAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetState), new { matchId = state.MatchId }, state);
    }

    [HttpGet("matches/{matchId:int}")]
    public Task<MatchStateDto> GetState(int matchId, CancellationToken cancellationToken) =>
        game.GetStateAsync(matchId, cancellationToken);

    /// <summary>Current challenge of a player; only the player whose turn it is gets one. Idempotent: reloading returns the same attempt.</summary>
    [HttpGet("matches/{matchId:int}/players/{playerId:int}/challenge")]
    public Task<ChallengeDto> GetCurrentChallenge(int matchId, int playerId, CancellationToken cancellationToken) =>
        game.GetCurrentChallengeAsync(matchId, playerId, cancellationToken);

    [HttpPost("matches/{matchId:int}/answers")]
    public async Task<ActionResult<AnswerResultDto>> SubmitAnswer(int matchId, SubmitAnswerRequest request, CancellationToken cancellationToken)
    {
        if (request.MatchId != matchId)
        {
            ModelState.AddModelError(nameof(request.MatchId), "Le numéro de partie ne correspond pas à l'adresse.");
            return ValidationProblem(ModelState);
        }

        return await game.SubmitAnswerAsync(request, cancellationToken);
    }

    [HttpGet("matches/{matchId:int}/result")]
    public Task<MatchResultDto> GetResult(int matchId, CancellationToken cancellationToken) =>
        game.GetResultAsync(matchId, cancellationToken);
}

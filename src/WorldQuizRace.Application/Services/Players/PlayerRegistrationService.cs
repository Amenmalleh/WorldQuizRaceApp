using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using WorldQuizRace.Application.Abstractions;
using WorldQuizRace.Application.Dtos;
using WorldQuizRace.Application.Exceptions;
using WorldQuizRace.Application.Options;
using WorldQuizRace.Domain.Entities;

namespace WorldQuizRace.Application.Services.Players;

public interface IPlayerRegistrationService
{
    /// <summary>Validates and stores a player (nickname, age, optional avatar). Collects only what the game needs.</summary>
    Task<PlayerDto> RegisterAsync(RegisterPlayerRequest request, CancellationToken cancellationToken = default);
}

public sealed partial class PlayerRegistrationService(
    IApplicationDbContext db,
    IAgeGroupService ageGroups,
    IOptions<GameSettings> settings,
    TimeProvider time) : IPlayerRegistrationService
{
    public const int NameMinLength = 2;
    public const string NameField = "name";
    public const string AvatarField = "avatar";

    public async Task<PlayerDto> RegisterAsync(RegisterPlayerRequest request, CancellationToken cancellationToken = default)
    {
        var errors = new Dictionary<string, string[]>();

        var name = NormalizeName(request.Name);
        if (name.Length is < NameMinLength or > Player.NameMaxLength)
        {
            errors[NameField] = [$"Le pseudo doit contenir entre {NameMinLength} et {Player.NameMaxLength} caractères."];
        }
        else if (!AllowedNameCharacters().IsMatch(name))
        {
            errors[NameField] = ["Le pseudo ne peut contenir que des lettres, des chiffres, des espaces et les signes - _ ' ."];
        }

        var avatar = string.IsNullOrWhiteSpace(request.Avatar) ? null : request.Avatar.Trim().ToLowerInvariant();
        if (avatar is not null && !settings.Value.AllowedAvatars.Contains(avatar, StringComparer.OrdinalIgnoreCase))
        {
            errors[AvatarField] = ["Cet avatar n'existe pas."];
        }

        AgeGroupDto? ageGroup = null;
        try
        {
            ageGroup = await ageGroups.GetForAgeAsync(request.Age, cancellationToken);
        }
        catch (GameValidationException ex)
        {
            foreach (var (field, messages) in ex.Errors)
            {
                errors[field] = messages;
            }
        }

        if (errors.Count > 0 || ageGroup is null)
        {
            throw new GameValidationException(errors);
        }

        var player = new Player
        {
            Name = name,
            Age = request.Age,
            AgeGroupId = ageGroup.Id,
            Avatar = avatar,
            CreatedAt = time.GetUtcNow()
        };
        db.Players.Add(player);
        await db.SaveChangesAsync(cancellationToken);

        return new PlayerDto(player.Id, player.Name, player.Age, ageGroup.Id, ageGroup.Name, player.Avatar);
    }

    /// <summary>Trims the nickname and collapses inner whitespace ("  Ad   am " → "Ad am").</summary>
    public static string NormalizeName(string? name) =>
        string.Join(' ', (name ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    [GeneratedRegex(@"^[\p{L}\p{N} _'.\-]+$")]
    private static partial Regex AllowedNameCharacters();
}

using Microsoft.EntityFrameworkCore;
using WorldQuizRace.Application.Abstractions;
using WorldQuizRace.Application.Dtos;
using WorldQuizRace.Application.Exceptions;
using WorldQuizRace.Domain.Entities;

namespace WorldQuizRace.Application.Services.Players;

public interface IAgeGroupService
{
    /// <summary>Returns the age group of an age; throws <see cref="GameValidationException"/> outside 5–15.</summary>
    Task<AgeGroupDto> GetForAgeAsync(int age, CancellationToken cancellationToken = default);
}

public sealed class AgeGroupService(IApplicationDbContext db) : IAgeGroupService
{
    public const string AgeField = "age";

    public async Task<AgeGroupDto> GetForAgeAsync(int age, CancellationToken cancellationToken = default)
    {
        if (age is < Player.MinimumAge or > Player.MaximumAge)
        {
            throw new GameValidationException(AgeField, $"L'âge doit être compris entre {Player.MinimumAge} et {Player.MaximumAge} ans.");
        }

        return await db.AgeGroups
                   .AsNoTracking()
                   .Where(a => a.MinimumAge <= age && age <= a.MaximumAge)
                   .Select(a => new AgeGroupDto(a.Id, a.Name, a.MinimumAge, a.MaximumAge))
                   .SingleOrDefaultAsync(cancellationToken)
               ?? throw new GameValidationException(AgeField, $"Aucune tranche d'âge n'est définie pour {age} ans.");
    }
}

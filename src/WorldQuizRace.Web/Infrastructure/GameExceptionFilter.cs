using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using WorldQuizRace.Application.Exceptions;

namespace WorldQuizRace.Web.Infrastructure;

/// <summary>
/// Turns expected business errors into ProblemDetails responses (400 validation, 404 not found, 409 rule/conflict)
/// with a stable "code" extension for the JavaScript client. Unexpected exceptions are left untouched.
/// </summary>
public sealed class GameExceptionFilter(ProblemDetailsFactory problemDetails) : IExceptionFilter
{
    public const string CodeExtension = "code";

    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not GameException exception)
        {
            return;
        }

        var status = exception switch
        {
            GameValidationException => StatusCodes.Status400BadRequest,
            GameNotFoundException => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status409Conflict
        };

        ProblemDetails details;
        if (exception is GameValidationException validation)
        {
            var modelState = new ModelStateDictionary();
            foreach (var (field, messages) in validation.Errors)
            {
                foreach (var message in messages)
                {
                    modelState.AddModelError(field, message);
                }
            }

            details = problemDetails.CreateValidationProblemDetails(context.HttpContext, modelState, status, title: exception.Message);
        }
        else
        {
            details = problemDetails.CreateProblemDetails(context.HttpContext, status, title: exception.Message);
        }

        details.Extensions[CodeExtension] = exception.Code;
        context.Result = new ObjectResult(details) { StatusCode = status, ContentTypes = { "application/problem+json" } };
        context.ExceptionHandled = true;
    }
}

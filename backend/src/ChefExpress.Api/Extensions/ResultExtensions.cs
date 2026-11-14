using ChefExpress.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace ChefExpress.Api.Extensions;

/// <summary>Maps application <see cref="Error"/>s to RFC 7807 ProblemDetails responses.</summary>
public static class ResultExtensions
{
    public static IActionResult ToProblem(this ControllerBase controller, Error error)
    {
        var status = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        // Use the framework factory so responses match ASP.NET's own errors (type, traceId, ...).
        var factory = controller.ProblemDetailsFactory;
        ProblemDetails problem;
        if (error.Details is { Count: > 0 })
        {
            var modelState = new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary();
            foreach (var (field, messages) in error.Details)
                foreach (var message in messages)
                    modelState.AddModelError(field, message);

            problem = factory.CreateValidationProblemDetails(controller.HttpContext, modelState, status, error.Message);
        }
        else
        {
            problem = factory.CreateProblemDetails(controller.HttpContext, status, error.Message);
        }

        problem.Extensions["code"] = error.Code;

        return controller.StatusCode(status, problem);
    }
}

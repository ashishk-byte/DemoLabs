using DemoWebApiDB.Infrastructure.Results;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;


namespace DemoWebApiDB.Controllers;


/// <summary>
///     Provides a common base controller for all API controllers.
/// 
///     Centralizes translation of the application-layer Result pattern
///     into proper HTTP responses compliant with RFC 7807 (Problem Details).
/// </summary>
/// <remarks>
///     Ensure that:
///     - Reduced duplication across controllers
///     - Clean separation between application services and HTTP layer
///     - Result Pattern aligned consistent API responses
///         - Structured and predictable RFC7807 compliant error handling
/// </remarks>
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[ApiController]
public abstract class BaseApiController : ControllerBase
{

    #region Result TO IActionResult Translator Helper methods

    /// <summary>
    ///     Converts a non-generic Result into an appropriate IActionResult.
    ///     Used for operations that do not return a payload (e.g., Update, Delete).
    /// </summary>
    /// <param name="result">Service layer result.</param>
    protected IActionResult HandleResult(Result result)
    {
        return result.Status switch
        {
            ResultStatus.Success 
                => NoContent(),

            ResultStatus.Created 
                => StatusCode(StatusCodes.Status201Created),

            ResultStatus.Accepted 
                => Accepted(),

            ResultStatus.NotFound 
                => NotFound(CreateProblemDetails(result)),

            ResultStatus.Concurrency
                => StatusCode(StatusCodes.Status412PreconditionFailed, CreateProblemDetails(result)),
 
            ResultStatus.Conflict 
                => Conflict(CreateProblemDetails(result)),

            ResultStatus.ValidationError 
                => BadRequest(CreateValidationProblemDetails(result)),

            ResultStatus.Unauthorized 
                => Unauthorized(CreateProblemDetails(result)),

            ResultStatus.Forbidden 
                => StatusCode(StatusCodes.Status403Forbidden, CreateProblemDetails(result)),

            _ =>  StatusCode(StatusCodes.Status500InternalServerError,
                    CreateProblemDetails(
                        result: result,
                        title: "Unexpected error",
                        detail: "An unexpected server error occurred."))
        };
    }


    /// <summary>
    ///     Converts a generic Result(of Type T) into an IActionResult.
    ///     Used for operations returning data payloads.
    /// </summary>
    /// <typeparam name="T">Payload type.</typeparam>
    /// <param name="result">Service result.</param>
    protected IActionResult HandleResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return result.Status switch
            {
                ResultStatus.Success => 
                    Ok(result.Data),

                ResultStatus.Created =>
                    Created(string.Empty, result.Data),

                ResultStatus.Accepted =>
                    Accepted(result.Data),

                _ =>
                    Ok(result.Data)
            };
        }

        // since failure, no data payload.
        return result.Status switch
        {
            ResultStatus.NotFound =>
                NotFound(CreateProblemDetails(result)),

            ResultStatus.Concurrency
                => StatusCode(StatusCodes.Status412PreconditionFailed,
                    CreateProblemDetails(
                        result: result,
                        title: "Concurrency conflict",
                        detail: "The information was modified by another user. Please reload and try again.")),

            ResultStatus.Conflict =>
                Conflict(CreateProblemDetails(result)),

            ResultStatus.ValidationError =>
                BadRequest(CreateValidationProblemDetails(result)),

            ResultStatus.Unauthorized =>
                Unauthorized(CreateProblemDetails(result)),

            ResultStatus.Forbidden =>
                StatusCode(StatusCodes.Status403Forbidden, CreateProblemDetails(result)),

            _ => 
                StatusCode(StatusCodes.Status500InternalServerError,
                    CreateProblemDetails(
                        result: result,
                        title: "Unexpected error",
                        detail: "An unexpected server error occurred."))
        };
    }

    #endregion



    #region ProblemDetails Builder Helper methods

    /// <summary>
    ///     Creates RFC 7807 ProblemDetails from a non-generic Result.
    /// </summary>
    protected ProblemDetails CreateProblemDetails(
        Result result,
        string? title = null,
        string? detail = null)
    {
        var error = result.Errors.FirstOrDefault();

        var problem = new ProblemDetails
        {
            Title = title ?? error?.Code ?? "Error",
            Detail = detail ?? error?.Message,
            Status = MapStatusCode(result.Status),
            Instance = HttpContext.Request.Path
        };

        // attach "errorCode" as extension to the Problem details, if available, for better client-side handling
        if (error is not null)
        {
            problem.Extensions["errorCode"] = error.Code; 
        }

        // Add CorrelationId
        problem.Extensions["correlationId"] 
            = HttpContext.Items["CorrelationId"]?.ToString()
                ?? HttpContext.TraceIdentifier;

        return problem;
    }


    /// <summary>
    ///     Creates RFC 7807 ProblemDetails from a generic Result.
    /// </summary>
    protected ProblemDetails CreateProblemDetails<T>(
        Result<T> result,
        string? title = null,
        string? detail = null)
    {
        var error = result.Errors.FirstOrDefault();

        return new ProblemDetails
        {
            Title = title ?? error?.Code ?? "Error",
            Detail = detail ?? error?.Message,
            Status = MapStatusCode(result.Status),
            Instance = HttpContext.Request.Path
        };
    }


    /// <summary>
    ///     Creates ValidationProblemDetails from a non-generic Result.
    ///     Used when validation failures occur.
    /// </summary>
    protected ValidationProblemDetails CreateValidationProblemDetails(Result result)
    {
        var errors = result.ValidationErrors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key, 
                g => g.Select(e => e.ErrorMessage).ToArray());

        return new ValidationProblemDetails(errors)
        {
            Title = "Validation failed",
            Status = StatusCodes.Status400BadRequest,
            Instance = HttpContext.Request.Path
        };
    }


    /// <summary>
    ///     Creates ValidationProblemDetails from a generic Result.
    /// </summary>
    protected ValidationProblemDetails CreateValidationProblemDetails<T>(Result<T> result)
    {
        var errors = result.ValidationErrors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        return new ValidationProblemDetails(errors)
        {
            Title = "Validation failed",
            Status = StatusCodes.Status400BadRequest,
            Instance = HttpContext.Request.Path
        };
    }

    #endregion



    #region Status Mapping Helper methods

    /// <summary>
    ///     Maps ResultStatus to HTTP status code.
    ///     Used for ProblemDetails responses.
    /// </summary>
    private static int MapStatusCode(ResultStatus status) =>
        status switch
        {
            ResultStatus.NotFound           => StatusCodes.Status404NotFound,
            ResultStatus.Concurrency        => StatusCodes.Status412PreconditionFailed,
            ResultStatus.Conflict           => StatusCodes.Status409Conflict,
            ResultStatus.ValidationError    => StatusCodes.Status400BadRequest,
            ResultStatus.Unauthorized       => StatusCodes.Status401Unauthorized,
            ResultStatus.Forbidden          => StatusCodes.Status403Forbidden,
            _                               => StatusCodes.Status500InternalServerError
        };

    #endregion

}

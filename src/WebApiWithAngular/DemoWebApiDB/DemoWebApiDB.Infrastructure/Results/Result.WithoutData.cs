namespace DemoWebApiDB.Infrastructure.Results;


/// <summary>
///     Represents the outcome of a service operation that does NOT return data.
///     
///     This type is commonly used for operations such as:
///         - UPDATE
///         - DELETE
///     
///     It communicates only the operation status and error information.
/// </summary>
/// <remarks>
///     For example, a DELETE operation requires NoContent response if successful.  
///     But requires error details on failure - this is handled by Result(of Type T) - the other class.
/// </remarks>
public sealed class Result : ResultBase
{

    /// <summary>
    ///     Initializes a new instance of <see cref="Result"/>.
    /// </summary>
    /// <param name="status">Operation outcome status.</param>
    /// <param name="errors">General errors.</param>
    /// <param name="validationErrors">Validation errors.</param>
    private Result(
        ResultStatus status,
        IEnumerable<ErrorModel>? errors = null,
        IEnumerable<ValidationErrorModel>? validationErrors = null)
        : base(status, errors, validationErrors)
    {
    }


    #region Factory Methods

    /// <summary>
    ///     Creates a successful result.
    /// </summary>
    public static Result Success() =>
        new(ResultStatus.Success);


    /// <summary>
    ///     Creates a result representing successful resource creation.
    /// </summary>
    public static Result Created() =>
        new(ResultStatus.Created);


    /// <summary>
    ///     Creates a result representing an accepted operation.
    /// </summary>
    public static Result Accepted() =>
        new(ResultStatus.Accepted);


    /// <summary>
    ///     Creates a result indicating the requested resource was not found.
    /// </summary>
    public static Result NotFound(string message) =>
        new(ResultStatus.NotFound,
            errors: new[]
            {
                new ErrorModel("NotFound", message)
            });


    /// <summary>
    ///     Creates a result indicating a concurrency conflict.
    /// </summary>
    public static Result Concurrency(string message) =>
        new(ResultStatus.Concurrency,
            errors: new[]
            {
                new ErrorModel("Concurrency", message)
            });


    /// <summary>
    ///     Creates a result indicating a business rule conflict.
    /// </summary>
    public static Result Conflict(string message) =>
        new(ResultStatus.Conflict,
            errors: new[]
            {
                new ErrorModel("Conflict", message)
            });


    /// <summary>
    ///     Creates a result representing validation failures.
    /// </summary>
    public static Result ValidationFailure(
        IEnumerable<ValidationErrorModel> validationErrors) =>
        new(ResultStatus.ValidationError,
            validationErrors: validationErrors);


    /// <summary>
    ///     Creates a result indicating authentication failure.
    /// </summary>
    public static Result Unauthorized(string message) =>
        new(ResultStatus.Unauthorized,
            errors: new[]
            {
                new ErrorModel("Unauthorized", message)
            });


    /// <summary>
    ///     Creates a result indicating authorization failure.
    /// </summary>
    public static Result Forbidden(string message) =>
        new(ResultStatus.Forbidden,
            errors: new[]
            {
                new ErrorModel("Forbidden", message)
            });


    /// <summary>
    ///     Creates a result representing an unexpected system error.
    /// </summary>
    public static Result Error(string message) =>
        new(ResultStatus.Error,
            errors: new[]
            {
                new ErrorModel("Error", message)
            });

    #endregion

}
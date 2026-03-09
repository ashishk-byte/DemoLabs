namespace DemoWebApiDB.Infrastructure.Results;


/// <summary>
///     Represents the outcome of a service operation that returns data.
///     
///     This class is typically used for:
///         - GET operations
///         - POST operations returning created objects
///         - Reporting queries
///     
///     It extends <see cref="ResultBase"/> by including a payload.
/// </summary>
/// <typeparam name="T">
///     Type of data returned when the operation succeeds.
/// </typeparam>
public sealed class Result<T> : ResultBase
{

    /// <summary>
    ///     Data returned when the operation succeeds.
    /// </summary>
    public T? Data { get; }


    /// <summary>
    ///     Initializes a new instance of <see cref="Result{T}"/>.
    /// </summary>
    /// <param name="status">Operation status.</param>
    /// <param name="data">Payload data.</param>
    /// <param name="errors">General errors.</param>
    /// <param name="validationErrors">Validation errors.</param>
    private Result(
        ResultStatus status,
        T? data = default,
        IEnumerable<ErrorModel>? errors = null,
        IEnumerable<ValidationErrorModel>? validationErrors = null)
        : base(status, errors, validationErrors)
    {
        Data = data;
    }


    #region Factory Methods

    /// <summary>
    ///     Creates a successful result containing data.
    /// </summary>
    public static Result<T> Success(T data) =>
        new(ResultStatus.Success, data);


    /// <summary>
    ///     Creates a result representing successful resource creation.
    /// </summary>
    public static Result<T> Created(T data) =>
        new(ResultStatus.Created, data);


    /// <summary>
    ///     Creates a result representing an accepted operation.
    /// </summary>
    public static Result<T> Accepted(T data) =>
        new(ResultStatus.Accepted, data);


    /// <summary>
    ///     Creates a result indicating the requested resource was not found.
    /// </summary>
    public static Result<T> NotFound(string message) =>
        new(ResultStatus.NotFound,
            errors: new[]
            {
                new ErrorModel("NotFound", message)
            });


    /// <summary>
    ///     Creates a result indicating a concurrency conflict occurred.
    /// </summary>
    public static Result<T> Concurrency(string message) =>
        new(ResultStatus.Concurrency,
            errors: new[]
            {
                new ErrorModel("Concurrency", message)
            });


    /// <summary>
    ///     Creates a result indicating a business rule conflict.
    /// </summary>
    public static Result<T> Conflict(string message) =>
        new(ResultStatus.Conflict,
            errors: new[]
            {
                new ErrorModel("Conflict", message)
            });


    /// <summary>
    ///     Creates a result representing validation failures.
    /// </summary>
    public static Result<T> ValidationFailure(
        IEnumerable<ValidationErrorModel> validationErrors) =>
        new(ResultStatus.ValidationError,
            validationErrors: validationErrors);


    /// <summary>
    ///     Creates a result indicating authentication failure.
    /// </summary>
    public static Result<T> Unauthorized(string message) =>
        new(ResultStatus.Unauthorized,
            errors: new[]
            {
                new ErrorModel("Unauthorized", message)
            });


    /// <summary>
    ///     Creates a result indicating authorization failure.
    /// </summary>
    public static Result<T> Forbidden(string message) =>
        new(ResultStatus.Forbidden,
            errors: new[]
            {
                new ErrorModel("Forbidden", message)
            });


    /// <summary>
    ///     Creates a result representing an unexpected system error.
    /// </summary>
    public static Result<T> Error(string message) =>
        new(ResultStatus.Error,
            errors: new[]
            {
                new ErrorModel("Error", message)
            });

    #endregion

}
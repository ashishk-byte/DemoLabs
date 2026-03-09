namespace DemoWebApiDB.Infrastructure.Results;


/// <summary>
///     Represents the base class for all Result types.
///     
///     This class encapsulates common properties shared by both:
///     - <see cref="Result"/> (non-generic)
///     - <see cref="Result{T}"/> (generic)
///
///     The purpose of this base class is to:
///         - eliminate duplication
///         - provide consistent success/failure metadata
///         - enable future extensibility (e.g., correlationId, traceId, errorCode)
/// </summary>
public abstract class ResultBase
{

    /// <summary>
    ///     Status representing the outcome of the operation.
    /// </summary>
    public ResultStatus Status { get; }


    /// <summary>
    ///     Collection of non-validation errors returned from the operation.
    /// </summary>
    public IReadOnlyList<ErrorModel> Errors { get; }


    /// <summary>
    ///     Collection of validation errors returned from the operation.
    /// </summary>
    public IReadOnlyList<ValidationErrorModel> ValidationErrors { get; }


    /// <summary>
    ///     Indicates whether the operation completed successfully.
    /// </summary>
    /// <remarks>
    ///     Success states include:
    ///         - Success
    ///         - Created
    ///         - Accepted
    /// </remarks>
    public bool IsSuccess =>
        Status is ResultStatus.Success or ResultStatus.Created or ResultStatus.Accepted;


    /// <summary>
    ///     Initializes a new instance of <see cref="ResultBase"/>.
    /// </summary>
    /// <param name="status">Outcome status.</param>
    /// <param name="errors">General errors (non-validation).</param>
    /// <param name="validationErrors">Validation errors.</param>
    protected ResultBase(
        ResultStatus status,
        IEnumerable<ErrorModel>? errors = null,
        IEnumerable<ValidationErrorModel>? validationErrors = null)
    {
        Status = status;

        // Convert to read-only collections to prevent mutation outside the class
        Errors = errors?.ToList() ?? new List<ErrorModel>();

        ValidationErrors = validationErrors?.ToList() ?? new List<ValidationErrorModel>();
    }

}

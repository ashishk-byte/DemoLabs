using DemoWebApiDB.DtoModels.Products;
using DemoWebApiDB.Infrastructure.Results;
using DemoWebApiDB.Services.Products;


namespace DemoWebApiDB.Controllers;


/// <summary>
///     Provides API endpoints for managing Products.
/// </summary>
/// <remarks>
///     This controller demonstrates:
///     - Clean separation of concerns
///     - Result pattern usage
///     - RFC7807 compliant error handling
///     - Concurrency-safe updates and deletes
///     - Enterprise-grade API design
/// 
/// TODO:
///     Add paging for GET ALL endpoint in future.
/// </remarks>
[Route("api/[controller]")]
public sealed class ProductsController : BaseApiController
{
    private readonly ProductService _service;

    /// <summary>
    ///     Initializes a new instance of <see cref="ProductsController"/>.
    /// </summary>
    public ProductsController(ProductService service)
    {
        _service = service;
    }


    #region CREATE

    /// <summary>
    ///     Creates a new Product.
    /// </summary>
    /// <remarks>
    ///     This endpoint:
    ///     - Normalizes Product Name
    ///     - Ensures Product Name is unique within Category
    ///     - Validates Category existence
    ///     - Returns only the Location of created resource
    /// 
    ///     Returns 201 Created with Location header.
    ///     Client should perform GET using returned location if details required.
    /// </remarks>
    [HttpPost]
    [EndpointSummary("Create new Product")]
    [EndpointDescription(
        "Creates a new Product after validating existence of Category and duplicate names within Category. "
        + "Returns 201 CREATED with the Location pointing to newly created resource."
    )]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateProduct([FromBody] ProductCreateDto dto)
    {
        var result = await _service.CreateAsync(dto);

        if (result.IsSuccess && result.Status == ResultStatus.Created)
        {
            return Created(result.Data!, null);
        }

        return HandleResult(result);
    }

    #endregion


    #region GET BY ID

    /// <summary>
    ///     Retrieve a Product by its identifier.
    /// </summary>
    [HttpGet("{id:guid}")]
    [EndpointSummary("Retrieve Product by ID")]
    [EndpointDescription(
        "Retrieves a specific Product by its identifier including category details and audit information."
    )]
    [ProducesResponseType(typeof(ProductReadDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return HandleResult(result);
    }

    #endregion


    #region GET ALL

    /// <summary>
    ///     Retrieve all Products.
    /// </summary>
    /// <remarks>
    ///     Returns Products ordered by Product Name.
    ///     NOTE: Paging not implemented yet.
    /// </remarks>
    [HttpGet]
    [EndpointSummary("Retrieve all Products")]
    [EndpointDescription(
        "Returns all Products ordered by Product Name. "
        + "Includes Category details. Paging will be implemented in future."
    )]
    [ProducesResponseType(typeof(IReadOnlyList<ProductReadDTO>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllProducts()
    {
        var result = await _service.GetAllAsync();
        return HandleResult(result);
    }

    #endregion


    #region UPDATE

    /// <summary>
    ///     Updates an existing Product.
    /// </summary>
    /// <remarks>
    ///     Requires:
    ///     - Matching route and payload IDs
    ///     - Valid RowVersion
    ///     - Category existence validation
    ///     - Duplicate name prevention within category
    /// 
    ///     Returns 202 Accepted on success.
    /// </remarks>
    [HttpPut("{id:guid}")]
    [EndpointSummary("Update an existing Product")]
    [EndpointDescription(
        "Updates a Product using optimistic concurrency validation. "
        + "Requires valid RowVersion and matching route/payload identifiers. "
        + "Allows moving Product across Categories."
    )]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]               // errorCode: "Conflict"
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status412PreconditionFailed)]     // errorCode: "Concurency"
    public async Task<IActionResult> UpdateProduct(
        Guid id,
        [FromBody] ProductUpdateDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        return HandleResult(result);
    }

    #endregion


    #region DELETE

    /// <summary>
    ///     Deletes an existing product.
    /// </summary>
    /// <remarks>
    ///     Requirements:
    ///     - Matching route and payload ID
    ///     - Valid RowVersion
    ///     - Concurrency-safe delete
    /// 
    ///     Returns 204 NoContent on success.
    /// </remarks>
    [HttpDelete("{id:guid}")]
    [EndpointSummary("Delete existing Product")]
    [EndpointDescription(
        "Deletes a Product using optimistic concurrency validation. "
        + "Requires valid RowVersion. Returns 204 NoContent on success."
    )]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteProduct(
        Guid id,
        [FromBody] ProductDeleteDto dto)
    {
        var result = await _service.DeleteAsync(id, dto);
        return HandleResult(result);
    }

    #endregion


    #region VIEW METHODS

    // GET https://localhost:xxxx/api/products/with-category
    /// <summary>
    ///     Retrieves Products with Category details from VIEW.
    /// </summary>
    /// <remarks>
    ///     Uses VIEW:
    ///     dbo.vw_ProductsWithCategory
    /// 
    ///     Demonstrates how Entity Framework Core consumes a View
    ///     and maps results to read-only projection models.
    /// </remarks>
    [HttpGet("with-category")]
    [EndpointSummary("Get Products with Category")]
    [EndpointDescription(
        "Returns Product details along with Category information from a database VIEW."
    )]
    [ProducesResponseType(typeof(IReadOnlyList<ProductWithCategoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProductsWithCategory()
    {
        var result = await _service.GetProductsWithCategoryAsync();
        return HandleResult(result);
    }

    #endregion

}
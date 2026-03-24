using DemoWebApiDB.DtoModels.Products;
using DemoWebApiDB.DtoModels.ReadModels;
using DemoWebApiDB.Infrastructure.Helpers;


namespace DemoWebApiDB.Services.Products;


/// <summary>
///     Handles business logic for Product operations.
/// </summary>
public sealed class ProductService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<ProductService> _logger;


    public ProductService(
        ApplicationDbContext db,
        ILogger<ProductService> logger)
    {
        _dbContext = db;
        _logger = logger;
    }


    /// <summary>
    ///     Creates a new Product.
    ///     Returns Location URI of created resource, if successful.
    /// </summary>
    public async Task<Result<string>> CreateAsync(ProductCreateDto dto)
    {
        // ----- 01. Normalize and Validate Product Name

        var cleanedName = dto.ProductName.TrimOrEmpty();
        if (!cleanedName.HasValue())
        {
            _logger.LogWarning(
                "Product creation failed: empty name provided.");

            return Result<string>.ValidationFailure(new[]
            {
                new ValidationErrorModel(
                    nameof(dto.ProductName),
                    "Product name cannot be empty or whitespace.")
            });
        }

        var normalizedName = cleanedName.NormalizeKey();


        // ----- 02. Referrencial Data Validity: Validate if the Category exists

        bool categoryExists =
            await _dbContext.Categories
                .AnyAsync(c => c.CategoryId == dto.CategoryId);

        if (!categoryExists)
        {
            _logger.LogWarning(
                "Product creation failed: CategoryId {CategoryId} not found.",
                dto.CategoryId);

            return Result<string>.NotFound(
                $"Category with id '{dto.CategoryId}' was not found.");
        }


        // ----- 03. Business Rule: Product Name must be UNIQUE within Category

        bool duplicateExists =
            await _dbContext.Products
                .AnyAsync(p =>
                    p.CtgryId == dto.CategoryId &&
                    p.ProductName.ToUpper() == normalizedName);

        if (duplicateExists)
        {
            _logger.LogWarning(
                "Product creation failed: Duplicate product '{ProductName}' in CategoryId {CategoryId}.",
                cleanedName,
                dto.CategoryId);

            return Result<string>.Conflict(
                $"Product '{cleanedName}' already exists in this category.");
        }


        // ----- 04. Create the Entity

        var entity = new Product
        {
            ProductName = cleanedName,
            Price = dto.Price,
            QtyInStock = dto.QtyInStock,
            CtgryId = dto.CategoryId
        };

        _dbContext.Products.Add(entity);


        // ----- 05. Flush to Database

        await _dbContext.SaveChangesAsync();


        // ----- 06. Build Location URI

        var location = $"/api/products/{entity.ProductId}";


        // ----- 07. Log Success

        _logger.LogInformation(
            "Product created successfully. ProductId: {ProductId}, Name: {ProductName}, CategoryId: {CategoryId}",
            entity.ProductId,
            entity.ProductName,
            entity.CtgryId);


        // ----- 08. Return Result

        return Result<string>.Created(location);
    }


    /// <summary>
    ///     Retrieves a Product by its identifier.
    /// </summary>
    public async Task<Result<ProductReadDTO>> GetByIdAsync(Guid id)
    {
        // ----- 01. Fetch the entity (Include Category for display purposes)

        var entity = await _dbContext.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.ProductId == id);


        // ----- 02. If not found, return NOT FOUND

        if (entity is null)
        {
            _logger.LogWarning(
                "Product retrieval failed. ProductId: {ProductId} not found.",
                id);

            return Result<ProductReadDTO>.NotFound(
                $"Product with id '{id}' was not found.");
        }


        // ----- 03. Map to DTO

        var dto = new ProductReadDTO(
            ProductId: entity.ProductId,
            ProductName: entity.ProductName,
            Price: entity.Price,
            QtyInStock: entity.QtyInStock,
            CategoryId: entity.CtgryId,
            CategoryName: entity.Category?.Name ?? string.Empty,
            CreatedAtUtc: entity.CreatedAtUtc,
            ModifiedAtUtc: entity.ModifiedAtUtc,
            RowVersion: RowVersionHelper.ToBase64(entity.RowVersion)
        );


        // ----- 04. Log Success

        _logger.LogInformation(
            "Product retrieved successfully. ProductId: {ProductId}",
            entity.ProductId);


        // ----- 05. Return Result

        return Result<ProductReadDTO>.Success(dto);
    }


    /// <summary>
    ///     Retrieves all the products.
    /// </summary>
    /// <remarks>
    ///     Ensure that:
    ///     - Return an IReadOnlyList to:
    ///         - prevent accidental modification 
    ///         - communicate intent
    ///         - enterprise convention
    ///     - Never return unordered data.
    ///         - Default DB order is undefined
    ///         - UI flickering possible
    ///         - Paging later becomes inconsistent
    ///     - Default sort on ProductName
    ///     - Use AsNoTracking for read-only discipline
    ///     - Project to DTO
    ///     - Log summary only
    /// </remarks>
    public async Task<Result<IReadOnlyList<ProductReadDTO>>> GetAllAsync()
    {
        // ----- 01. Fetch the entities (no tracking + include category + ordered)

        var entities = await _dbContext.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .OrderBy(p => p.ProductName)
            .ToListAsync();


        // ----- 02. Map to DTO and return as read-only list

        var dtos = entities
            .Select(entity => new ProductReadDTO(
                ProductId: entity.ProductId,
                ProductName: entity.ProductName,
                Price: entity.Price,
                QtyInStock: entity.QtyInStock,
                CategoryId: entity.CtgryId,
                CategoryName: entity.Category?.Name ?? string.Empty,
                CreatedAtUtc: entity.CreatedAtUtc,
                ModifiedAtUtc: entity.ModifiedAtUtc,
                RowVersion: RowVersionHelper.ToBase64(entity.RowVersion)
            ))
            .ToList()
            .AsReadOnly();


        // ----- 03. Log summary only

        _logger.LogInformation(
            "Retrieved {ProductCount} products.",
            dtos.Count);


        // ----- 04. Return Result

        return Result<IReadOnlyList<ProductReadDTO>>.Success(dtos);
    }


    /// <summary>
    ///     Updates an existing Product.
    /// </summary>
    /// <remarks>
    ///     Ensure that the UPDATE Operation is:
    ///     - Defensive API Design
    ///     - Concurrency Safe
    ///     - Business Rule Safe
    ///     - Category Integrity Safe
    ///     - Normalization Disciplined
    ///     - Structured Logging Friendly
    ///     - Result Pattern compliant
    /// </remarks>
    public async Task<Result> UpdateAsync(Guid routeId, ProductUpdateDto dto)
    {
        // ----- 01. Route Vs. Body validation

        if (routeId != dto.ProductId)
        {
            _logger.LogWarning(
                "Product update failed: route ID {RouteId} does not match payload ID {PayloadId}.",
                routeId,
                dto.ProductId);

            return Result.ValidationFailure(new[]
            {
            new ValidationErrorModel(
                nameof(dto.ProductId),
                "Route ID and payload ID must match.")
        });
        }


        // ----- 02. Fetch entity

        var entity = await _dbContext.Products
            .FirstOrDefaultAsync(p => p.ProductId == routeId);

        if (entity is null)
        {
            _logger.LogWarning(
                "Product update failed. ProductId: {ProductId} not found.",
                routeId);

            return Result.NotFound(
                $"Product with id '{routeId}' was not found.");
        }


        // ----- 03. Apply RowVersion for Concurrency Check

        byte[] incomingRowVersion;

        try
        {
            incomingRowVersion = RowVersionHelper.FromBase64(dto.RowVersion);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex,
                "Product update failed due to invalid RowVersion format. ProductId: {ProductId}",
                routeId);

            return Result.ValidationFailure(new[]
            {
            new ValidationErrorModel(
                nameof(dto.RowVersion),
                "Invalid RowVersion format.")
        });
        }

        _dbContext.Entry(entity)
            .Property(e => e.RowVersion)
            .OriginalValue = incomingRowVersion;


        // ----- 04. Normalize and Validate Product Name

        var cleanName = dto.ProductName.TrimOrEmpty();

        if (!cleanName.HasValue())
        {
            _logger.LogWarning(
                "Product update failed: empty name provided. ProductId: {ProductId}",
                routeId);

            return Result.ValidationFailure(new[]
            {
            new ValidationErrorModel(
                nameof(dto.ProductName),
                "Product name cannot be empty or whitespace.")
        });
        }

        var normalizedName = cleanName.NormalizeKey();


        // ----- 05. Validate Category exists (in case product moved)

        bool categoryExists =
            await _dbContext.Categories
                .AnyAsync(c => c.CategoryId == dto.CategoryId);

        if (!categoryExists)
        {
            _logger.LogWarning(
                "Product update failed: CategoryId {CategoryId} not found.",
                dto.CategoryId);

            return Result.NotFound(
                $"Category with id '{dto.CategoryId}' was not found.");
        }


        // ----- 06. Business Rule: Duplicate check within Category (excluding self)

        bool duplicateExists =
            await _dbContext.Products
                .AnyAsync(p =>
                    p.ProductId != routeId &&
                    p.CtgryId == dto.CategoryId &&
                    p.ProductName.ToUpper() == normalizedName);

        if (duplicateExists)
        {
            _logger.LogWarning(
                "Product update failed: duplicate product '{ProductName}' in CategoryId {CategoryId}.",
                cleanName,
                dto.CategoryId);

            return Result.Conflict(
                $"Another product with name '{cleanName}' already exists in this category.");
        }


        // ----- 07. Apply changes

        entity.ProductName = cleanName;
        entity.Price = dto.Price;
        entity.QtyInStock = dto.QtyInStock;
        entity.CtgryId = dto.CategoryId;


        // ----- 08. Commit changes with Concurrency Handling

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning(
                "Product update concurrency conflict. ProductId: {ProductId}",
                routeId);

            return Result.Concurrency(
                "The record was modified by another user. Please reload and try again.");
        }


        // ----- 09. Log Success

        _logger.LogInformation(
            "Product updated successfully. ProductId: {ProductId}",
            routeId);


        // ----- 10. Return Result

        return Result.Accepted();
    }



    /// <summary>
    ///     Deletes an existing Product.
    /// </summary>
    /// <remarks>
    ///     Ensure that the DELETE Operation is:
    ///     - Defensive API Design
    ///     - Concurrency Safe
    ///     - Audit Friendly
    ///     - Result Pattern compliant
    ///     - Hard delete only (no soft delete)
    /// </remarks>
    public async Task<Result> DeleteAsync(Guid routeId, ProductDeleteDto dto)
    {
        // ----- 01. Route Vs. Body validation

        if (routeId != dto.ProductId)
        {
            _logger.LogWarning(
                "Product delete failed: route ID {RouteId} does not match payload ID {PayloadId}.",
                routeId,
                dto.ProductId);

            return Result.ValidationFailure(new[]
            {
            new ValidationErrorModel(
                nameof(dto.ProductId),
                "Route ID and payload ID must match.")
        });
        }


        // ----- 02. Fetch entity

        var entity = await _dbContext.Products
            .FirstOrDefaultAsync(p => p.ProductId == routeId);

        if (entity is null)
        {
            _logger.LogWarning(
                "Product delete failed. ProductId: {ProductId} not found.",
                routeId);

            return Result.NotFound(
                $"Product with id '{routeId}' was not found.");
        }


        // ----- 03. Apply RowVersion for Concurrency Check

        byte[] incomingRowVersion;

        try
        {
            incomingRowVersion = RowVersionHelper.FromBase64(dto.RowVersion);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex,
                "Product delete failed due to invalid RowVersion format. ProductId: {ProductId}",
                routeId);

            return Result.ValidationFailure(new[]
            {
            new ValidationErrorModel(
                nameof(dto.RowVersion),
                "Invalid RowVersion format.")
        });
        }

        _dbContext.Entry(entity)
            .Property(e => e.RowVersion)
            .OriginalValue = incomingRowVersion;


        // ----- 04. Remove entity (Hard delete)

        _dbContext.Products.Remove(entity);


        // ----- 05. Commit with concurrency handling

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning(
                "Product delete concurrency conflict. ProductId: {ProductId}",
                routeId);

            return Result.Conflict(
                "The record was modified by another user. Please reload and try again.");
        }


        // ----- 06. Log Success

        _logger.LogInformation(
            "Product deleted successfully. ProductId: {ProductId}",
            routeId);


        // ----- 07. Return Result

        return Result.Success();
    }


    /// <summary>
    ///     Retrieves Products with Category details from database VIEW.
    /// </summary>
    /// <remarks>
    ///     - Read-only query
    ///     - Uses AsNoTracking discipline
    ///     - Ordered by ProductName
    ///     - Maps ReadModel to DTO
    ///     - Logs summary only
    ///     
    ///     VERY IMPORTANT NOTE:
    ///     SQLite does not support VIEWS.
    ///     I have implemented a fallback code block only to clear the Integration Tests.
    /// </remarks>
    public async Task<Result<IReadOnlyList<ProductWithCategoryDto>>> GetProductsWithCategoryAsync()
    {
        // ----- 01. Fetch from VIEW (no tracking + ordered)

        List<ProductWithCategoryReadModel> entities;

        if (_dbContext.IsSqlite)
        {
            // Fallback code for providing data using SQLite (which is used for Integration Testing only)
            entities = await _dbContext.Products
                    .Include(p => p.Category)
                    .AsNoTracking()
                    .OrderBy(p => p.ProductName)
                    .Select(p => new ProductWithCategoryReadModel
                    {
                        Id = p.ProductId,
                        ProductName = p.ProductName,
                        Price = p.Price,
                        QtyInStock = p.QtyInStock,
                        CategoryId = p.CtgryId,
                        CategoryName = p.Category!.Name
                    })
                    .ToListAsync();
        }
        else
        {
            // Fetch the data from SQL SERVER VIEW
            entities = await _dbContext.ProductsWithCategoryView
            .AsNoTracking()
            .OrderBy(p => p.ProductName)
            .ToListAsync();
        }


        // ----- 02. Map to DTO

        var dtos = entities
            .Select(e => new ProductWithCategoryDto(
                ProductId: e.Id,
                ProductName: e.ProductName,
                Price: e.Price,
                QtyInStock: e.QtyInStock,
                CategoryId: e.CategoryId,
                CategoryName: e.CategoryName
            ))
            .ToList()
            .AsReadOnly();


        // ----- 03. Log summary

        _logger.LogInformation(
            "Retrieved {ProductCount} products with category details from VIEW.",
            dtos.Count);


        // ----- 04. Return result

        return Result<IReadOnlyList<ProductWithCategoryDto>>.Success(dtos);
    }

}
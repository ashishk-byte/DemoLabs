using DemoWebApiDB.Data.Data;
using DemoWebApiDB.Data.Entities;
using DemoWebApiDB.DtoModels.Products;
using DemoWebApiDB.Tests.TestInfrastructure;

using Microsoft.AspNetCore.Http;


namespace DemoWebApiDB.Tests.ProductTests;


/// <summary>
///     Integration tests for Product UPDATE endpoint.
///     
///     Demonstrates:
///     - Success update                            202
///     - Move to different category                202
///     - Route/body mismatch                       400
///     - Invalid payload (invalid name)            400
///     - Invalid payload (invalid rowversion)      400
///     - Category Not found                        404
///     - Invalid category                          404
///     - Duplicate name within same category       409
///     - Concurrency conflict                      412
/// </summary>
public sealed class Product_Update_Tests
{

    [Fact]
    public async Task UpdateProduct_ShouldReturn202_WhenValid()
    {
        // ----- Arrange
        
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var product = db.Products.First();

        // ----- Act

        var dto = new ProductUpdateDto(
                    ProductId: product.ProductId,
                    ProductName: "Updated Product",
                    Price: 200,
                    QtyInStock: 20,
                    CategoryId: product.CtgryId,
                    RowVersion: Convert.ToBase64String(product.RowVersion)
                );
        
        var response 
            = await client.PutAsJsonAsync( $"/api/products/{product.ProductId}", dto, TestContext.Current.CancellationToken);

        // ---- Assert

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);                   // HTTP 202 "Accepted"

        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var updated = verifyDb.Products.First(p => p.ProductId == product.ProductId);

        updated.ProductName.Should().Be("Updated Product");
        updated.Price.Should().Be(200);
    }


    [Fact]
    public async Task UpdateCategory_Return400_WhenInvalidPayload()
    {
        // ------ Arrange
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var product = db.Products.First();

        // ----- Act

        var dto = new ProductUpdateDto(
                    ProductId: product.ProductId,
                    ProductName: "",              // invalid name 
                    Price: 200,
                    QtyInStock: 20,
                    CategoryId: product.CtgryId,
                    RowVersion: Convert.ToBase64String(product.RowVersion)
                );

        var response
            = await client.PutAsJsonAsync($"/api/products/{product.ProductId}", dto, TestContext.Current.CancellationToken);

        // ----- Assert that the response is BadRequest
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);         // HTTP 400 "Bad Request"

        // ----- Assert that the response provides ProblemDetails
        var problem
            = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestContext.Current.CancellationToken);
        problem.Should().NotBeNull();

        // ----- Assert that the ProblemDetails provides info on Name
        problem!.Errors.Should().ContainKey("ProductName");
    }


    [Fact]
    public async Task UpdateProduct_ShouldReturn400_WhenRouteBodyMismatch()
    {
        // ----- Arrange

        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var product = db.Products.First();

        // ----- Arrange

        var dto = new ProductUpdateDto(
            ProductId: Guid.NewGuid(),          // mismatch
            ProductName: product.ProductName,
            Price: product.Price,
            QtyInStock: product.QtyInStock,
            CategoryId: product.CtgryId,
            RowVersion: Convert.ToBase64String(product.RowVersion)
        );

        var response 
            = await client.PutAsJsonAsync( $"/api/products/{product.ProductId}", dto, TestContext.Current.CancellationToken);

        // ----- Act

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }


    [Fact]
    public async Task UpdateProduct_ShouldReturn400_WhenRowVersionInvalidFormat()
    {
        // ----- Arrange

        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var product = db.Products.First();

        var dto = new ProductUpdateDto(
            ProductId: product.ProductId,
            ProductName: product.ProductName,
            Price: product.Price,
            QtyInStock: product.QtyInStock,
            CategoryId: product.CtgryId,
            RowVersion: "INVALID_BASE64"     // invalid format
        );

        // ----- Act

        var response 
            = await client.PutAsJsonAsync($"/api/products/{product.ProductId}", dto, TestContext.Current.CancellationToken);

        // ----- Assert

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem 
            = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestContext.Current.CancellationToken);

        // Assert that problem details response returned, and it contains info about RowVersion
        problem.Should().NotBeNull();
        problem!.Errors.Should().ContainKey("RowVersion");
    }


    [Fact]
    public async Task UpdateProduct_ShouldReturn404_WhenNotFound()
    {
        // ----- Arrange

        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        // ----- Act

        var dto = new ProductUpdateDto(
            ProductId: Guid.NewGuid(),
            ProductName: "Ghost",
            Price: 10,
            QtyInStock: 1,
            CategoryId: 1,
            RowVersion: Convert.ToBase64String(Guid.NewGuid().ToByteArray())
        );

        var response 
            = await client.PutAsJsonAsync( $"/api/products/{dto.ProductId}", dto, TestContext.Current.CancellationToken);

        // ----- Assert

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);           // HTTP 404 "Not Found"
    }


    [Fact]
    public async Task UpdateProduct_ShouldReturn404_WhenCategoryDoesNotExist()
    {
        // ----- Arrange

        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var product = db.Products.First();

        var dto = new ProductUpdateDto(
            ProductId: product.ProductId,
            ProductName: product.ProductName,
            Price: product.Price,
            QtyInStock: product.QtyInStock,
            CategoryId: 999999,                                         // non-existent category
            RowVersion: Convert.ToBase64String(product.RowVersion)
        );

        // ----- Act

        var response 
            = await client.PutAsJsonAsync( $"/api/products/{product.ProductId}", dto, TestContext.Current.CancellationToken);

        // ----- Assert

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var problem 
            = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);

        problem.Should().NotBeNull();
        problem!.Status.Should().Be(StatusCodes.Status404NotFound);             // HTTP 404 "Not Found"
    }


    [Fact]
    public async Task UpdateProduct_ShouldReturn409_WhenDuplicateNameInSameCategory()
    {
        // ----- Arrange

        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // take two products from the seeded database - each from same category
        var group = db.Products
            .AsEnumerable()                     // ensure in-memory grouping
            .GroupBy(p => p.CtgryId)
            .First(g => g.Count() >= 2);

        var first = group.ElementAt(0);
        var second = group.ElementAt(1);

        /********
        // take a product from the seeded database - each from different categories
        var products = db.Products
            .GroupBy(p => p.CtgryId)            // Group by the category ID
            .Select(g => g.FirstOrDefault())    // Pick the first product in each category
            .Take(2)                            // Limit to only 2 products total
            .ToList();
        var first = products[0];
        var second = products[1];
        ******/

        // ----- Act

        var dto = new ProductUpdateDto(
            ProductId: first.ProductId,
            ProductName: second.ProductName,            // duplicate name in same Category
            Price: first.Price,
            QtyInStock: first.QtyInStock,
            CategoryId: first.CtgryId,
            RowVersion: Convert.ToBase64String(first.RowVersion)
        );

        var response 
            = await client.PutAsJsonAsync( $"/api/products/{first.ProductId}", dto, TestContext.Current.CancellationToken);

        // ----- Assert

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);           // HTTP 409 "Conflict"
    }


    [Fact]
    public async Task UpdateProduct_ShouldAllowMovingToDifferentCategory()
    {
        // ---- Arrange

        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var product = db.Products.First();
        var otherCategory = db.Categories.First(c => c.CategoryId != product.CtgryId);

        var dto = new ProductUpdateDto(
            ProductId: product.ProductId,
            ProductName: product.ProductName,
            Price: product.Price,
            QtyInStock: product.QtyInStock,
            CategoryId: otherCategory.CategoryId,
            RowVersion: Convert.ToBase64String(product.RowVersion)
        );

        // ----- Act

        var response 
            = await client.PutAsJsonAsync($"/api/products/{product.ProductId}", dto, TestContext.Current.CancellationToken);

        // ----- Assert

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);           // HTTP 202 "Accepted"
    }


    [Fact]
    public async Task UpdateProduct_ShouldReturn412_WhenConcurrencyConflict()
    {
        // ----- Arrange

        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        Guid productId;
        int oldCategoryId;
        string oldRowVersion;

        // read initial state of the product, including RowVersion, which will be used for concurrency check
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var product = db.Products.First();
            productId = product.ProductId;
            oldCategoryId = product.CtgryId;
            oldRowVersion = Convert.ToBase64String(product.RowVersion);
        }


        // ----- Act - Simulate another update to the same product, which will change the RowVersion in the database
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var product = db.Products.Single(p => p.ProductId == productId);
            product.ProductName = "Changed Elsewhere";
            db.SaveChanges();                   // generates new RowVersion in the database
        }

        // ----- Act - call API to update

        var dto = new ProductUpdateDto(
            ProductId: productId,
            ProductName: "My Update",
            Price: 100,
            QtyInStock: 5,
            CategoryId: oldCategoryId,
            RowVersion: oldRowVersion
        );

        var response 
            = await client.PutAsJsonAsync( $"/api/products/{productId}", dto, TestContext.Current.CancellationToken );

        // ----- Assert

        response.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
    }

}
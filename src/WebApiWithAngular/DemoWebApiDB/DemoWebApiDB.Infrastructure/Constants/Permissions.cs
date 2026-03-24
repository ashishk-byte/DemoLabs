namespace DemoWebApiDB.Infrastructure.Constants;


/// <summary>
///     Defines the Strongly Typed Permission Constants used for authorization checks throughout the application.
/// </summary>
/// <remarks>
///     Defined to ensure consistency and maintainability when referencing permissions in code, 
///     thus avoiding "magic strings" and typos.
///     
///     To configure the permission on an API endpoint:
///         [Authorize(Policy = Permissions.CanAddCategory)]
///         [Authorize(Roles = "Admin")]
/// </remarks>
public static class Permissions
{

    // Category Permissions
    public const string CanAddCategory = "CanAddCategory";
    public const string CanEditCategory = "CanEditCategory";
    public const string CanDeleteCategory = "CanDeleteCategory";
    public const string CanViewCategory = "CanViewCategory";


    // Product Permissions
    public const string CanAddProduct = "CanAddProduct";
    public const string CanEditProduct = "CanEditProduct";
    public const string CanDeleteProduct = "CanDeleteProduct";
    public const string CanViewProduct = "CanViewProduct";


    // Report Permissions
    public const string CanViewReports = "CanViewReports";


    /// <summary>
    ///     Defines a collection of all permission constants for easy reference and iteration when needed 
    ///     (e.g., seeding the database, checking permissions).
    /// </summary>
    public static IReadOnlyCollection<string> All =>
    [
        CanAddCategory,
        CanEditCategory,
        CanDeleteCategory,
        CanViewCategory,
        CanAddProduct,
        CanEditProduct,
        CanDeleteProduct,
        CanViewProduct,
        CanViewReports
    ];

}
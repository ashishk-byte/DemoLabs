namespace DemoWebApiDB.Data.Constants;

public static class Permissions
{
    public const string CanAddCategory = "CanAddCategory";
    public const string CanEditCategory = "CanEditCategory";
    public const string CanDeleteCategory = "CanDeleteCategory";
    public const string CanViewCategory = "CanViewCategory";    

    public const string CanAddProduct = "CanAddProduct";
    public const string CanEditProduct = "CanEditProduct";
    public const string CanDeleteProduct = "CanDeleteProduct";
    public const string CanViewProduct = "CanViewProduct";


    public const string CanViewReports = "CanViewReports";


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

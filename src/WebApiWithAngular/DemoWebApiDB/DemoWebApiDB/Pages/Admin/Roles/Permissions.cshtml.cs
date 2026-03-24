using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;


namespace DemoWebApiDB.Pages.Admin.Roles;


/// <summary>
///     Allows assigning and removing of permissions to an Application Role.
/// </summary>
[Authorize(Roles = "Admin")]
public class PermissionsModel : PageModel
{

    private readonly ILogger<PermissionsModel> _logger;
    private readonly ApplicationDbContext _context;
    private readonly RoleManager<ApplicationRole> _roleManager;


    /// <summary>
    ///     Gets or sets the Application Role, selected in the UI 
    /// </summary>
    public ApplicationRole Role { get; set; } = default!;


    /// <summary>
    ///     Gets or sets the collection of Permissions assigned to the Role.
    /// </summary>
    /// <remarks>
    ///     PermissionItem represents a single permission with its Id, Name, and a flag to indicate if it is assigned to the role.
    ///     PermissionItem is the ViewModel for the Razor Page.
    ///     This property acts as the InputModel for the Razor Page, allowing the view to display all the permissions 
    ///       and indicate which ones are currently assigned to the role.
    /// </remarks>
    public List<PermissionItem> Permissions { get; set; } = new();


    /// <summary>
    ///     Constructor
    /// </summary>
    public PermissionsModel(
        ILogger<PermissionsModel> logger,
        ApplicationDbContext context,
        RoleManager<ApplicationRole> roleManager)
    {
        _logger = logger;
        _context = context;
        _roleManager = roleManager;
    }


    /// <summary>
    ///     Gets or sets the list of permission identifiers selected by the user.
    /// </summary>
    /// <remarks>
    ///     This property is typically used to bind selected permissions from the form input in ASP.NET MVC. 
    ///     The list will contain only those items that have been "checked" to indicate that permission is granted.</remarks>
    [BindProperty]
    public List<int> SelectedPermissions { get; set; } = new();


    /// <summary>
    ///     On GET handler for the Razor Page. 
    ///     Retrieves the permissions for the role based on the "roleId" parameter, retrieves all permissions,
    /// </summary>
    /// <param name="roleId">
    ///     The unique identifier of the role whose permissions are to be shown. 
    ///     Cannot be null or empty.
    /// </param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException">
    ///     If the role specified by "roleId" querystring parameter is not found, 
    ///     an InvalidOperationException is thrown with the message "Role not found".
    /// </exception>
    public async Task<IActionResult> OnGetAsync(string roleId)
    {
        // Retrieve the role based on the "roleId" parameter passed in the query string to the page
        // (from the Index listing page).
        Role = await _roleManager.FindByIdAsync(roleId)
            ?? throw new InvalidOperationException("Role not found");

        // Retrieve all permissions from the database 
        var allPermissions 
            = await _context.Permissions.ToListAsync();

        // Retrieve the permissions currently assigned to the role
        // by querying the RolePermissions table for entries matching the selected Role's RoleId.
        var rolePermissions = await _context.RolePermissions
            .Where(rp => rp.RoleId == Role.Id)
            .Select(rp => rp.PermissionId)
            .ToListAsync();

        // Create a list of PermissionItem objects to represent all permissions
        // and indicate which ones have been currently assigned to the role.
        Permissions = allPermissions
            .Select(p => new PermissionItem
            {
                Id = p.Id,
                Name = p.Name,
                IsAssigned = rolePermissions.Contains(p.Id),
                DisplayName = ExtractPermissionAction(p.Name),
                Group = ExtractPermissionGroup(p.Name)
            })
            .ToList();

        return Page();
    }



    /// <summary>
    ///     On POST handler for the Razor Page.
    ///     It updates the permissions associated with the specified role and redirects to the index page.
    /// </summary>
    /// <remarks>
    ///     Existing permissions for the role are removed before new permissions are assigned. 
    ///     Then, changes are persisted to the database, asynchronously.
    /// </remarks>
    /// <param name="roleId">
    ///     The unique identifier of the role whose permissions are being updated. 
    ///     Cannot be null or empty.
    /// </param>
    /// <returns>
    ///     A Redirect Action Result that navigates the browser to the Index page,
    ///     after the permissions have been updated sucessfully to the database.
    /// </returns>
    public async Task<IActionResult> OnPostAsync(string roleId)
    {
        // Query the RolePermissions table for current permissions for the selected Role's RoleId
        var existing = _context.RolePermissions
            .Where(r => r.RoleId == roleId);

        if (existing.Any())
        {
            // Remove existing permissions for the role
            // by deleting the entries from the RolePermissions table. 
            _context.RolePermissions.RemoveRange(existing);
        }

        if (SelectedPermissions.Any())
        {
            // Add new permissions for the role based on the list of SelectedPermissions received from the form input.
            foreach (var permissionId in SelectedPermissions)
            {
                _context.RolePermissions.Add(new RolePermission
                {
                    RoleId = roleId,
                    PermissionId = permissionId
                });
            }

            await _context.SaveChangesAsync();
        }

        return RedirectToPage("Index");
    }



    #region Helper Members

    /// <summary>
    ///     Extracts the action portion from the permission name.
    ///     Example:
    ///         CanAddCategory → Add
    ///         CanEditProduct → Edit
    /// </summary>
    private static string ExtractPermissionAction(string permissionName)
    {
        if (permissionName.StartsWith("CanAdd"))
        {
            return "Add";
        }

        if (permissionName.StartsWith("CanEdit"))
        {
            return "Edit";
        }

        if (permissionName.StartsWith("CanDelete"))
        {
            return "Delete";
        }

        if (permissionName.StartsWith("CanView"))
        {
            return "View";
        }

        return permissionName;
    }
    

    /// <summary>
    ///     Extracts the logical group from the Permission name.
    ///     Example: "CanAddCategory" → "Category".
    /// </summary>
    private static string ExtractPermissionGroup(string permissionName)
    {
        if (permissionName.EndsWith("Category"))
        {
            return "Category";
        }

        if (permissionName.EndsWith("Product"))
        {
            return "Product";
        }

        if (permissionName.EndsWith("Reports"))
        {
            return "Reports";
        }

        return "Other";
    }


    /// <summary>
    ///     The ViewModel for displaying permissions in the UI.
    /// </summary>
    /// <remarks>
    ///     Represents a single permission item with its Id, Name, and a flag to indicate if it is assigned to the role.
    ///     This class acts as the ViewModel for the Razor Page.
    /// </remarks>
    public class PermissionItem
    {

        /// <summary>
        ///     The Id of the Permission
        /// </summary>
        public int Id { get; set; }


        /// <summary>
        ///     The name of the Permission
        /// </summary>
        public string Name { get; set; } = string.Empty;


        /// <summary>
        ///     The Display-friendly action name (Add, Edit, Delete, View).
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;


        /// <summary>
        ///     Gets or sets a boolean flag to indicate if the permission has been assigned to the Role.
        /// </summary>
        public bool IsAssigned { get; set; }


        /// <summary>
        ///     Logical group derived from the Permission Name
        ///     (Category, Product, Reports).
        /// </summary>
        public string Group { get; set; } = string.Empty;

    }

    #endregion

}
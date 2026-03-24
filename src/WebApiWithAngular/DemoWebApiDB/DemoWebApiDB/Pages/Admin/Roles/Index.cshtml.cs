using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;


namespace DemoWebApiDB.Pages.Admin.Roles;


/// <summary>
///     Displays all the Roles in the Application.
/// </summary>
/// <remarks>
///     URL: https://localhost:7123/Admin/Roles/Index
///     Will report 401, if unauthorized (meaning, un-authenticated)!
///     Will report 403, if forbidden    (meaning, no permissions)
/// </remarks>
[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{

    private readonly ILogger<IndexModel> _logger;
    private readonly RoleManager<ApplicationRole> _roleManager;


    /// <summary>
    ///     List of roles displayed in the UI.
    /// </summary>
    public List<ApplicationRole> Roles { get; private set; } = new();


    /// <summary>
    ///     Constructor.
    /// </summary>
    public IndexModel(
        ILogger<IndexModel> logger,
        RoleManager<ApplicationRole> roleManager)
    {
        _logger = logger;
        _roleManager = roleManager;
    }


    /// <summary>
    ///     Retrieves Roles from the database using RoleManager.
    /// </summary>
    public void OnGet()
    {
        Roles = _roleManager.Roles.ToList();
    }

}

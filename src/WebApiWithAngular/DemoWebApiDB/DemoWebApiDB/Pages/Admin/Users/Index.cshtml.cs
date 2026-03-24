using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;


namespace DemoWebApiDB.Pages.Admin.Users;


/// <summary>
/// Displays all users in the system.
/// </summary>
[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{

    private readonly ILogger<IndexModel> _logger;
    private readonly UserManager<ApplicationUser> _userManager;


    /// <summary>
    ///     List of users in the application.
    /// </summary>
    public List<UserViewModel> Users { get; set; } = new();


    /// <summary>
    ///     Constructor.
    /// </summary>
    public IndexModel(
        ILogger<IndexModel> logger,
        UserManager<ApplicationUser> userManager)
    {
        _logger = logger;
        _userManager = userManager;
    }


    /// <summary>
    ///     Retrieves the list of Users from the database, using UserManager.
    /// </summary>
    public void OnGet()
    {
        Users = _userManager.Users
                .Select(u => new UserViewModel
                {
                    Id = u.Id,
                    Username = u.UserName!,
                    FullName = u.FullName!,
                    Email = u.Email!
                })
                .ToList();
    }


    #region Helper members


    /// <summary>
    ///     The View Model used to display users in the list of Users.
    /// </summary>
    public class UserViewModel
    {

        /// <summary>
        ///     User identifier.
        /// </summary>
        public string Id { get; set; } = string.Empty;


        /// <summary>
        ///     Username of the User
        /// </summary>
        public string Username { get; set; } = string.Empty;


        /// <summary>
        ///     Display Name of the User
        /// </summary>
        public string FullName { get; set; } = string.Empty;


        /// <summary>
        ///     Email Address of the User.
        /// </summary>
        public string Email { get; set; } = string.Empty;

    }

    #endregion
}

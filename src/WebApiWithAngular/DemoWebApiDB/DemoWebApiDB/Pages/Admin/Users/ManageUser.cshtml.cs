using System.ComponentModel.DataAnnotations;
using System.Data;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;


namespace DemoWebApiDB.Pages.Admin.Users;


/// <summary>
///     Page used to create or edit a user and assign roles.
/// </summary>
[Authorize(Roles = "Admin")]
public class ManageUserModel : PageModel
{

    private readonly ILogger<ManageUserModel> _logger;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;


    /// <summary>
    ///     Constructor.
    /// </summary>
    public ManageUserModel(
        ILogger<ManageUserModel> logger,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager)
    {
        _logger = logger;
        _userManager = userManager;
        _roleManager = roleManager;
    }


    /// <summary>
    ///     The ViewModel object, bound to the Razor Page form.
    /// </summary>
    [BindProperty]
    public UserEditViewModel UserViewModel { get; set; } = new();


    /// <summary>
    ///     Loads the details of the User and any Roles that might have been assigned to the User.
    /// </summary>
    /// <param name="id">
    ///     User identifier (optional).  
    ///     Will be NULL for a new user, else will map to the selected user for edit.
    /// </param>
    public async Task<IActionResult> OnGetAsync(string? id)
    {

        // If ID is null, we are creating a new user - So, show empty form.
        // Else, continue to populate the viewmodel object, so that we can edit the user details.
        if (string.IsNullOrEmpty(id))
        {
            // Populate the role selection list from the list of all roles, indicating if the role has been selected
            BuildRoleList();            // since new user, all roles will be unchecked!

            return Page();
        }

        // Retrieve the existing details for the User, using the UserManager
        // NOTE: To get all the roles associated to the user, using the GetRolesAsync() method of UserManager,
        //       you need the user identity object.  That is why, we use FindById(), first.
        var user = await _userManager.FindByIdAsync(id);

        if (user is null)
        {
            return NotFound();
        }

        // Populate the ViewModel
        UserViewModel = new UserEditViewModel
        {
            Id = user.Id,
            Email = user.Email!,
            FullName = user.FullName ?? null
        };

        // Retrieve the roles currently associated to the user.
        var currentRoles = (await _userManager.GetRolesAsync(user)).ToList();

        // Populate the role selection list from the list of all roles, indicating if the role has been selected
        BuildRoleList(currentRoles);

        return Page();
    }


    /// <summary>
    /// Saves user and role assignments.
    /// </summary>
    public async Task<IActionResult> OnPostAsync()
    {
        // Sanitize the data 
        UserViewModel.FullName = UserViewModel.FullName?.Trim();
        UserViewModel.Email = UserViewModel.Email.Trim().ToLowerInvariant();
        UserViewModel.Password = UserViewModel.Password?.Trim();

        // Extract selected roles from the ViewModel
        var selectedRoles = UserViewModel.SelectedRoles
            .Where(r => r.Selected)
            .Select(r => r.RoleName)
            .ToList();

        // Populate the role selection list from the list of all roles, indicating if the role has been selected
        BuildRoleList(selectedRoles);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Check if the email address is a duplicate
        var existingUser = await _userManager.FindByEmailAsync(UserViewModel.Email);
        if (existingUser != null && existingUser.Id != UserViewModel.Id)
        {
            ModelState.AddModelError("UserViewModel.Email", "Email address already exists.");
            return Page();
        }

        ApplicationUser user;

        if (string.IsNullOrEmpty(UserViewModel.Id))
        {
            // ------ CREATE USER

            // ensure that the password is not empty.
            if(string.IsNullOrEmpty(UserViewModel.Password))
            {
                // IMPORTANT NOTE:
                // The name of the rendered textbox in the HTML form will have "_" instead of "."!
                ModelState.AddModelError("UserViewModel.Password", "Password cannot be empty!");
                return Page();
            }

            user = new ApplicationUser
            {
                Email = UserViewModel.Email,
                UserName = UserViewModel.Email,             // NOTE: for now, username == email
                FullName = UserViewModel.FullName,
                EmailConfirmed = true                       // TODO: in next version, implement email workflow
            };

            var result = await _userManager.CreateAsync(user, UserViewModel.Password!);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return Page();
            }
        }
        else
        {
            // ------ UPDATE USER (note: Password cannot be changed this way!!!!!)

            user = await _userManager.FindByIdAsync(UserViewModel.Id)
                ?? throw new InvalidOperationException("User not found");

            user.Email = UserViewModel.Email;
            user.UserName = UserViewModel.Email;        // NOTE: for now, username == email
            user.FullName = UserViewModel.FullName;
            user.EmailConfirmed = true;                 // TODO: in next version, implement email workflow

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }

                return Page();
            }
        }


        // ---------- BUSINESS RULE: SAFETY CHECK - ENSURE AT LEAST ONE ADMIN REMAINS

        if (!selectedRoles.Contains("Admin"))
        {
            var adminCount = await _userManager.GetUsersInRoleAsync("Admin");

            // If the user is currently an Admin and the only one left
            if (adminCount.Count == 1 && adminCount.First().Id == user.Id)
            {
                ModelState.AddModelError("", "At least one administrator must remain in the system.");

                BuildRoleList(selectedRoles);

                return Page();
            }
        }



        // ---------- UPDATE USER ROLES 

        var existingRoles = await _userManager.GetRolesAsync(user);

        // remove existing roles
        await _userManager.RemoveFromRolesAsync(user, existingRoles);       

        if (selectedRoles.Any())
        {
            // add the mapped roles now
            await _userManager.AddToRolesAsync(user, selectedRoles);

            _logger.LogInformation(
                "User {Email} roles updated: {Roles}",
                user.Email,
                string.Join(",", selectedRoles));
        }


        // ---------- REDIRECT TO THE USERS LISTING PAGE

        return RedirectToPage("Index");
    }



    #region Helper members


    /// <summary>
    ///     Populate the role selection list from the list of all roles, indicating if the role has been selected
    ///     - A method called during the GET and POST 
    ///     - to ensure that the role selection list for the ViewModel is built every time.
    /// </summary>
    private void BuildRoleList(IEnumerable<string>? selectedRoles = null)
    {
        selectedRoles ??= Enumerable.Empty<string>();

        UserViewModel.SelectedRoles = _roleManager.Roles
            .Select(r => new RoleItemViewModel
            {
                RoleName = r.Name!,
                Selected = selectedRoles.Contains(r.Name!)
            })
            .ToList();
    }


    /// <summary>
    ///     The View model used by ModelBinding to the FORM in the ManageUser Razor Page.
    /// </summary>
    /// <remarks>
    ///     This model represents only the fields required by the UI
    ///     and prevents exposing the full Identity User entity.
    /// </remarks>
    public class UserEditViewModel
    {

        /// <summary>
        ///     User identifier.
        ///     Will be NULL when creating a new user.
        /// </summary>
        public string? Id { get; set; }


        /// <summary>
        ///     Display Name of the user.
        /// </summary>
        [Display(Name = "Full name")]
        [Required(ErrorMessage = "{0} cannot be empty.")]
        [StringLength(maximumLength: 60, 
                      MinimumLength = 4,
                      ErrorMessage = "{0} should have between {2} and {1} characters.")]
        public string? FullName { get; set; } = string.Empty;


        /// <summary>
        ///     Email address of the user.
        /// </summary>
        [Required(ErrorMessage = "{0} cannot be empty.")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;


        /// <summary>
        ///     Password used only during creation of a new user.
        /// </summary>
        /// <remarks>
        ///     NOTE: It is not marked REQUIRED because it is required for NEW USER only!
        ///           This is validated OnPost() server-side only!
        /// </remarks>
        [StringLength(maximumLength:32, 
            MinimumLength = 8, 
            ErrorMessage = "{0} should have between {2} and {1} characters.") ]
        public string? Password { get; set; }


        /// <summary>
        ///     Roles for the user.
        /// </summary>
        public List<RoleItemViewModel> SelectedRoles { get; set; } = new();
    
    }



    /// <summary>
    ///     Role selection item used by the UI.
    /// </summary>
    public class RoleItemViewModel
    {
        public string RoleName { get; set; } = string.Empty;

        public bool Selected { get; set; }
    }

    #endregion

}
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;


namespace DemoWebApiDB.Pages.Account;


/// <summary>
///     Login page used for Razor Admin authentication.
/// </summary>
public class LoginModel : PageModel
{
    private const string DefaultRedirectURL = "/Admin/Roles";

    private readonly SignInManager<ApplicationUser> _signInManager;

    /// <summary>
    ///     The Redirect URL after successful login.
    /// </summary>
    public string? ReturnUrl { get; set; }


    /// <summary>
    ///     The Input ViewModel used for login form binding.
    /// </summary>
    [BindProperty]
    public LoginViewModel Input { get; set; } = new();


    public LoginModel(SignInManager<ApplicationUser> signInManager)
    {
        _signInManager = signInManager;
    }


   
    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? DefaultRedirectURL;
    }


    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= DefaultRedirectURL;

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await _signInManager.PasswordSignInAsync(
            Input.Email,
            Input.Password,
            Input.RememberMe,
            lockoutOnFailure: false
        );

        if (result.Succeeded)
        {
            return LocalRedirect(returnUrl);
        }

        ModelState.AddModelError("", "Invalid login credentials.");

        return Page();
    }


    /// <summary>
    ///     The ViewModel for the Login form.
    /// </summary>
    public class LoginViewModel
    {

        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        public bool RememberMe { get; set; }

    }

}
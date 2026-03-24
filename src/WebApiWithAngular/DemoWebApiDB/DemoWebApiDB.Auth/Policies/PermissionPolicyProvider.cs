using DemoWebApiDB.Infrastructure.Constants;

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;


namespace DemoWebApiDB.Auth.Policies;


/// <summary>
///     Dynamically creates authorization policies based on permission names.
/// </summary>
/// <remarks
///     This provider intercepts policy requests from ASP.NET Core and
///     dynamically builds policies when the requested policy name matches
///     one of the defined application permissions.
///
///     This avoids the need to manually register every single policy in Program.cs.
///     All one now needs to do is define using the policy like:
///         [Authorize(Roles = "Admin")]
///                 or
///         [Authorize(Policy = Permissions.CanAddCategory)]
///     without having to explicitly register the policy in Program.cs
/// </remarks>
public sealed class PermissionPolicyProvider : DefaultAuthorizationPolicyProvider
{

    /// <summary>
    ///     Initializes a new instance of the <see cref="PermissionPolicyProvider"/>.
    /// </summary>
    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
        : base(options)
    {
    }


    /// <summary>
    ///     Returns an authorization policy for the given policy name.
    /// </summary>
    /// <param name="policyName">Name of the policy requested by the Authorize attribute.</param>
    /// <returns>
    ///     A dynamically constructed authorization policy if the name matches a valid permission; 
    ///     otherwise the default provider is used.
    /// </returns>
    /// <remarks>
    ///     NOTE: 
    ///     This demonstrates "Defensive Programming" by validating the policy name against a known list of permissions.
    ///     And throws an exception to "fail fast" if an invalid policy name is requested, 
    ///     which can help catch configuration errors early in development.
    /// </remarks>
    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // Check whether the requested policy name matches a known permission.
        if (Permissions.All.Contains(policyName))
        {
            var policy = new AuthorizationPolicyBuilder()
                .AddRequirements(new PermissionRequirement(policyName))
                .Build();

            return policy;
        }

        // --- If the policy is not a known permission, fall back to the default policy provider.
        // return await base.GetPolicyAsync(policyName);

        // --- Or, if you want to be more strict and fail fast, you can throw an exception instead:
        throw new InvalidOperationException(
            $"Authorization policy '{policyName}' does not match a known permission.");
    }

}

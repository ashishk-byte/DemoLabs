using DemoWebApiDB.Auth.Policies;
using DemoWebApiDB.Infrastructure.Constants;

using Microsoft.AspNetCore.Authorization;


namespace DemoWebApiDB.Auth.Handlers;


/// <summary>
///     Handles authorization checks for permission-based policies.
/// </summary>
/// <remarks>
///     This handler verifies whether the current authenticated user
///     possesses the required permission claim required by the policy.
/// </remarks>
public sealed class PermissionAuthorizationHandler
    : AuthorizationHandler<PermissionRequirement>
{

    /// <summary>
    ///     Evaluates whether the user satisfies the permission requirement.
    /// </summary>
    /// <param name="context">Authorization context containing user claims.</param>
    /// <param name="requirement">Permission requirement being evaluated.</param>
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // Ensure a valid authenticated user exists
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        // Retrieve all permission claims associated with the user.
        //   Using Distinct() ensures that duplicate permissions from multiple roles are evaluated only once.
        // and Check if the user has the required permission claim 
        var hasPermission = context.User
            .FindAll(CustomClaimTypes.Permission)
            .Any(c => string.Equals(c.Value, requirement.Permission, StringComparison.OrdinalIgnoreCase));

        if (hasPermission)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }

}
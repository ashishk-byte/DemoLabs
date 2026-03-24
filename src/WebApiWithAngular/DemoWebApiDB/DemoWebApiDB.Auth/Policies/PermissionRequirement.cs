using Microsoft.AspNetCore.Authorization;


namespace DemoWebApiDB.Auth.Policies;


/// <summary>
///     Represents a permission requirement used by the authorization system.
/// </summary>
/// <remarks>
///     Each permission policy corresponds to a specific permission
///     such as "CanAddCategory" or "CanEditProduct".
/// </remarks>
public sealed class PermissionRequirement 
    : IAuthorizationRequirement
{

    /// <summary>
    ///     Name of the permission required to access the resource.
    /// </summary>
    public string Permission { get; }


    /// <summary>
    ///     Initializes a new instance of the requirement.
    /// </summary>
    /// <param name="permission">Permission name.</param>
    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }

}
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using DemoWebApiDB.Data.Data;
using DemoWebApiDB.Data.Entities;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;


namespace DemoWebApiDB.Auth.Services;


/// <summary>
///     Responsible for generating JWT tokens for authenticated users.
/// </summary>
/// <remarks>
///     The generated JWT contains:
///     - User identifier
///     - Email
///     - Roles
///     - Permissions
///
///     These claims will later be used by ASP.NET authorization policies.
/// </remarks>
public sealed class TokenService
{

    private readonly IConfiguration _configuration;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;

    /// <summary>
    ///     Initializes a new instance of <see cref="TokenService"/>.
    /// </summary>
    public TokenService(
        IConfiguration configuration,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context)
    {
        _configuration = configuration;
        _userManager = userManager;
        _context = context;
    }


    /// <summary>
    ///     Generates a signed JWT token for the specified user.
    /// </summary>
    /// <param name="user">Authenticated application user.</param>
    /// <returns>JWT token string.</returns>
    public async Task<string> GenerateTokenAsync(ApplicationUser user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");

        var issuer = jwtSettings["Issuer"];
        var audience = jwtSettings["Audience"];
        var secretKey = jwtSettings["SecretKey"];
        var expiry = jwtSettings["ExpiryMinutes"] ?? "60";
        var expiryMinutes = int.Parse(jwtSettings["ExpiryMinutes"] ?? "60");        // fallback 60 minutes if not set

        // Create a signing key
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Collect the claims
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(ClaimTypes.NameIdentifier, user.Id)
        };

        // Add the roles of the user as claims
        var roles = await _userManager.GetRolesAsync(user);
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // Add permissions associated with the user's roles as claims
        var permissions = await GetPermissionsOfUserRolesAsync(roles);
        foreach (var permission in permissions)
        {
            claims.Add(new Claim("permission", permission));
        }

        // Create the JWT Token
        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }



    /// <summary>
    ///     Retrieves permissions associated with the supplied roles.
    /// </summary>
    /// <param name="roles">User roles.</param>
    /// <returns>List of permission names.</returns>
    private async Task<List<string>> GetPermissionsOfUserRolesAsync(IList<string> roles)
    {

        // Step 1: Retrieve the RoleIds corresponding to the roles from the Roles table.
        var roleIds = await _context.Roles
            .Where(r => roles.Contains(r.Name!))
            .Select(r => r.Id)
            .ToListAsync();

        // Step 2: Retrieve permissions mapped to those RoleIds from the RolePermissions table.
        var permissionIds = await _context.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Select(rp => rp.PermissionId)
            .Distinct()
            .ToListAsync();

        // Step 3: Retrieve permission names from the Permissions table.
        var permissions = await _context.Permissions
            .Where(p => permissionIds.Contains(p.Id))
            .Select(p => p.Name)
            .ToListAsync();

        return permissions;
    }

}
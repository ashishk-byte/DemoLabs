using DemoWebApiDB.Auth.DTOs;
using DemoWebApiDB.Data.Entities;
using DemoWebApiDB.Infrastructure.Results;

using Microsoft.AspNetCore.Identity;


namespace DemoWebApiDB.Auth.Services;


/// <summary>
///     Handles authentication operations such as login.
/// </summary>
public sealed class AuthService
{

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly TokenService _tokenService;


    public AuthService(
        UserManager<ApplicationUser> userManager,
        TokenService tokenService)
    {
        _userManager = userManager;
        _tokenService = tokenService;
    }


    /// <summary>
    ///     Authenticates a user and returns a JWT token if credentials are valid.
    /// </summary>
    /// <param name="dto">DTO representing the login credentials.</param>
    /// <remarks>
    ///     Implements the Result Pattern configured in the Infrastructure project, 
    ///     to provide a consistent way of handling success and failure cases, including unauthorized access.
    /// </remarks>
    public async Task<Result<string>> LoginAsync(LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);

        if (user is null)
        {
            return Result<string>.Unauthorized("Invalid login credentials");
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, dto.Password);

        if (!passwordValid)
        {
            return Result<string>.Unauthorized("Invalid login credentials");
        }

        var token = await _tokenService.GenerateTokenAsync(user);
        return Result<string>.Success(token);
    }

}
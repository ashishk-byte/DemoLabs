using DemoWebApiDB.Auth.DTOs;
using DemoWebApiDB.Auth.Services;

using Microsoft.AspNetCore.Authorization;

namespace DemoWebApiDB.Controllers;


/// <summary>
///     Provides endpoints related to authentication.
/// </summary>
[Route("api/auth")]
public sealed class AuthController 
    : BaseApiController
{
    
    private readonly AuthService _authService;


    /// <summary>
    ///     Initializes a new instance of <see cref="AuthController"/>.
    /// </summary>
    public AuthController(AuthService authService)
    {
        _authService = authService;
    }


    /// <summary>
    ///     Authenticates a user and returns a JWT token.
    /// </summary>
    /// <param name="dto">Login credentials.</param>
    /// <returns>JWT token if authentication succeeds.</returns>
    [AllowAnonymous]
    [HttpPost("login")]
    [EndpointDescription(
        "Authenticates login credentials containing email and password.  "
        + "Returns 200 with the JWT Token, if valid credentials provided."
    )]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, 
                          Description = "Invalid login credentials")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var result = await _authService.LoginAsync(dto);

        return HandleResult(result);
    }

}
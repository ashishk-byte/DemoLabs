namespace DemoWebApiDB.Auth.DTOs;


/// <summary>
///     DTO used for authenticating a user.
/// </summary>
/// <remarks>
///     This model represents the credentials required for login.
///     The API will validate these credentials using ASP.NET Identity
///     and issue a JWT token if authentication is successful.
/// </remarks>
public sealed record LoginDto
(

    /// <summary>
    ///     Email address of the user attempting login.
    /// </summary>
    string Email,


    /// <summary>
    ///     Plain-text password provided by the user.
    /// </summary>
    string Password

);

using Microsoft.AspNetCore.Identity;


namespace DemoWebApiDB.Data.Entities;


public class ApplicationUser : IdentityUser
{

    public string? FullName { get; set; }

}

using DemoWebApiDB.Infrastructure.Constants;
using Microsoft.AspNetCore.Identity;


namespace DemoWebApiDB.Data.Data;


public static class IdentitySeeder
{

    public static async Task SeedAsync(
        ApplicationDbContext dbContext,
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager)
    {
        await dbContext.Database.MigrateAsync();

        await SeedPermissions(dbContext);

        await SeedRoles(roleManager);

        await SeedRolePermissions(dbContext, roleManager);

        await SeedAdminUser(userManager);
    }



    private static async Task SeedPermissions(ApplicationDbContext dbContext)
    {
        foreach (var permission in Permissions.All)
        {
            if (!await dbContext.Permissions.AnyAsync(p => p.Name == permission))
            {
                dbContext.Permissions.Add(new Permission
                {
                    Name = permission
                });
            }
        }

        await dbContext.SaveChangesAsync();
    }


    private static async Task SeedRoles(RoleManager<ApplicationRole> roleManager)
    {
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new ApplicationRole
            {
                Name = "Admin"
            });
        }

        if (!await roleManager.RoleExistsAsync("User"))
        {
            await roleManager.CreateAsync(new ApplicationRole
            {
                Name = "User"
            });
        }
    }


    private static async Task SeedRolePermissions(
        ApplicationDbContext dbContext,
        RoleManager<ApplicationRole> roleManager)
    {
        var adminRole = await roleManager.FindByNameAsync("Admin");
        var userRole = await roleManager.FindByNameAsync("User");

        if (adminRole == null || userRole == null)
        {
            throw new Exception("Roles must be seeded before assigning allPermissions.");
        }

        
        var allPermissions = await dbContext.Permissions.ToListAsync();

        // Admin gets ALL allPermissions
        foreach (var permission in allPermissions)
        {
            if (!await dbContext.RolePermissions.AnyAsync(rp =>
                rp.RoleId == adminRole!.Id 
                && rp.PermissionId == permission.Id))
            {
                dbContext.RolePermissions.Add(new RolePermission
                {
                    RoleId = adminRole.Id,
                    PermissionId = permission.Id
                });
            }
        }

        // User gets limited allPermissions
        var userPermissions = allPermissions
            .Where(p =>
                p.Name == Permissions.CanViewCategory 
                || p.Name == Permissions.CanViewProduct)
            .ToList();

        foreach (var permission in userPermissions)
        {
            if (!await dbContext.RolePermissions.AnyAsync(rp =>
                rp.RoleId == userRole!.Id 
                && rp.PermissionId == permission.Id))
            {
                dbContext.RolePermissions.Add(new RolePermission
                {
                    RoleId = userRole.Id,
                    PermissionId = permission.Id
                });
            }
        }

        await dbContext.SaveChangesAsync();
    }


    private static async Task SeedAdminUser(UserManager<ApplicationUser> userManager)
    {
        const string adminEmail = "admin@demo.com";
        const string adminPassword = "Password@123";

        var user = await userManager.FindByEmailAsync(adminEmail);

        if (user is not null)
        {
            return;
        }

        user = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            FullName = "System Administrator"
        };

        var result = await userManager.CreateAsync(user, adminPassword);

        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, "Admin");
        }
    }

}

namespace DemoWebApiDB.Data.Entities;


/*************************
    NOTE: 
    This entity will have a composite primary key consisting of RoleId and PermissionId.
    EF Core does not support composite key definition using the [Key] attribute.

    The composite key will be configured in the DbContext using Fluent API:
            modelBuilder.Entity<RolePermission>()
                        .HasKey(rp => new { rp.RoleId, rp.PermissionId });

   After defining the composite key in the DbContext, we can define the navigation properties in this entity 
********************/

public class RolePermission
{

    #region Navigation Properties to the ApplicationRole

    public string RoleId { get; set; } = default!;

    public ApplicationRole Role { get; set; } = default!;

    #endregion


    #region Navigation Properties to the Permission

    public int PermissionId { get; set; }


    public Permission Permission { get; set; } = default!;

    #endregion

}

namespace DemoWebApiDB.Data.Entities;


public class Permission
{

    public int Id { get; set; }


    public string Name { get; set; } = string.Empty;


    #region Navigation Properties to the RolePermission

    public ICollection<RolePermission> RolePermissions { get; set; }
        = [];               // C# 12.0 collection initializer, instead of = new List<RolePermission>();

    #endregion

}

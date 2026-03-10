using DemoWebApiDB.Auth.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace DemoWebApiDB.Data.Entities;

public class RolePermission
{
    public string RoleId { get; set; } = default!;

    public ApplicationRole Role { get; set; } = default!;
    public int PermissionId { get; set; }
    public Permission Permission { get; set; } = default!;
}

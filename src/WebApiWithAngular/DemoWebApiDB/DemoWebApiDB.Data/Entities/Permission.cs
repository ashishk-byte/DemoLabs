using System;
using System.Collections.Generic;
using System.Text;

namespace DemoWebApiDB.Auth.Entities
{
    public class Permission
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}

using System.Collections.Generic;

namespace Attendance.Models.Entities;

public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Navigation
    public ICollection<UserRoleEntry> UserRoles { get; set; } = new List<UserRoleEntry>();
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

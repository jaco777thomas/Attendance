using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Attendance.Models.ViewModels;

public class RoleIndexViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int PermissionCount { get; set; }
}

public class RoleEditViewModel
{
    public int Id { get; set; }
    
    [Required, MaxLength(50)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;
    
    public List<int> SelectedPermissionIds { get; set; } = new();
    public List<PermissionViewModel> AvailablePermissions { get; set; } = new();
}

public class PermissionViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
}

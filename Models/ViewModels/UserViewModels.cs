using System.ComponentModel.DataAnnotations;
using Attendance.Models.Entities;

namespace Attendance.Models.ViewModels;

public class LoginViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class UserCreateViewModel
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Phone]
    public string? Phone { get; set; }

    [MinLength(1, ErrorMessage = "Please select at least one role")]
    public List<int> SelectedRoleIds { get; set; } = new();
    public List<Role>? AvailableRoles { get; set; }

    [Required, MinLength(6), DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Compare(nameof(Password)), DataType(DataType.Password)]
    public string? ConfirmPassword { get; set; }

    public string? AssignedClass { get; set; }
    public Division? AssignedDivision { get; set; }
    public Language? AssignedLanguage { get; set; }
}

public class UserEditViewModel
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Phone]
    public string Phone { get; set; } = string.Empty;

    [MinLength(1, ErrorMessage = "Please select at least one role")]
    public List<int> SelectedRoleIds { get; set; } = new();
    public List<Role>? AvailableRoles { get; set; }

    public bool IsActive { get; set; }

    [DataType(DataType.Password), MinLength(6)]
    public string? NewPassword { get; set; }

    public string? AssignedClass { get; set; }
    public Division? AssignedDivision { get; set; }
    public Language? AssignedLanguage { get; set; }
}

public class UserProfileViewModel
{
    public int Id { get; set; }
    [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Phone] public string Phone { get; set; } = string.Empty;
    [DataType(DataType.Password), MinLength(6)] public string? NewPassword { get; set; }
    [Compare(nameof(NewPassword)), DataType(DataType.Password)] public string? ConfirmPassword { get; set; }
}

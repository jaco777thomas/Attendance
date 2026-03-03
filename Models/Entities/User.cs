namespace Attendance.Models.Entities;

[System.Flags]
public enum UserRole 
{ 
    None = 0,
    Teacher = 1,
    AttendanceIncharge = 2,
    HeadMaster = 4,
    DBAdmin = 8,
    NoticeManager = 16,
    StudentManager = 32,
    Admin = 64
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    [System.Obsolete("Use UserRoles and Role entity instead")]
    public UserRole? Role { get; set; }
    
    public ICollection<UserRoleEntry> UserRoles { get; set; } = new List<UserRoleEntry>();

    public bool IsActive { get; set; } = true;
    public string? AssignedClass { get; set; } // For Teachers
    public Division? AssignedDivision { get; set; } // For Teachers
    public Language? AssignedLanguage { get; set; } // For Teachers
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<StaffAttendance> StaffAttendances { get; set; } = new List<StaffAttendance>();
    public ICollection<Notice> CreatedNotices { get; set; } = new List<Notice>();
}

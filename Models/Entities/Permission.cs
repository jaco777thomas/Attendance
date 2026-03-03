using System.Collections.Generic;

namespace Attendance.Models.Entities;

public class Permission
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // Human readable name
    public string Key { get; set; } = string.Empty;  // Unique constant key (e.g., "ViewStudents")

    // Navigation
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

// Help constant for permission keys
public static class PermissionKeys
{
    public const string ViewStudents = "ViewStudents";
    public const string ManageStudents = "ManageStudents";
    public const string ViewTeachers = "ViewTeachers";
    public const string ManageTeachers = "ManageTeachers";
    public const string TakeStudentAttendance = "TakeStudentAttendance";
    public const string TakeTeacherAttendance = "TakeTeacherAttendance";
    public const string ViewReports = "ViewReports";
    public const string ExportReports = "ExportReports";
    public const string ViewNotifications = "ViewNotifications";
    public const string ManageNotifications = "ManageNotifications";
    public const string ViewDashboardOwnClass = "ViewDashboardOwnClass";
    public const string ViewDashboardAllClasses = "ViewDashboardAllClasses";
    public const string UserManagement = "UserManagement";
    public const string RoleManagement = "RoleManagement";
    public const string DatabaseManagement = "DatabaseManagement";
}

using System.ComponentModel.DataAnnotations;
using Attendance.Models.Entities;

namespace Attendance.Models.ViewModels;

public class NoticeCreateViewModel
{
    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Message { get; set; } = string.Empty;

    [Required]
    public NoticeTarget Target { get; set; } = NoticeTarget.All;
}

public class NoticeEditViewModel : NoticeCreateViewModel
{
    public int Id { get; set; }
}

public class PromotionViewModel
{
    public string? FilterClass { get; set; }
    public Division? FilterDivision { get; set; }
    public Language? FilterLanguage { get; set; }
    public bool PromoteAll { get; set; }

    public List<Student> PreviewStudents { get; set; } = new();
    public bool ShowFinalClassWarning { get; set; }
    public string FinalClass { get; set; } = "15";
}

public class DashboardViewModel
{
    public int TotalStudents { get; set; }
    public int ActiveStudents { get; set; }
    public int TotalTeachers { get; set; }
    public int PresentStudentsToday { get; set; }
    public int AbsentStudentsToday { get; set; }
    public int PresentTeachersToday { get; set; }
    public int AbsentTeachersToday { get; set; }
    public List<Notice> RecentNotices { get; set; } = new();
    public bool AttendanceMarkedToday { get; set; }
}

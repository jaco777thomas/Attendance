using System.ComponentModel.DataAnnotations;
using Attendance.Models.Entities;

namespace Attendance.Models.ViewModels;

public class AttendanceMarkViewModel
{
    [Required, DataType(DataType.Date)]
    public DateTime Date { get; set; } = DateTime.Today;

    [Required]
    public string Class { get; set; } = string.Empty;

    [Required]
    public Division Division { get; set; }

    [Required]
    public Language Language { get; set; } = Attendance.Models.Entities.Language.English;

    public List<StudentAttendanceRow> Rows { get; set; } = new();
}

public class StudentAttendanceRow
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;
}

public class StaffAttendanceMarkViewModel
{
    [Required, DataType(DataType.Date)]
    public DateTime Date { get; set; } = DateTime.Today;

    public List<StaffAttendanceRow> Rows { get; set; } = new();
}

public class StaffAttendanceRow
{
    public int UserId { get; set; }
    public string StaffName { get; set; } = string.Empty;
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;
    public bool CanEdit { get; set; }
}

public class ReportFilterViewModel
{
    [DataType(DataType.Date)]
    public DateTime? FromDate { get; set; }

    [DataType(DataType.Date)]
    public DateTime? ToDate { get; set; }

    public string? Class { get; set; }
    public Division? Division { get; set; }
    public Language? Language { get; set; }
    public int? StudentId { get; set; }
    public int? TeacherId { get; set; }

    [Range(1, 12)]
    public int? Month { get; set; }

    [Range(2000, 2100)]
    public int? Year { get; set; }

    public string ReportType { get; set; } = "Student"; // Student | Teacher
}

public class StudentAttendanceReportRow
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string Class { get; set; } = string.Empty;
    public Division Division { get; set; }
    public Language Language { get; set; }
    public int TotalDays { get; set; }
    public int PresentDays { get; set; }
    public double Percentage => TotalDays == 0 ? 0 : Math.Round((double)PresentDays / TotalDays * 100, 1);
}

public class StaffAttendanceReportRow
{
    public int UserId { get; set; }
    public string StaffName { get; set; } = string.Empty;
    public int TotalDays { get; set; }
    public int PresentDays { get; set; }
    public double Percentage => TotalDays == 0 ? 0 : Math.Round((double)PresentDays / TotalDays * 100, 1);
}

public class ReportViewModel
{
    public ReportFilterViewModel Filter { get; set; } = new();
    public List<StudentAttendanceReportRow> StudentRows { get; set; } = new();
    public List<StaffAttendanceReportRow> StaffRows { get; set; } = new();
    public List<Student> Students { get; set; } = new();
    public List<User> Teachers { get; set; } = new();
}

namespace Attendance.Models.Entities;

public enum NoticeTarget { All, Teachers, AttendanceIncharges }

public class Notice
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public NoticeTarget Target { get; set; } = NoticeTarget.All;
    public int CreatedById { get; set; }

    // Navigation
    public User CreatedBy { get; set; } = null!;
}

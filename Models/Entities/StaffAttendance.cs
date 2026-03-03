namespace Attendance.Models.Entities;

public class StaffAttendance
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime Date { get; set; }
    public AttendanceStatus Status { get; set; }
    public int? MarkedById { get; set; }

    // Navigation
    public User User { get; set; } = null!;
    public User? MarkedBy { get; set; }
}

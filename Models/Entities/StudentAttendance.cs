namespace Attendance.Models.Entities;

public enum AttendanceStatus { Present, Absent }

public class StudentAttendance
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public DateTime Date { get; set; }
    public AttendanceStatus Status { get; set; }

    // Navigation
    public Student Student { get; set; } = null!;
}

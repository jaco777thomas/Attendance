namespace Attendance.Models.Entities;

public enum StudentStatus { Active, Inactive }
public enum Division { A, B, C }

public class Student
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Class { get; set; } = string.Empty; // LKG, UKG, 1–15
    public Division? Division { get; set; }
    public Language? Language { get; set; } = Attendance.Models.Entities.Language.English;
    public DateTime? DateOfBirth { get; set; }
    public string? Address { get; set; } = string.Empty;
    public string? FatherName { get; set; } = string.Empty;
    public string? MotherName { get; set; } = string.Empty;
    public string? Phone { get; set; } = string.Empty;
    public StudentStatus Status { get; set; } = StudentStatus.Active;
    public int? AcademicYear { get; set; }
    public DateTime? LastPromotedDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<StudentAttendance> Attendances { get; set; } = new List<StudentAttendance>();
}

using System.ComponentModel.DataAnnotations;
using Attendance.Models.Entities;

namespace Attendance.Models.ViewModels;

public class StudentCreateViewModel
{
    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Class { get; set; } = string.Empty;

    public Division Division { get; set; }

    [Required]
    public Language? Language { get; set; } = Attendance.Models.Entities.Language.English;

    [DataType(DataType.Date)]
    public DateTime? DateOfBirth { get; set; }

    [StringLength(300)]
    public string? Address { get; set; } = string.Empty;

    [StringLength(150)]
    public string? FatherName { get; set; } = string.Empty;

    [StringLength(150)]
    public string? MotherName { get; set; } = string.Empty;

    [Phone]
    public string? Phone { get; set; } = string.Empty;

    public StudentStatus Status { get; set; } = StudentStatus.Active;

    [Range(2000, 2100)]
    public int? AcademicYear { get; set; }
}

public class StudentEditViewModel : StudentCreateViewModel
{
    public int Id { get; set; }
}

public class StudentListViewModel
{
    public List<Student> Students { get; set; } = new();
    public string? FilterClass { get; set; }
    public Division? FilterDivision { get; set; }
    public Language? FilterLanguage { get; set; }
    public StudentStatus? FilterStatus { get; set; }
    public string? SearchName { get; set; }
    public int TotalCount { get; set; }
}

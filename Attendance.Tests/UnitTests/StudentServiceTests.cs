using Attendance.Data;
using Attendance.Models.Entities;
using Attendance.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Attendance.Tests.UnitTests;

public class StudentServiceTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly StudentService _sut;

    public StudentServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options);
        _sut = new StudentService(_db);
    }

    // ---- GetForAttendanceAsync ----

    [Fact]
    public async Task GetForAttendanceAsync_ReturnsOnlyActiveStudents()
    {
        // Arrange
        _db.Students.AddRange(
            new Student { Name = "Active Kid",   Class = "1", Division = Division.A, Language = Language.English, Status = StudentStatus.Active },
            new Student { Name = "Inactive Kid", Class = "1", Division = Division.A, Language = Language.English, Status = StudentStatus.Inactive },
            new Student { Name = "Other Class",  Class = "2", Division = Division.A, Language = Language.English, Status = StudentStatus.Active }
        );
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.GetForAttendanceAsync("1", Division.A, Language.English, DateTime.Today);

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Active Kid");
    }

    [Fact]
    public async Task GetForAttendanceAsync_FiltersCorrectly_ByClassDivisionAndLanguage()
    {
        // Arrange
        _db.Students.AddRange(
            new Student { Name = "A Eng", Class = "3", Division = Division.A, Language = Language.English,  Status = StudentStatus.Active },
            new Student { Name = "B Eng", Class = "3", Division = Division.B, Language = Language.English,  Status = StudentStatus.Active },
            new Student { Name = "A Kan", Class = "3", Division = Division.A, Language = Language.Kannada,  Status = StudentStatus.Active },
            new Student { Name = "A Eng 4", Class = "4", Division = Division.A, Language = Language.English, Status = StudentStatus.Active }
        );
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.GetForAttendanceAsync("3", Division.A, Language.English, DateTime.Today);

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("A Eng");
    }

    // ---- GetAllAsync with filters ----

    [Fact]
    public async Task GetAllAsync_ReturnsAllStudents_WhenNoFilterApplied()
    {
        // Arrange
        _db.Students.AddRange(
            new Student { Name = "Student A", Class = "1", Division = Division.A, Language = Language.English, Status = StudentStatus.Active },
            new Student { Name = "Student B", Class = "2", Division = Division.A, Language = Language.English, Status = StudentStatus.Inactive }
        );
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.GetAllAsync(null, null, null, null, null);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_FiltersActiveStudents_WhenStatusIsActive()
    {
        // Arrange
        _db.Students.AddRange(
            new Student { Name = "Active",   Class = "1", Division = Division.A, Language = Language.English, Status = StudentStatus.Active },
            new Student { Name = "Inactive", Class = "1", Division = Division.A, Language = Language.English, Status = StudentStatus.Inactive }
        );
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.GetAllAsync(null, null, null, StudentStatus.Active, null);

        // Assert
        result.Should().HaveCount(1).And.Contain(s => s.Name == "Active");
    }

    [Fact]
    public async Task GetAllAsync_SearchByName_IsCaseInsensitive()
    {
        // Arrange
        _db.Students.AddRange(
            new Student { Name = "John Smith", Class = "1", Division = Division.A, Language = Language.English, Status = StudentStatus.Active },
            new Student { Name = "Jane Doe",   Class = "1", Division = Division.A, Language = Language.English, Status = StudentStatus.Active }
        );
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.GetAllAsync(null, null, null, null, "JOHN");

        // Assert
        result.Should().HaveCount(1).And.Contain(s => s.Name == "John Smith");
    }

    // ---- ToggleStatusAsync ----

    [Fact]
    public async Task ToggleStatusAsync_SwitchesActiveToInactive()
    {
        var student = new Student { Name = "Toggle", Class = "1", Division = Division.A, Language = Language.English, Status = StudentStatus.Active };
        _db.Students.Add(student);
        await _db.SaveChangesAsync();

        await _sut.ToggleStatusAsync(student.Id);

        var updated = await _db.Students.FindAsync(student.Id);
        updated!.Status.Should().Be(StudentStatus.Inactive);
    }

    [Fact]
    public async Task ToggleStatusAsync_SwitchesInactiveToActive()
    {
        var student = new Student { Name = "Toggle2", Class = "1", Division = Division.A, Language = Language.English, Status = StudentStatus.Inactive };
        _db.Students.Add(student);
        await _db.SaveChangesAsync();

        await _sut.ToggleStatusAsync(student.Id);

        var updated = await _db.Students.FindAsync(student.Id);
        updated!.Status.Should().Be(StudentStatus.Active);
    }

    public void Dispose() => _db.Dispose();
}

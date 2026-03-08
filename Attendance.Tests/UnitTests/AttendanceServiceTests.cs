using Attendance.Data;
using Attendance.Models.Entities;
using Attendance.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Attendance.Tests.UnitTests;

public class AttendanceServiceTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly AttendanceService _sut;

    public AttendanceServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options);
        _sut = new AttendanceService(_db);
    }

    [Fact]
    public async Task GetAbsentStudentNamesAsync_ReturnsCorrectNames()
    {
        // Arrange
        var today = DateTime.Today;
        var s1 = new Student { Id = 1, Name = "Absent Student", Class = "1", Division = Division.A, Status = StudentStatus.Active };
        var s2 = new Student { Id = 2, Name = "Present Student", Class = "1", Division = Division.A, Status = StudentStatus.Active };
        _db.Students.AddRange(s1, s2);
        _db.StudentAttendances.AddRange(
            new StudentAttendance { StudentId = 1, Date = today, Status = AttendanceStatus.Absent },
            new StudentAttendance { StudentId = 2, Date = today, Status = AttendanceStatus.Present }
        );
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.GetAbsentStudentNamesAsync(null, today);

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain("Absent Student");
    }

    [Fact]
    public async Task GetAbsentStaffNamesAsync_ReturnsCorrectNames()
    {
        // Arrange
        var today = DateTime.Today;
        var u1 = new User { Id = 1, Name = "Absent Teacher", Email = "a@t.com", PasswordHash = "x", Role = UserRole.Teacher, IsActive = true };
        var u2 = new User { Id = 2, Name = "Present Teacher", Email = "p@t.com", PasswordHash = "x", Role = UserRole.Teacher, IsActive = true };
        _db.Users.AddRange(u1, u2);
        _db.StaffAttendances.AddRange(
            new StaffAttendance { UserId = 1, Date = today, Status = AttendanceStatus.Absent },
            new StaffAttendance { UserId = 2, Date = today, Status = AttendanceStatus.Present }
        );
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.GetAbsentStaffNamesAsync(today);

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain("Absent Teacher");
    }

    public void Dispose() => _db.Dispose();
}

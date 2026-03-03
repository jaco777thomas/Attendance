using Attendance.Data;
using Attendance.Models.Entities;
using Attendance.Models.ViewModels;
using Attendance.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Attendance.Tests.UnitTests;

public class UserServiceTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly UserService _sut;

    public UserServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()) // unique DB per test
            .Options;
        _db = new ApplicationDbContext(options);
        _sut = new UserService(_db);
    }

    // ---- EnsureAdminSeedAsync ----

    [Fact]
    public async Task EnsureAdminSeedAsync_CreatesAdminUser_WhenNotExists()
    {
        // Arrange: seed HeadMaster role
        var role = new Role { Name = "HeadMaster", Description = "Top Level" };
        _db.Roles.Add(role);
        await _db.SaveChangesAsync();

        // Act
        await _sut.EnsureAdminSeedAsync();

        // Assert
        var admin = await _db.Users.Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Email == "admin@church.org");
        admin.Should().NotBeNull();
        admin!.UserRoles.Should().ContainSingle(ur => ur.RoleId == role.Id);
    }

    [Fact]
    public async Task EnsureAdminSeedAsync_DoesNotDuplicateUser_WhenAlreadyExists()
    {
        // Arrange: seed HeadMaster role + existing admin
        var role = new Role { Name = "HeadMaster", Description = "Top Level" };
        _db.Roles.Add(role);
        await _db.SaveChangesAsync();

        var existing = new User
        {
            Name = "Headmaster",
            Email = "admin@church.org",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            Phone = "",
            UserRoles = new List<UserRoleEntry> { new() { RoleId = role.Id } }
        };
        _db.Users.Add(existing);
        await _db.SaveChangesAsync();

        // Act
        await _sut.EnsureAdminSeedAsync();

        // Assert: still exactly one admin
        var count = await _db.Users.CountAsync(u => u.Email == "admin@church.org");
        count.Should().Be(1);
    }

    [Fact]
    public async Task EnsureAdminSeedAsync_AddsRoleLink_WhenAdminExistsWithoutRole()
    {
        // Arrange: role + user with no roles
        var role = new Role { Name = "HeadMaster", Description = "Top Level" };
        _db.Roles.Add(role);
        var user = new User
        {
            Name = "Headmaster",
            Email = "admin@church.org",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            Phone = ""
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // Act
        await _sut.EnsureAdminSeedAsync();

        // Assert: role link was added
        var updated = await _db.Users.Include(u => u.UserRoles)
            .FirstAsync(u => u.Email == "admin@church.org");
        updated.UserRoles.Should().ContainSingle(ur => ur.RoleId == role.Id);
    }

    // ---- CreateAsync ----

    [Fact]
    public async Task CreateAsync_Fails_WhenEmailAlreadyExists()
    {
        // Arrange
        _db.Users.Add(new User
        {
            Name = "Existing", Email = "dup@test.com",
            PasswordHash = "hash", Phone = ""
        });
        await _db.SaveChangesAsync();

        var vm = new UserCreateViewModel
        {
            Name = "New User", Email = "dup@test.com",
            Password = "Test@123", SelectedRoleIds = new List<int>()
        };

        // Act
        var (success, error) = await _sut.CreateAsync(vm);

        // Assert
        success.Should().BeFalse();
        error.Should().Be("Email already in use.");
    }

    [Fact]
    public async Task CreateAsync_Succeeds_WithValidData()
    {
        // Arrange
        var vm = new UserCreateViewModel
        {
            Name = "John Doe",
            Email = "john@test.com",
            Password = "Test@123",
            SelectedRoleIds = new List<int>()
        };

        // Act
        var (success, error) = await _sut.CreateAsync(vm);

        // Assert
        success.Should().BeTrue();
        error.Should().BeEmpty();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == "john@test.com");
        user.Should().NotBeNull();
        BCrypt.Net.BCrypt.Verify("Test@123", user!.PasswordHash).Should().BeTrue();
    }

    // ---- DeleteAsync ----

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenUserNotFound()
    {
        var result = await _sut.DeleteAsync(9999);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_RemovesUser_WhenExists()
    {
        // Arrange
        var user = new User { Name = "Del", Email = "del@test.com", PasswordHash = "h", Phone = "" };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.DeleteAsync(user.Id);

        // Assert
        result.Should().BeTrue();
        var found = await _db.Users.FindAsync(user.Id);
        found.Should().BeNull();
    }

    // ---- AuthenticateAsync ----

    [Fact]
    public async Task AuthenticateAsync_ReturnsNull_WhenPasswordWrong()
    {
        var user = new User
        {
            Name = "Test",  Email = "auth@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("correct"),
            Phone = "", IsActive = true
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var result = await _sut.AuthenticateAsync("auth@test.com", "wrong");
        result.Should().BeNull();
    }

    [Fact]
    public async Task AuthenticateAsync_ReturnsNull_WhenUserInactive()
    {
        var user = new User
        {
            Name = "Inactive", Email = "inactive@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("pass"),
            Phone = "", IsActive = false
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var result = await _sut.AuthenticateAsync("inactive@test.com", "pass");
        result.Should().BeNull();
    }

    [Fact]
    public async Task AuthenticateAsync_ReturnsUser_WhenCredentialsCorrect()
    {
        var user = new User
        {
            Name = "Active", Email = "active@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("correct"),
            Phone = "", IsActive = true
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var result = await _sut.AuthenticateAsync("active@test.com", "correct");
        result.Should().NotBeNull();
        result!.Email.Should().Be("active@test.com");
    }

    public void Dispose() => _db.Dispose();
}

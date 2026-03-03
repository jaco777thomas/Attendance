using Attendance.Data;
using Attendance.Models.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Attendance.Tests.IntegrationTests;

// ---------------------------------------------------------------
// Custom WebApplicationFactory with in-memory DB
// ---------------------------------------------------------------
public class AttendanceWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Tell Program.cs to use InMemory DB instead of Npgsql
        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["UseInMemoryDatabase"] = "true"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Replace real cookie auth with fake test auth
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
        });
    }

    /// <summary>Seeds the in-memory database after the server has been built.</summary>
    public void SeedDatabase()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (db.Roles.Any()) return; // Already seeded
        SeedTestData(db);
    }

    private static void SeedTestData(ApplicationDbContext db)
    {
        // Seed permissions
        var perms = new[]
        {
            new Permission { Name = "View Students",       Key = "ViewStudents" },
            new Permission { Name = "Manage Students",     Key = "ManageStudents" },
            new Permission { Name = "Take Student Attendance", Key = "TakeStudentAttendance" },
            new Permission { Name = "Take Teacher Attendance", Key = "TakeTeacherAttendance" },
            new Permission { Name = "View Reports",        Key = "ViewReports" },
            new Permission { Name = "Export Reports",      Key = "ExportReports" },
            new Permission { Name = "View Notifications",  Key = "ViewNotifications" },
            new Permission { Name = "Manage Notifications",Key = "ManageNotifications" },
            new Permission { Name = "View Dashboard Own",  Key = "ViewDashboardOwnClass" },
            new Permission { Name = "View Dashboard All",  Key = "ViewDashboardAllClasses" },
            new Permission { Name = "User Management",     Key = "UserManagement" },
            new Permission { Name = "Role Management",     Key = "RoleManagement" },
            new Permission { Name = "Database Management", Key = "DatabaseManagement" },
        };
        db.Permissions.AddRange(perms);
        db.SaveChanges();

        var permDict = db.Permissions.ToDictionary(p => p.Key, p => p.Id);

        // Seed roles
        var teacherRole = new Role { Name = "Teacher", Description = "Teacher" };
        var adminRole   = new Role { Name = "Admin",   Description = "Admin" };
        var hmRole      = new Role { Name = "HeadMaster", Description = "HeadMaster" };
        var dbRole      = new Role { Name = "DBAdmin", Description = "DB Admin" };
        db.Roles.AddRange(teacherRole, adminRole, hmRole, dbRole);
        db.SaveChanges();

        // Teacher: ViewStudents, TakeStudentAttendance, ViewReports, ViewNotifications, ViewDashboardOwnClass
        var teacherPerms = new[] { "ViewStudents", "TakeStudentAttendance", "ViewReports", "ViewNotifications", "ViewDashboardOwnClass" };
        db.RolePermissions.AddRange(teacherPerms.Select(k => new RolePermission { RoleId = teacherRole.Id, PermissionId = permDict[k] }));

        // Admin: all except DatabaseManagement
        var adminPerms = new[] { "ViewStudents", "ManageStudents", "TakeStudentAttendance", "TakeTeacherAttendance",
            "ViewReports", "ExportReports", "ViewNotifications", "ManageNotifications",
            "ViewDashboardAllClasses", "UserManagement", "RoleManagement" };
        db.RolePermissions.AddRange(adminPerms.Select(k => new RolePermission { RoleId = adminRole.Id, PermissionId = permDict[k] }));

        // HeadMaster: same as Admin
        db.RolePermissions.AddRange(adminPerms.Select(k => new RolePermission { RoleId = hmRole.Id, PermissionId = permDict[k] }));

        // DBAdmin: DatabaseManagement only
        db.RolePermissions.Add(new RolePermission { RoleId = dbRole.Id, PermissionId = permDict["DatabaseManagement"] });

        // Seed test users
        var teacherUser = new User
        {
            Name = "Test Teacher", Email = "teacher@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test@123"),
            Phone = "", IsActive = true,
            AssignedClass = "1", AssignedDivision = Division.A, AssignedLanguage = Language.English,
            UserRoles = new List<UserRoleEntry> { new() { RoleId = teacherRole.Id } }
        };
        var adminUser = new User
        {
            Name = "Test Admin", Email = "admin@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test@123"),
            Phone = "", IsActive = true,
            UserRoles = new List<UserRoleEntry> { new() { RoleId = adminRole.Id } }
        };
        var hmUser = new User
        {
            Name = "Test HeadMaster", Email = "hm@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test@123"),
            Phone = "", IsActive = true,
            UserRoles = new List<UserRoleEntry> { new() { RoleId = hmRole.Id } }
        };
        db.Users.AddRange(teacherUser, adminUser, hmUser);
        db.SaveChanges();
    }
}

// ---------------------------------------------------------------
// Fake auth handler — allows injecting role claims per-request
// ---------------------------------------------------------------
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public static string CurrentRole { get; set; } = string.Empty;
    public static int    CurrentUserId { get; set; } = 1;
    public static string CurrentUserName { get; set; } = "TestUser";

    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder) : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (string.IsNullOrEmpty(CurrentRole))
            return Task.FromResult(AuthenticateResult.Fail("No role set"));

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, CurrentUserId.ToString()),
            new(ClaimTypes.Name, CurrentUserName),
        };

        foreach (var role in CurrentRole.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            claims.Add(new(ClaimTypes.Role, role));

        var identity  = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket    = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

// ---------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------
public static class TestClientFactory
{
    public static HttpClient CreateClientAs(AttendanceWebApplicationFactory factory, string role, int userId = 1)
    {
        TestAuthHandler.CurrentRole = role;
        TestAuthHandler.CurrentUserId = userId;
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        return client;
    }

    public static HttpClient CreateUnauthenticatedClient(AttendanceWebApplicationFactory factory)
    {
        TestAuthHandler.CurrentRole = string.Empty;
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        return client;
    }
}

// ---------------------------------------------------------------
// Role Access Tests
// ---------------------------------------------------------------
public class RoleAccessTests : IClassFixture<AttendanceWebApplicationFactory>, IAsyncLifetime
{
    private readonly AttendanceWebApplicationFactory _factory;

    public RoleAccessTests(AttendanceWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        // Trigger host startup then seed — Services is only available after first CreateClient
        _ = _factory.CreateClient();
        _factory.SeedDatabase();
        await Task.CompletedTask;
    }

    public Task DisposeAsync() => Task.CompletedTask;


    // ---- Unauthenticated redirects to login ----

    [Theory]
    [InlineData("/Dashboard")]
    [InlineData("/Students")]
    [InlineData("/Users")]
    [InlineData("/Reports")]
    [InlineData("/Attendance/Students")]
    public async Task Unauthenticated_Access_IsRestricted(string url)
    {
        var client = TestClientFactory.CreateUnauthenticatedClient(_factory);
        var response = await client.GetAsync(url);
        
        // In local test environment with fake auth, it may return 401 directly
        // In real app with cookies, it redirects to login (302)
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Found, HttpStatusCode.Unauthorized);
        
        if (response.StatusCode == HttpStatusCode.Redirect || response.StatusCode == HttpStatusCode.Found)
        {
            response.Headers.Location?.ToString().Should().Contain("/Account/Login");
        }
    }

    // ---- Teacher: allowed pages ----

    [Theory]
    [InlineData("/Dashboard")]
    [InlineData("/Students")]
    [InlineData("/Reports")]
    [InlineData("/Attendance/Students")]
    public async Task Teacher_CanAccess_AllowedPages(string url)
    {
        var client = TestClientFactory.CreateClientAs(_factory, "Teacher");
        var response = await client.GetAsync(url);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden, $"Teacher should be able to access {url}");
        response.StatusCode.Should().NotBe(HttpStatusCode.Redirect, $"Teacher should not be redirected from {url}");
    }

    // ---- Teacher: forbidden pages ----

    [Theory]
    [InlineData("/Users")]
    [InlineData("/Roles")]
    [InlineData("/Import")]
    [InlineData("/Reports/ExportExcel?ReportType=Student")]
    [InlineData("/Reports/ExportPdf?ReportType=Student")]
    public async Task Teacher_CannotAccess_ForbiddenPages(string url)
    {
        var client = TestClientFactory.CreateClientAs(_factory, "Teacher");
        var response = await client.GetAsync(url);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Redirect);
    }

    // ---- Admin: allowed pages ----

    [Theory]
    [InlineData("/Dashboard")]
    [InlineData("/Students")]
    [InlineData("/Users")]
    [InlineData("/Roles")]
    [InlineData("/Reports")]
    [InlineData("/Attendance/Students")]
    [InlineData("/Attendance/Staff")]
    [InlineData("/Reports/ExportExcel?ReportType=Student")]
    [InlineData("/Reports/ExportPdf?ReportType=Student")]
    public async Task Admin_CanAccess_AllowedPages(string url)
    {
        var client = TestClientFactory.CreateClientAs(_factory, "Admin");
        var response = await client.GetAsync(url);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden, $"Admin should be able to access {url}");
    }

    // ---- Admin: cannot access DBAdmin-only page ----

    [Fact]
    public async Task Admin_CannotAccess_Import()
    {
        var client = TestClientFactory.CreateClientAs(_factory, "Admin");
        var response = await client.GetAsync("/Import");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Redirect);
    }

    // ---- HeadMaster: allowed pages ----

    [Theory]
    [InlineData("/Dashboard")]
    [InlineData("/Students")]
    [InlineData("/Users")]
    [InlineData("/Roles")]
    [InlineData("/Reports")]
    [InlineData("/Promotion")]
    [InlineData("/Reports/ExportExcel?ReportType=Student")]
    public async Task HeadMaster_CanAccess_AllowedPages(string url)
    {
        var client = TestClientFactory.CreateClientAs(_factory, "HeadMaster");
        var response = await client.GetAsync(url);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden, $"HeadMaster should be able to access {url}");
    }

    // ---- DBAdmin: can access Import, cannot access Users ----

    [Fact]
    public async Task DBAdmin_CanAccess_Import()
    {
        var client = TestClientFactory.CreateClientAs(_factory, "DBAdmin");
        var response = await client.GetAsync("/Import");
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden, "DBAdmin should be able to access /Import");
    }

    [Fact]
    public async Task DBAdmin_CannotAccess_UsersPage()
    {
        var client = TestClientFactory.CreateClientAs(_factory, "DBAdmin");
        var response = await client.GetAsync("/Users");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Redirect);
    }
}

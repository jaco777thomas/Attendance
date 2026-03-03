using Attendance.Data;
using Attendance.Data.Repositories;
using Attendance.Infrastructure.Security;
using Attendance.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Attendance.Models.Entities;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);
// Force culture for consistent date parsing
var defaultCulture = new System.Globalization.CultureInfo("en-IN");
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(defaultCulture);
    options.SupportedCultures = new List<System.Globalization.CultureInfo> { defaultCulture };
    options.SupportedUICultures = new List<System.Globalization.CultureInfo> { defaultCulture };
});

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// Database — supports InMemory mode for integration tests
var useInMemory = builder.Configuration.GetValue<bool>("UseInMemoryDatabase");
if (useInMemory)
{
    builder.Services.AddDbContext<ApplicationDbContext>(opt =>
        opt.UseInMemoryDatabase("IntegrationTestDb"));
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? Environment.GetEnvironmentVariable("DATABASE_URL")
        ?? throw new InvalidOperationException("No database connection string found.");
    builder.Services.AddDbContext<ApplicationDbContext>(opt =>
        opt.UseNpgsql(connectionString));
}

// Cookie Auth
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionHandler>();

// DI Services
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<INoticeService, NoticeService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<IPromotionService, PromotionService>();

builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

var app = builder.Build();

// Migrate and seed DB on startup
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.EnsureCreated();
        
        // Check for missing tables and create them
        var tablesToCreate = new List<(string Name, string Sql)>
        {
            ("Roles", "CREATE TABLE IF NOT EXISTS \"Roles\" (\"Id\" SERIAL PRIMARY KEY, \"Name\" VARCHAR(50) NOT NULL UNIQUE, \"Description\" VARCHAR(200));"),
            ("Permissions", "CREATE TABLE IF NOT EXISTS \"Permissions\" (\"Id\" SERIAL PRIMARY KEY, \"Name\" VARCHAR(100) NOT NULL, \"Key\"  VARCHAR(50) NOT NULL UNIQUE);"),
            ("RolePermissions", "CREATE TABLE IF NOT EXISTS \"RolePermissions\" (\"RoleId\" INTEGER NOT NULL REFERENCES \"Roles\"(\"Id\") ON DELETE CASCADE, \"PermissionId\" INTEGER NOT NULL REFERENCES \"Permissions\"(\"Id\") ON DELETE CASCADE, PRIMARY KEY (\"RoleId\", \"PermissionId\"));"),
            ("UserRoles", "CREATE TABLE IF NOT EXISTS \"UserRoles\" (\"UserId\" INTEGER NOT NULL REFERENCES \"Users\"(\"Id\") ON DELETE CASCADE, \"RoleId\" INTEGER NOT NULL REFERENCES \"Roles\"(\"Id\") ON DELETE CASCADE, PRIMARY KEY (\"UserId\", \"RoleId\"));")
        };

        foreach (var (tableName, sql) in tablesToCreate)
        {
            try {
                await db.Database.ExecuteSqlRawAsync($"SELECT 1 FROM \"{tableName}\" LIMIT 1;");
            } catch {
                logger.LogInformation("Creating missing table: {Table}...", tableName);
                await db.Database.ExecuteSqlRawAsync(sql);
            }
        }

        // Ensure legacy Role is optional
        try {
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"Users\" ALTER COLUMN \"Role\" DROP NOT Null;");
        } catch { }

        // Seed Roles and Permissions
        await SeedPermissionsAndRolesAsync(db);

        // Link existing users to roles if not already linked (Migration)
        var usersToLink = await db.Users.Include(u => u.UserRoles).Where(u => !u.UserRoles.Any()).ToListAsync();
        if (usersToLink.Any())
        {
            var roleMap = await db.Roles.ToDictionaryAsync(r => r.Name, r => r.Id);
            foreach (var user in usersToLink)
            {
                // Try to map from legacy Role enum or name
                string? legacyRole = user.Role?.ToString();
                if (string.IsNullOrEmpty(legacyRole)) continue;

                // Handle mapping
                string targetRole = legacyRole switch {
                    "Headmaster" => "HeadMaster",
                    "DatabaseAdmin" => "DBAdmin",
                    _ => legacyRole
                };

                if (roleMap.TryGetValue(targetRole, out var rid))
                {
                    user.UserRoles.Add(new UserRoleEntry { UserId = user.Id, RoleId = rid });
                }
            }
            await db.SaveChangesAsync();
            logger.LogInformation("Linked {Count} users to the new dynamic role system.", usersToLink.Count);
        }

        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        await userService.EnsureAdminSeedAsync();
        logger.LogInformation("Database initialized and permission system seeded.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database initialization issue occurred.");
    }
}

async Task SeedPermissionsAndRolesAsync(ApplicationDbContext db)
{
    var permissions = new List<(string Name, string Key)>
    {
        ("View Students", PermissionKeys.ViewStudents),
        ("Manage Students", PermissionKeys.ManageStudents),
        ("View Teachers", PermissionKeys.ViewTeachers),
        ("Manage Teachers", PermissionKeys.ManageTeachers),
        ("Take Student Attendance", PermissionKeys.TakeStudentAttendance),
        ("Take Teacher Attendance", PermissionKeys.TakeTeacherAttendance),
        ("View Reports", PermissionKeys.ViewReports),
        ("Export Reports", PermissionKeys.ExportReports),
        ("View Notifications", PermissionKeys.ViewNotifications),
        ("Manage Notifications", PermissionKeys.ManageNotifications),
        ("View Dashboard (Own Class)", PermissionKeys.ViewDashboardOwnClass),
        ("View All Classes Dashboard", PermissionKeys.ViewDashboardAllClasses),
        ("User Management", PermissionKeys.UserManagement),
        ("Role Management", PermissionKeys.RoleManagement),
        ("Database Management", PermissionKeys.DatabaseManagement)
    };

    foreach (var p in permissions)
    {
        if (!await db.Permissions.AnyAsync(perm => perm.Key == p.Key))
        {
            db.Permissions.Add(new Permission { Name = p.Name, Key = p.Key });
        }
    }
    await db.SaveChangesAsync();

    var roleDefinitions = new List<(string Name, string Description, string[] Permissions)>
    {
        ("Teacher", "Standard Teacher Access", new[] { 
            PermissionKeys.ViewStudents, PermissionKeys.TakeStudentAttendance, 
            PermissionKeys.ViewReports, PermissionKeys.ViewNotifications, 
            PermissionKeys.ViewDashboardOwnClass 
        }),
        ("AttendanceIncharge", "Attendance Manager Access", new[] { 
            PermissionKeys.ViewStudents, PermissionKeys.ViewTeachers, 
            PermissionKeys.TakeStudentAttendance, PermissionKeys.TakeTeacherAttendance, 
            PermissionKeys.ViewReports, PermissionKeys.ExportReports, 
            PermissionKeys.ViewDashboardAllClasses 
        }),
        ("Admin", "Full System Admin", new[] { 
            PermissionKeys.ViewStudents, PermissionKeys.ManageStudents, 
            PermissionKeys.ViewTeachers, PermissionKeys.ManageTeachers, 
            PermissionKeys.TakeStudentAttendance, PermissionKeys.TakeTeacherAttendance, 
            PermissionKeys.ViewReports, PermissionKeys.ExportReports, 
            PermissionKeys.ViewNotifications, PermissionKeys.ManageNotifications, 
            PermissionKeys.ViewDashboardAllClasses, PermissionKeys.UserManagement,
            PermissionKeys.RoleManagement
        }),
        ("HeadMaster", "Top Level Access", new[] { 
            PermissionKeys.ViewStudents, PermissionKeys.ManageStudents, 
            PermissionKeys.ViewTeachers, PermissionKeys.ManageTeachers, 
            PermissionKeys.TakeStudentAttendance, PermissionKeys.TakeTeacherAttendance, 
            PermissionKeys.ViewReports, PermissionKeys.ExportReports, 
            PermissionKeys.ViewNotifications, PermissionKeys.ManageNotifications, 
            PermissionKeys.ViewDashboardAllClasses, PermissionKeys.UserManagement,
            PermissionKeys.RoleManagement
        }),
        ("DBAdmin", "Database Administrator", new[] { PermissionKeys.DatabaseManagement, PermissionKeys.ViewDashboardAllClasses })
    };

    foreach (var rd in roleDefinitions)
    {
        var role = await db.Roles.Include(r => r.RolePermissions).FirstOrDefaultAsync(r => r.Name == rd.Name);
        if (role == null)
        {
            role = new Role { Name = rd.Name, Description = rd.Description };
            db.Roles.Add(role);
            await db.SaveChangesAsync();
        }

        foreach (var pk in rd.Permissions)
        {
            var p = await db.Permissions.FirstAsync(perm => perm.Key == pk);
            if (!role.RolePermissions.Any(rp => rp.PermissionId == p.Id))
            {
                db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = p.Id });
            }
        }
    }
    await db.SaveChangesAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseRequestLocalization();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// Required for WebApplicationFactory in integration tests
public partial class Program { }

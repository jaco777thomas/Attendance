using Attendance.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Attendance.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<UserRoleEntry> UserRoles { get; set; }
    public DbSet<Student> Students { get; set; }
    public DbSet<StudentAttendance> StudentAttendances { get; set; }
    public DbSet<StaffAttendance> StaffAttendances { get; set; }
    public DbSet<Notice> Notices { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Role).HasConversion<string>(); // Legacy role enum support
            e.Property(u => u.AssignedDivision).HasConversion<string>();
            e.Property(u => u.AssignedLanguage).HasConversion<string>();
        });

        // UserRoleEntry Many-to-Many
        modelBuilder.Entity<UserRoleEntry>(e =>
        {
            e.HasKey(ur => new { ur.UserId, ur.RoleId });
            e.HasOne(ur => ur.User)
             .WithMany(u => u.UserRoles)
             .HasForeignKey(ur => ur.UserId);
            e.HasOne(ur => ur.Role)
             .WithMany(r => r.UserRoles)
             .HasForeignKey(ur => ur.RoleId);
        });

        // Role
        modelBuilder.Entity<Role>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.Name).IsRequired().HasMaxLength(50);
        });

        // Permission
        modelBuilder.Entity<Permission>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Name).IsRequired().HasMaxLength(100);
            e.Property(p => p.Key).IsRequired().HasMaxLength(50);
            e.HasIndex(p => p.Key).IsUnique();
        });

        // RolePermission
        modelBuilder.Entity<RolePermission>(e =>
        {
            e.HasKey(rp => new { rp.RoleId, rp.PermissionId });
            e.HasOne(rp => rp.Role)
             .WithMany(r => r.RolePermissions)
             .HasForeignKey(rp => rp.RoleId);
            e.HasOne(rp => rp.Permission)
             .WithMany(p => p.RolePermissions)
             .HasForeignKey(rp => rp.PermissionId);
        });

        // Student
        modelBuilder.Entity<Student>(e =>
        {
            e.Property(s => s.Status).HasConversion<string>();
            e.Property(s => s.Division).HasConversion<string>();
            e.Property(s => s.Language).HasConversion<string>();
            e.Property(s => s.CreatedAt).HasColumnType("timestamp"); // Use timestamp without TZ for simple audit
            e.HasIndex(s => new { s.Class, s.Division, s.Language, s.Status });
        });

        // StudentAttendance
        modelBuilder.Entity<StudentAttendance>(e =>
        {
            e.Property(a => a.Status).HasConversion<string>();
            e.Property(a => a.Date).HasColumnType("date"); // Explicitly date type
            e.HasIndex(a => new { a.StudentId, a.Date }).IsUnique();
            e.HasOne(a => a.Student)
             .WithMany(s => s.Attendances)
             .HasForeignKey(a => a.StudentId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // StaffAttendance
        modelBuilder.Entity<StaffAttendance>(e =>
        {
            e.Property(a => a.Status).HasConversion<string>();
            e.Property(a => a.Date).HasColumnType("date"); // Explicitly date type
            e.HasIndex(a => new { a.UserId, a.Date }).IsUnique();
            e.HasOne(a => a.User)
             .WithMany(u => u.StaffAttendances)
             .HasForeignKey(a => a.UserId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.MarkedBy)
             .WithMany()
             .HasForeignKey(a => a.MarkedById)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // Notice
        modelBuilder.Entity<Notice>(e =>
        {
            e.Property(n => n.Target).HasConversion<string>();
            e.HasOne(n => n.CreatedBy)
             .WithMany(u => u.CreatedNotices)
             .HasForeignKey(n => n.CreatedById)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

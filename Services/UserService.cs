using Attendance.Data;
using Attendance.Models.Entities;
using Attendance.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Attendance.Services;

public interface IUserService
{
    Task<User?> AuthenticateAsync(string email, string password);
    Task<List<User>> GetAllAsync();
    Task<List<User>> GetTeachersAsync();
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByEmailAsync(string email);
    Task<(bool Success, string Error)> CreateAsync(UserCreateViewModel vm);
    Task<bool> UpdateAsync(UserEditViewModel vm);
    Task<bool> DeleteAsync(int id);
    Task EnsureAdminSeedAsync();
}

public class UserService : IUserService
{
    private readonly ApplicationDbContext _db;
    public UserService(ApplicationDbContext db) => _db = db;

    public async Task<User?> AuthenticateAsync(string email, string password)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower() && u.IsActive);
            
        if (user == null) return null;
        return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash) ? user : null;
    }

    public Task<List<User>> GetAllAsync() => _db.Users
        .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
        .OrderBy(u => u.Name).ToListAsync();

    public Task<List<User>> GetTeachersAsync() => _db.Users
        .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
        .Where(u => u.UserRoles.Any(ur => ur.Role.Name == "Teacher") && u.IsActive)
        .OrderBy(u => u.Name).ToListAsync();

    public Task<User?> GetByIdAsync(int id) => _db.Users
        .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
        .FirstOrDefaultAsync(u => u.Id == id);

    public Task<User?> GetByEmailAsync(string email) => _db.Users
        .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
        .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

    public async Task<(bool Success, string Error)> CreateAsync(UserCreateViewModel vm)
    {
        if (await _db.Users.AnyAsync(u => u.Email.ToLower() == vm.Email.ToLower()))
            return (false, "Email already in use.");

        var user = new User
        {
            Name = vm.Name, Email = vm.Email, Phone = vm.Phone ?? "",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(vm.Password),
            AssignedClass = vm.AssignedClass,
            AssignedDivision = vm.AssignedDivision,
            AssignedLanguage = vm.AssignedLanguage,
            UserRoles = vm.SelectedRoleIds.Select(rid => new UserRoleEntry { RoleId = rid }).ToList()
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<bool> UpdateAsync(UserEditViewModel vm)
    {
        var user = await _db.Users.Include(u => u.UserRoles).FirstOrDefaultAsync(u => u.Id == vm.Id);
        if (user == null) return false;

        user.Name = vm.Name; user.Email = vm.Email; user.Phone = vm.Phone;
        user.IsActive = vm.IsActive;
        user.AssignedClass = vm.AssignedClass;
        user.AssignedDivision = vm.AssignedDivision;
        user.AssignedLanguage = vm.AssignedLanguage;

        // Update Roles
        user.UserRoles.Clear();
        foreach (var rid in vm.SelectedRoleIds)
        {
            user.UserRoles.Add(new UserRoleEntry { UserId = user.Id, RoleId = rid });
        }

        if (!string.IsNullOrWhiteSpace(vm.NewPassword))
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(vm.NewPassword);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return false;
        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task EnsureAdminSeedAsync()
    {
        var headmasterRole = await _db.Roles.FirstOrDefaultAsync(r => r.Name == "HeadMaster");
        if (headmasterRole == null) return;

        var adminUser = await _db.Users.Include(u => u.UserRoles).FirstOrDefaultAsync(u => u.Email.ToLower() == "admin@church.org");
        if (adminUser == null)
        {
            _db.Users.Add(new User
            {
                Name = "Headmaster",
                Email = "admin@church.org",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                Phone = "",
                UserRoles = new List<UserRoleEntry> { new UserRoleEntry { RoleId = headmasterRole.Id } }
            });
            await _db.SaveChangesAsync();
        }
        else if (!adminUser.UserRoles.Any(ur => ur.RoleId == headmasterRole.Id))
        {
            adminUser.UserRoles.Add(new UserRoleEntry { UserId = adminUser.Id, RoleId = headmasterRole.Id });
            await _db.SaveChangesAsync();
        }
    }
}

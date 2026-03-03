using Attendance.Data.Repositories;
using Attendance.Models.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Attendance.Services;

public interface IRoleService
{
    Task<IEnumerable<Role>> GetAllRolesAsync();
    Task<Role?> GetByIdAsync(int id);
    Task CreateRoleAsync(string name, string description, List<int> permissionIds);
    Task UpdateRoleAsync(int id, string name, string description, List<int> permissionIds);
    Task DeleteRoleAsync(int id);
    Task<IEnumerable<Permission>> GetAllPermissionsAsync();
}

public class RoleService : IRoleService
{
    private readonly IUnitOfWork _uow;

    public RoleService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IEnumerable<Role>> GetAllRolesAsync() => await _uow.Roles.GetAllAsync();

    public async Task<Role?> GetByIdAsync(int id)
    {
        var role = await _uow.Roles.GetByIdAsync(id);
        if (role != null)
        {
            // Load permissions manually since GenericRepository doesn't do includes
            var rolePerms = await _uow.RolePermissions.FindAsync(rp => rp.RoleId == id);
            // This is still a bit problematic with the GenericRepository.
        }
        return role;
    }

    public async Task CreateRoleAsync(string name, string description, List<int> permissionIds)
    {
        var role = new Role { Name = name, Description = description };
        await _uow.Roles.AddAsync(role);
        await _uow.CompleteAsync(); // To get ID

        foreach (var pid in permissionIds)
        {
            await _uow.RolePermissions.AddAsync(new RolePermission { RoleId = role.Id, PermissionId = pid });
        }
        await _uow.CompleteAsync();
    }

    public async Task UpdateRoleAsync(int id, string name, string description, List<int> permissionIds)
    {
        var role = await _uow.Roles.GetByIdAsync(id);
        if (role == null) return;

        role.Name = name;
        role.Description = description;
        _uow.Roles.Update(role);

        // Update permissions
        var existing = await _uow.RolePermissions.FindAsync(rp => rp.RoleId == id);
        foreach (var ep in existing) _uow.RolePermissions.Delete(ep);

        foreach (var pid in permissionIds)
        {
            await _uow.RolePermissions.AddAsync(new RolePermission { RoleId = id, PermissionId = pid });
        }
        await _uow.CompleteAsync();
    }

    public async Task DeleteRoleAsync(int id)
    {
        var role = await _uow.Roles.GetByIdAsync(id);
        if (role != null)
        {
            _uow.Roles.Delete(role);
            await _uow.CompleteAsync();
        }
    }

    public async Task<IEnumerable<Permission>> GetAllPermissionsAsync() => await _uow.Permissions.GetAllAsync();
}

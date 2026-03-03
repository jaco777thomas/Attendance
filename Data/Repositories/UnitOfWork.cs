using Attendance.Models.Entities;
using System;
using System.Threading.Tasks;

namespace Attendance.Data.Repositories;

public interface IUnitOfWork : IDisposable
{
    IRepository<User> Users { get; }
    IRepository<Role> Roles { get; }
    IRepository<Permission> Permissions { get; }
    IRepository<RolePermission> RolePermissions { get; }
    // Add other repositories as needed
    
    Task<int> CompleteAsync();
}

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
        Users = new GenericRepository<User>(_context);
        Roles = new GenericRepository<Role>(_context);
        Permissions = new GenericRepository<Permission>(_context);
        RolePermissions = new GenericRepository<RolePermission>(_context);
    }

    public IRepository<User> Users { get; private set; }
    public IRepository<Role> Roles { get; private set; }
    public IRepository<Permission> Permissions { get; private set; }
    public IRepository<RolePermission> RolePermissions { get; private set; }

    public async Task<int> CompleteAsync() => await _context.SaveChangesAsync();

    public void Dispose() => _context.Dispose();
}

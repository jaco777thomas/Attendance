using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Threading.Tasks;
using Attendance.Models.Entities;
using System.Linq;
using Attendance.Data;
using Attendance.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Attendance.Infrastructure.Security;

public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }
    public PermissionRequirement(string permission) => Permission = permission;
}

public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IServiceScopeFactory _scopeFactory;

    public PermissionHandler(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var roleNames = context.User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<PermissionHandler>>();

        if (!roleNames.Any())
        {
            logger.LogWarning("Permission check failed: User {Name} has no role claims.", context.User.Identity?.Name);
            return;
        }

        // Check if any role the user has has the required permission
        var hasPermission = await db.RolePermissions
            .AnyAsync(rp => roleNames.Contains(rp.Role.Name) && rp.Permission.Key == requirement.Permission);

        if (hasPermission)
        {
            logger.LogInformation("Permission '{Permission}' granted to user {Name} with roles {Roles}.", requirement.Permission, context.User.Identity?.Name, string.Join(", ", roleNames));
            context.Succeed(requirement);
        }
        else
        {
            logger.LogWarning("Permission '{Permission}' DENIED to user {Name} with roles {Roles}.", requirement.Permission, context.User.Identity?.Name, string.Join(", ", roleNames));
        }
    }
}

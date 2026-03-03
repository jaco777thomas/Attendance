using Microsoft.AspNetCore.Authorization;

namespace Attendance.Infrastructure.Security;

public class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permission) : base(permission)
    {
    }
}

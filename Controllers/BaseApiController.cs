using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SecureSistem.Controllers
{
    /// <summary>
    /// Base controller providing shared helpers to read claims from the authenticated user's JWT.
    /// </summary>
    [Authorize]
    public abstract class BaseApiController : ControllerBase
    {
        protected int GetCompanyId() => int.Parse(User.FindFirstValue("companyId")!);
        protected int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        protected int GetRoleId() => int.Parse(User.FindFirstValue("roleId")!);
        protected string GetCurrentUsername() => User.FindFirstValue(ClaimTypes.Name)!;

        /// <summary>
        /// True if the authenticated user is a system administrator, allowed to see and manage
        /// data across all companies instead of just their own.
        /// </summary>
        protected bool IsSystemAdmin() => bool.Parse(User.FindFirstValue("isSystemAdmin") ?? "false");
    }
}

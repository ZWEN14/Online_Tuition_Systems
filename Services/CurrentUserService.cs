using System.Security.Claims;

namespace Online_Tuition_Systems.Services;

public interface ICurrentUserService
{
    int UserId { get; }
    bool IsInRole(string role);
}

public class CurrentUserService(IHttpContextAccessor accessor) : ICurrentUserService
{
    public int UserId => int.TryParse(accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
    public bool IsInRole(string role) => accessor.HttpContext?.User.IsInRole(role) == true;
}

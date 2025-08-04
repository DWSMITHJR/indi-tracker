using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Tracker.Infrastructure.Data;
using Tracker.Infrastructure.Models;

namespace Tracker.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public abstract class BaseController<T> : ControllerBase where T : BaseEntity
    {
        protected readonly ApplicationDbContext _context;
        protected readonly ILogger<BaseController<T>> _logger;

        protected BaseController(ApplicationDbContext context, ILogger<BaseController<T>> logger)
        {
            _context = context;
            _logger = logger;
        }

        protected string CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        protected string CurrentUserEmail => User.FindFirst(ClaimTypes.Email)?.Value;
        protected string CurrentUserRole => User.FindFirst(ClaimTypes.Role)?.Value;

        protected async Task<bool> IsAuthorized(Guid organizationId)
        {
            if (string.IsNullOrEmpty(CurrentUserId) || !Guid.TryParse(CurrentUserId, out var userId))
            {
                return false;
            }

            // Admin users have access to all organizations
            if (CurrentUserRole == "Admin")
            {
                return true;
            }

            // Check if the user is a member of the organization
            var user = await _context.Users
                .Include(u => u.Organizations)
                .FirstOrDefaultAsync(u => u.Id == userId);
                
            if (user == null)
            {
                return false;
            }
                
            return user.Organizations.Any(o => o.Id == organizationId);
        }

        protected IActionResult HandleError(string message, Exception ex = null, int statusCode = 500)
        {
            _logger.LogError(ex, message);
            return StatusCode(statusCode, new { Message = message, Error = ex?.Message });
        }
    }
}

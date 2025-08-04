using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tracker.Infrastructure.Data;
using Tracker.Infrastructure.Models;
using Microsoft.AspNetCore.Identity;

namespace Tracker.API.Controllers
{
    public class EnrollmentCodesController : BaseController<EnrollmentCode>
    {
        private readonly UserManager<User> _userManager;

        public EnrollmentCodesController(
            ApplicationDbContext context, 
            ILogger<EnrollmentCodesController> logger,
            UserManager<User> userManager)
                    : base(context, logger)
        {
            _userManager = userManager;
        }

        // GET: api/organizations/{organizationId}/enrollment-codes
        [HttpGet("organizations/{organizationId}")]
        public async Task<IActionResult> GetEnrollmentCodesByOrganization(Guid organizationId, [FromQuery] bool? isActive = null)
        {
            try
            {
                // Check if the current user has access to this organization
                if (!await IsAuthorized(organizationId))
                {
                    return Forbid();
                }

                var query = _context.EnrollmentCodes
                    .Where(ec => ec.OrganizationId == organizationId);

                if (isActive.HasValue)
                {
                    var now = DateTime.UtcNow;
                    if (isActive.Value)
                    {
                        query = query.Where(ec => ec.IsActive && 
                                               ec.BeginDate <= now && 
                                               ec.EndDate >= now && 
                                               (!ec.Used || ec.UsedAt == null));
                    }
                    else
                    {
                        query = query.Where(ec => !ec.IsActive || 
                                               ec.BeginDate > now || 
                                               ec.EndDate < now || 
                                               (ec.Used && ec.UsedAt != null));
                    }
                }

                var enrollmentCodes = await query.ToListAsync();
                return Ok(enrollmentCodes);
            }
            catch (Exception ex)
            {
                return HandleError($"An error occurred while retrieving enrollment codes for organization with ID {organizationId}.", ex);
            }
        }

        // GET: api/enrollment-codes/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetEnrollmentCode(Guid id)
        {
            try
            {
                var enrollmentCode = await _context.EnrollmentCodes
                    .Include(ec => ec.Organization)
                    .Include(ec => ec.UsedBy)
                    .FirstOrDefaultAsync(ec => ec.Id == id);

                if (enrollmentCode == null)
                {
                    return NotFound();
                }

                // Check if the current user has access to this enrollment code's organization
                if (!await IsAuthorized(enrollmentCode.OrganizationId))
                {
                    return Forbid();
                }

                return Ok(enrollmentCode);
            }
            catch (Exception ex)
            {
                return HandleError($"An error occurred while retrieving enrollment code with ID {id}.", ex);
            }
        }

        // POST: api/enrollment-codes
        [HttpPost]
        [Authorize(Roles = "Admin,OrganizationAdmin")]
        public async Task<IActionResult> CreateEnrollmentCode([FromBody] EnrollmentCode enrollmentCode)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Check if the current user has access to this organization
                if (!await IsAuthorized(enrollmentCode.OrganizationId) && CurrentUserRole != "Admin")
                {
                    return Forbid();
                }

                // Ensure the code is unique
                if (await _context.EnrollmentCodes.AnyAsync(ec => ec.Code == enrollmentCode.Code))
                {
                    return BadRequest("An enrollment code with this value already exists.");
                }

                // Set default values
                enrollmentCode.CreatedAt = DateTime.UtcNow;
                enrollmentCode.IsActive = true;
                enrollmentCode.Used = false;
                enrollmentCode.UsedAt = null;
                enrollmentCode.UsedById = null;

                _context.EnrollmentCodes.Add(enrollmentCode);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetEnrollmentCode), new { id = enrollmentCode.Id }, enrollmentCode);
            }
            catch (Exception ex)
            {
                return HandleError("An error occurred while creating the enrollment code.", ex);
            }
        }

        // PUT: api/enrollment-codes/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,OrganizationAdmin")]
        public async Task<IActionResult> UpdateEnrollmentCode(Guid id, [FromBody] EnrollmentCode enrollmentCode)
        {
            try
            {
                if (id != enrollmentCode.Id)
                {
                    return BadRequest("ID in the URL does not match the ID in the request body.");
                }

                var existingEnrollmentCode = await _context.EnrollmentCodes.FindAsync(id);
                if (existingEnrollmentCode == null)
                {
                    return NotFound();
                }

                // Check if the current user has access to this enrollment code's organization
                if (!await IsAuthorized(existingEnrollmentCode.OrganizationId) && CurrentUserRole != "Admin")
                {
                    return Forbid();
                }

                // Prevent modifying used codes
                if (existingEnrollmentCode.Used && existingEnrollmentCode.UsedAt.HasValue)
                {
                    return BadRequest("Cannot modify a used enrollment code.");
                }

                // Update only the allowed properties
                existingEnrollmentCode.Code = enrollmentCode.Code;
                existingEnrollmentCode.BeginDate = enrollmentCode.BeginDate;
                existingEnrollmentCode.EndDate = enrollmentCode.EndDate;
                existingEnrollmentCode.IsActive = enrollmentCode.IsActive;
                existingEnrollmentCode.UpdatedAt = DateTime.UtcNow;

                _context.Entry(existingEnrollmentCode).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!await EnrollmentCodeExists(id))
                {
                    return NotFound();
                }
                else
                {
                    return HandleError("A concurrency error occurred while updating the enrollment code.", ex);
                }
            }
            catch (Exception ex)
            {
                return HandleError($"An error occurred while updating enrollment code with ID {id}.", ex);
            }
        }

        // DELETE: api/enrollment-codes/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,OrganizationAdmin")]
        public async Task<IActionResult> DeleteEnrollmentCode(Guid id)
        {
            try
            {
                var enrollmentCode = await _context.EnrollmentCodes.FindAsync(id);
                if (enrollmentCode == null)
                {
                    return NotFound();
                }

                // Check if the current user has access to this enrollment code's organization
                if (!await IsAuthorized(enrollmentCode.OrganizationId) && CurrentUserRole != "Admin")
                {
                    return Forbid();
                }

                // Don't allow deleting used codes
                if (enrollmentCode.Used && enrollmentCode.UsedAt.HasValue)
                {
                    return BadRequest("Cannot delete a used enrollment code.");
                }

                _context.EnrollmentCodes.Remove(enrollmentCode);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return HandleError($"An error occurred while deleting enrollment code with ID {id}.", ex);
            }
        }

        // POST: api/enrollment-codes/validate
        [HttpPost("validate")]
        [AllowAnonymous]
        public async Task<IActionResult> ValidateEnrollmentCode([FromBody] ValidateEnrollmentCodeRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Code))
                {
                    return BadRequest("Enrollment code is required.");
                }

                var now = DateTime.UtcNow;
                var enrollmentCode = await _context.EnrollmentCodes
                    .Include(ec => ec.Organization)
                    .FirstOrDefaultAsync(ec => ec.Code == request.Code && 
                                             ec.IsActive && 
                                             ec.BeginDate <= now && 
                                             ec.EndDate >= now && 
                                             (!ec.Used || ec.UsedAt == null));

                if (enrollmentCode == null)
                {
                    return NotFound("Invalid or expired enrollment code.");
                }

                return Ok(new 
                { 
                    IsValid = true, 
                    OrganizationId = enrollmentCode.OrganizationId,
                    OrganizationName = enrollmentCode.Organization.Name,
                    ExpiresAt = enrollmentCode.EndDate
                });
            }
            catch (Exception ex)
            {
                return HandleError("An error occurred while validating the enrollment code.", ex);
            }
        }

        // POST: api/enrollment-codes/use
        [HttpPost("use")]
        [Authorize]
        public async Task<IActionResult> UseEnrollmentCode([FromBody] UseEnrollmentCodeRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Code))
                {
                    return BadRequest("Enrollment code is required.");
                }

                var now = DateTime.UtcNow;
                var enrollmentCode = await _context.EnrollmentCodes
                    .Include(ec => ec.Organization)
                    .FirstOrDefaultAsync(ec => ec.Code == request.Code);

                if (enrollmentCode == null)
                {
                    return NotFound("Enrollment code not found.");
                }

                // Check if the code is already used
                if (enrollmentCode.Used && enrollmentCode.UsedAt.HasValue)
                {
                    return BadRequest("This enrollment code has already been used.");
                }

                // Check if the code is active and within the valid date range
                if (!enrollmentCode.IsActive || enrollmentCode.BeginDate > now || enrollmentCode.EndDate < now)
                {
                    return BadRequest("This enrollment code is not currently active or has expired.");
                }

                // Get the current user
                var user = await _userManager.FindByIdAsync(CurrentUserId);
                if (user == null)
                {
                    return Unauthorized();
                }

                // Mark the code as used
                enrollmentCode.Used = true;
                enrollmentCode.UsedAt = now;
                enrollmentCode.UsedById = user.Id;
                enrollmentCode.UpdatedAt = now;

                // Add the user to the organization
                var organization = await _context.Organizations
                    .Include(o => o.Users)
                    .FirstOrDefaultAsync(o => o.Id == enrollmentCode.OrganizationId);

                if (organization == null)
                {
                    return NotFound("Organization not found.");
                }

                // Check if the user is already a member of the organization
                if (!organization.Users.Any(u => u.Id == user.Id))
                {
                    organization.Users.Add(user);
                }

                // If this is the user's first organization, set it as their primary organization
                if (user.OrganizationId == null)
                {
                    user.OrganizationId = organization.Id;
                }

                await _context.SaveChangesAsync();

                return Ok(new 
                { 
                    Success = true, 
                    Message = "Enrollment code used successfully.",
                    OrganizationId = organization.Id,
                    OrganizationName = organization.Name
                });
            }
            catch (Exception ex)
            {
                return HandleError("An error occurred while using the enrollment code.", ex);
            }
        }

        private async Task<bool> EnrollmentCodeExists(Guid id)
        {
            return await _context.EnrollmentCodes.AnyAsync(e => e.Id == id);
        }
    }

    public class ValidateEnrollmentCodeRequest
    {
        public required string Code { get; set; }
    }

    public class UseEnrollmentCodeRequest
    {
        public required string Code { get; set; }
    }
}

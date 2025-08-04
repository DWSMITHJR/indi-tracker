using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tracker.Infrastructure.Data;
using Tracker.Infrastructure.Models;

namespace Tracker.API.Controllers
{
    public class IndividualsController : BaseController<Individual>
    {
        public IndividualsController(ApplicationDbContext context, ILogger<IndividualsController> logger)
            : base(context, logger)
        {
        }

        // GET: api/organizations/{organizationId}/individuals
        [HttpGet("organizations/{organizationId}")]
        public async Task<IActionResult> GetIndividualsByOrganization(Guid organizationId)
        {
            try
            {
                // Check if the current user has access to this organization
                if (!await IsAuthorized(organizationId))
                {
                    return Forbid();
                }

                var individuals = await _context.Individuals
                    .Where(i => i.OrganizationId == organizationId && i.IsActive)
                    .ToListAsync();

                return Ok(individuals);
            }
            catch (Exception ex)
            {
                return HandleError($"An error occurred while retrieving individuals for organization with ID {organizationId}.", ex);
            }
        }

        // GET: api/individuals/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetIndividual(Guid id)
        {
            try
            {
                var individual = await _context.Individuals
                    .Include(i => i.Organization)
                    .Include(i => i.IncidentInvolvements)
                        .ThenInclude(ii => ii.Incident)
                    .FirstOrDefaultAsync(i => i.Id == id && i.IsActive);

                if (individual == null)
                {
                    return NotFound();
                }

                // Check if the current user has access to this individual's organization
                if (!await IsAuthorized(individual.OrganizationId))
                {
                    return Forbid();
                }

                return Ok(individual);
            }
            catch (Exception ex)
            {
                return HandleError($"An error occurred while retrieving individual with ID {id}.", ex);
            }
        }

        // POST: api/individuals
        [HttpPost]
        public async Task<IActionResult> CreateIndividual([FromBody] Individual individual)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Check if the current user has access to this organization
                if (!await IsAuthorized(individual.OrganizationId))
                {
                    return Forbid();
                }

                individual.CreatedAt = DateTime.UtcNow;
                _context.Individuals.Add(individual);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetIndividual), new { id = individual.Id }, individual);
            }
            catch (Exception ex)
            {
                return HandleError("An error occurred while creating the individual.", ex);
            }
        }

        // PUT: api/individuals/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateIndividual(Guid id, [FromBody] Individual individual)
        {
            try
            {
                if (id != individual.Id)
                {
                    return BadRequest("ID in the URL does not match the ID in the request body.");
                }

                var existingIndividual = await _context.Individuals.FindAsync(id);
                if (existingIndividual == null)
                {
                    return NotFound();
                }

                // Check if the current user has access to this individual's organization
                if (!await IsAuthorized(existingIndividual.OrganizationId))
                {
                    return Forbid();
                }

                // Update only the allowed properties
                existingIndividual.FirstName = individual.FirstName;
                existingIndividual.LastName = individual.LastName;
                existingIndividual.DateOfBirth = individual.DateOfBirth;
                existingIndividual.Gender = individual.Gender;
                existingIndividual.Email = individual.Email;
                existingIndividual.Phone = individual.Phone;
                existingIndividual.Street = individual.Street;
                existingIndividual.City = individual.City;
                existingIndividual.State = individual.State;
                existingIndividual.ZipCode = individual.ZipCode;
                existingIndividual.Country = individual.Country;
                existingIndividual.Type = individual.Type;
                existingIndividual.Status = individual.Status;
                existingIndividual.UpdatedAt = DateTime.UtcNow;

                _context.Entry(existingIndividual).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!await IndividualExists(id))
                {
                    return NotFound();
                }
                else
                {
                    return HandleError("A concurrency error occurred while updating the individual.", ex);
                }
            }
            catch (Exception ex)
            {
                return HandleError($"An error occurred while updating individual with ID {id}.", ex);
            }
        }

        // DELETE: api/individuals/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteIndividual(Guid id)
        {
            try
            {
                var individual = await _context.Individuals.FindAsync(id);
                if (individual == null)
                {
                    return NotFound();
                }

                // Check if the current user has access to this individual's organization
                if (!await IsAuthorized(individual.OrganizationId))
                {
                    return Forbid();
                }

                // Soft delete
                individual.IsActive = false;
                individual.UpdatedAt = DateTime.UtcNow;

                _context.Entry(individual).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return HandleError($"An error occurred while deleting individual with ID {id}.", ex);
            }
        }

        private async Task<bool> IndividualExists(Guid id)
        {
            return await _context.Individuals.AnyAsync(e => e.Id == id && e.IsActive);
        }
    }
}

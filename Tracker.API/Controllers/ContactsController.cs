using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tracker.Infrastructure.Data;
using Tracker.Infrastructure.Models;

namespace Tracker.API.Controllers
{
    public class ContactsController : BaseController<Contact>
    {
        public ContactsController(ApplicationDbContext context, ILogger<ContactsController> logger)
            : base(context, logger)
        {
        }

        // GET: api/organizations/{organizationId}/contacts
        [HttpGet("organizations/{organizationId}")]
        public async Task<IActionResult> GetContactsByOrganization(Guid organizationId, [FromQuery] bool? isPrimary = null)
        {
            try
            {
                // Check if the current user has access to this organization
                if (!await IsAuthorized(organizationId))
                {
                    return Forbid();
                }

                var query = _context.Contacts
                    .Where(c => c.OrganizationId == organizationId && c.IsActive);

                if (isPrimary.HasValue)
                {
                    query = query.Where(c => c.IsPrimary == isPrimary.Value);
                }

                var contacts = await query.ToListAsync();
                return Ok(contacts);
            }
            catch (Exception ex)
            {
                return HandleError($"An error occurred while retrieving contacts for organization with ID {organizationId}.", ex);
            }
        }

        // GET: api/contacts/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetContact(Guid id)
        {
            try
            {
                var contact = await _context.Contacts
                    .Include(c => c.Organization)
                    .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

                if (contact == null)
                {
                    return NotFound();
                }

                // Check if the current user has access to this contact's organization
                if (!await IsAuthorized(contact.OrganizationId))
                {
                    return Forbid();
                }

                return Ok(contact);
            }
            catch (Exception ex)
            {
                return HandleError($"An error occurred while retrieving contact with ID {id}.", ex);
            }
        }

        // POST: api/contacts
        [HttpPost]
        public async Task<IActionResult> CreateContact([FromBody] Contact contact)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Check if the current user has access to this organization
                if (!await IsAuthorized(contact.OrganizationId))
                {
                    return Forbid();
                }

                // If this is set as primary, unset any existing primary contact
                if (contact.IsPrimary)
                {
                    var existingPrimary = await _context.Contacts
                        .FirstOrDefaultAsync(c => c.OrganizationId == contact.OrganizationId && c.IsPrimary && c.IsActive);

                    if (existingPrimary != null)
                    {
                        existingPrimary.IsPrimary = false;
                        _context.Entry(existingPrimary).State = EntityState.Modified;
                    }
                }

                contact.CreatedAt = DateTime.UtcNow;
                _context.Contacts.Add(contact);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetContact), new { id = contact.Id }, contact);
            }
            catch (Exception ex)
            {
                return HandleError("An error occurred while creating the contact.", ex);
            }
        }

        // PUT: api/contacts/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateContact(Guid id, [FromBody] Contact contact)
        {
            try
            {
                if (id != contact.Id)
                {
                    return BadRequest("ID in the URL does not match the ID in the request body.");
                }

                var existingContact = await _context.Contacts.FindAsync(id);
                if (existingContact == null)
                {
                    return NotFound();
                }

                // Check if the current user has access to this contact's organization
                if (!await IsAuthorized(existingContact.OrganizationId))
                {
                    return Forbid();
                }

                // If this is set as primary, unset any existing primary contact
                if (contact.IsPrimary && !existingContact.IsPrimary)
                {
                    var currentPrimary = await _context.Contacts
                        .FirstOrDefaultAsync(c => c.OrganizationId == existingContact.OrganizationId && c.IsPrimary && c.IsActive && c.Id != id);

                    if (currentPrimary != null)
                    {
                        currentPrimary.IsPrimary = false;
                        _context.Entry(currentPrimary).State = EntityState.Modified;
                    }
                }

                // Update the contact
                existingContact.FirstName = contact.FirstName;
                existingContact.LastName = contact.LastName;
                existingContact.Email = contact.Email;
                existingContact.Phone = contact.Phone;
                existingContact.Department = contact.Department;
                existingContact.Position = contact.Position;
                existingContact.IsPrimary = contact.IsPrimary;
                existingContact.PreferredContactMethod = contact.PreferredContactMethod;
                existingContact.UpdatedAt = DateTime.UtcNow;

                _context.Entry(existingContact).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!await ContactExists(id))
                {
                    return NotFound();
                }
                else
                {
                    return HandleError("A concurrency error occurred while updating the contact.", ex);
                }
            }
            catch (Exception ex)
            {
                return HandleError($"An error occurred while updating contact with ID {id}.", ex);
            }
        }

        // DELETE: api/contacts/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteContact(Guid id)
        {
            try
            {
                var contact = await _context.Contacts.FindAsync(id);
                if (contact == null)
                {
                    return NotFound();
                }

                // Check if the current user has access to this contact's organization
                if (!await IsAuthorized(contact.OrganizationId))
                {
                    return Forbid();
                }

                // Don't allow deleting the primary contact
                if (contact.IsPrimary)
                {
                    return BadRequest("Cannot delete the primary contact. Please set another contact as primary first.");
                }

                // Soft delete
                contact.IsActive = false;
                contact.UpdatedAt = DateTime.UtcNow;

                _context.Entry(contact).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return HandleError($"An error occurred while deleting contact with ID {id}.", ex);
            }
        }

        // POST: api/contacts/5/set-primary
        [HttpPost("{id}/set-primary")]
        public async Task<IActionResult> SetAsPrimaryContact(Guid id)
        {
            try
            {
                var contact = await _context.Contacts.FindAsync(id);
                if (contact == null)
                {
                    return NotFound("Contact not found.");
                }

                // Check if the current user has access to this contact's organization
                if (!await IsAuthorized(contact.OrganizationId))
                {
                    return Forbid();
                }

                // If already primary, return success
                if (contact.IsPrimary)
                {
                    return NoContent();
                }

                // Find and unset the current primary contact
                var currentPrimary = await _context.Contacts
                    .FirstOrDefaultAsync(c => c.OrganizationId == contact.OrganizationId && c.IsPrimary && c.IsActive && c.Id != id);

                if (currentPrimary != null)
                {
                    currentPrimary.IsPrimary = false;
                    _context.Entry(currentPrimary).State = EntityState.Modified;
                }

                // Set the new primary contact
                contact.IsPrimary = true;
                contact.UpdatedAt = DateTime.UtcNow;

                _context.Entry(contact).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return HandleError($"An error occurred while setting contact with ID {id} as primary.", ex);
            }
        }

        private async Task<bool> ContactExists(Guid id)
        {
            return await _context.Contacts.AnyAsync(e => e.Id == id && e.IsActive);
        }
    }
}

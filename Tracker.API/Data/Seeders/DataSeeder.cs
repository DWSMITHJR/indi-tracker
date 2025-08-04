using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using Tracker.Infrastructure.Data;
using Tracker.Infrastructure.Models;
using Tracker.API.Data.Seeders.Dtos;

namespace Tracker.API.Data.Seeders;

public interface IDataSeeder
{
    Task SeedAsync();
}

public class DataSeeder : IDataSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DataSeeder> _logger;
    private readonly ICsvDataReader _csvReader;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IConfiguration _configuration;
    private readonly string _dataPath;

    public DataSeeder(
        ApplicationDbContext context,
        ILogger<DataSeeder> logger,
        ICsvDataReader csvReader,
        UserManager<User> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration)
    {
        _context = context;
        _csvReader = csvReader;
        _logger = logger;
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
        _dataPath = configuration["Data:SeedDataPath"] ?? ".data";
        
        // Ensure the data directory exists
        if (!Directory.Exists(_dataPath))
        {
            _logger.LogWarning("Data directory not found: {DataPath}", Path.GetFullPath(_dataPath));
            _logger.LogWarning("Please ensure the .data directory exists in the solution root and contains the required CSV files.");
        }
        else
        {
            _logger.LogInformation("Using data directory: {DataPath}", Path.GetFullPath(_dataPath));
        }
    }

    public async Task SeedAsync()
    {
        _logger.LogInformation("Starting database seeding...");

        try
        {
            // Ensure the database is created and migrations are applied
            await _context.Database.MigrateAsync();
            _logger.LogInformation("Database migrations applied");

            // Check if data directory exists
            if (!Directory.Exists(_dataPath))
            {
                _logger.LogWarning("Data directory not found at: {DataPath}", Path.GetFullPath(_dataPath));
                _logger.LogWarning("Skipping data seeding. Please ensure the .data directory exists in the solution root and contains the required CSV files if you want to seed data.");
                return;
            }

            _logger.LogInformation("Using data directory: {DataPath}", Path.GetFullPath(_dataPath));

            // Seed data in the correct order to respect foreign key constraints
            // Wrap each seeding operation in a try-catch to continue even if one fails
            try { await SeedOrganizationsAsync(); } catch (Exception ex) { _logger.LogError(ex, "Error seeding organizations"); }
            try { await SeedUsersAsync(); } catch (Exception ex) { _logger.LogError(ex, "Error seeding users"); }
            try { await SeedIndividualsAsync(); } catch (Exception ex) { _logger.LogError(ex, "Error seeding individuals"); }
            try { await SeedEnrollmentCodesAsync(); } catch (Exception ex) { _logger.LogError(ex, "Error seeding enrollment codes"); }
            try { await SeedContactsAsync(); } catch (Exception ex) { _logger.LogError(ex, "Error seeding contacts"); }
            try { await SeedIncidentsAsync(); } catch (Exception ex) { _logger.LogError(ex, "Error seeding incidents"); }
            try { await SeedIncidentIndividualsAsync(); } catch (Exception ex) { _logger.LogError(ex, "Error seeding incident-individual relationships"); }

            _logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during database seeding");
            // Don't rethrow to allow the application to start even if seeding fails
        }
    }

    private async Task EnsureRolesExistAsync()
    {
        _logger.LogInformation("Ensuring roles exist...");
        
        // Create default roles if they don't exist
        string[] defaultRoles = { "Admin", "Manager", "User", "OrganizationUser" };
        
        foreach (var roleName in defaultRoles)
        {
            var roleExists = await _roleManager.RoleExistsAsync(roleName);
            if (!roleExists)
            {
                var role = new IdentityRole(roleName);
                await _roleManager.CreateAsync(role);
                _logger.LogInformation($"Created role: {roleName}");
            }
        }
        
        // Create organization-specific roles based on organization types
        var orgTypes = await _context.Organizations
            .Select(o => o.Type)
            .Distinct()
            .Where(t => !string.IsNullOrEmpty(t))
            .ToListAsync();
            
        foreach (var orgType in orgTypes)
        {
            var roleName = $"{orgType}Admin";
            var roleExists = await _roleManager.RoleExistsAsync(roleName);
            if (!roleExists)
            {
                var role = new IdentityRole(roleName);
                await _roleManager.CreateAsync(role);
                _logger.LogInformation($"Created organization role: {roleName}");
            }
        }
    }

    private async Task SeedOrganizationsAsync()
    {
        _logger.LogInformation("Seeding Organizations...");

        try
        {
            var orgCsvPath = Path.Combine(_dataPath, "organizations.csv");
            if (!File.Exists(orgCsvPath))
            {
                _logger.LogWarning($"Organizations CSV file not found at: {orgCsvPath}");
                return;
            }
            
            var orgDtos = await _csvReader.ReadCsvAsync<OrganizationDto>(orgCsvPath);
            if (!orgDtos?.Any() ?? true)
            {
                _logger.LogWarning("No organization records found in the CSV file.");
                return;
            }

            int count = 0;
            foreach (var dto in orgDtos!)
            {
                // Skip if organization already exists
                if (await _context.Organizations.AnyAsync(o => o.Name == dto.Name))
                {
                    _logger.LogInformation($"Organization {dto.Name} already exists. Skipping...");
                    continue;
                }

                var org = new Organization
                {
                    Name = dto.Name ?? string.Empty,
                    Type = dto.Type ?? string.Empty,
                    Phone = dto.Phone ?? string.Empty,
                    Email = dto.Email ?? string.Empty,
                    Street = dto.Street ?? string.Empty,
                    City = dto.City ?? string.Empty,
                    State = dto.State ?? string.Empty,
                    ZipCode = dto.ZipCode ?? string.Empty,
                    Country = dto.Country ?? string.Empty,
                    IsActive = true, // Set IsActive to true by default
                    CreatedAt = dto.CreatedAt == default ? DateTime.UtcNow : dto.CreatedAt,
                    UpdatedAt = dto.UpdatedAt
                };

                _context.Organizations.Add(org);
                count++;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Seeded {count} organizations.");
            
            // Ensure roles exist after organizations are created
            await EnsureRolesExistAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding organizations.");
            throw;
        }
    }

    private async Task SeedUsersAsync()
    {
        _logger.LogInformation("Seeding Users...");

        try
        {
            var usersCsvPath = Path.Combine(_dataPath, "users.csv");
            if (!File.Exists(usersCsvPath))
            {
                _logger.LogWarning($"Users CSV file not found at: {usersCsvPath}");
                return;
            }
            
            var userDtos = await _csvReader.ReadCsvAsync<UserDto>(usersCsvPath);
            if (!userDtos?.Any() ?? true)
            {
                _logger.LogWarning("No user records found in the CSV file.");
                return;
            }

            // Ensure we have at least one admin user
            var adminEmail = "admin@tracker.com";
            if (userDtos != null && !userDtos.Any(u => u.Email?.Equals(adminEmail, StringComparison.OrdinalIgnoreCase) == true))
            {
                userDtos = (userDtos ?? Enumerable.Empty<UserDto>()).Append(new UserDto
                {
                    FirstName = "Admin",
                    LastName = "User",
                    Email = adminEmail,
                    UserName = adminEmail,
                    Role = "Admin"
                }).ToList();
            }
            _logger.LogInformation("Added default admin user");

            int count = 0;
            if (userDtos == null) return;
            
            foreach (var dto in userDtos)
            {
                try 
                {
                    // Check if user already exists
                    var existingUser = await _userManager.FindByEmailAsync(dto.Email);
                    if (existingUser != null)
                    {
                        _logger.LogInformation($"User with email {dto.Email} already exists. Skipping...");
                        continue;
                    }

                    // Create new user
                    var user = new User
                    {
                        FirstName = dto.FirstName,
                        LastName = dto.LastName,
                        Email = dto.Email,
                        IsActive = true,
                        Role = dto.Role ?? "User"
                    };

                    // Set organization if specified (commented out as UserDto doesn't have OrganizationId)
                    // if (!string.IsNullOrEmpty(dto.OrganizationId) && Guid.TryParse(dto.OrganizationId, out var orgId))
                    // {
                    //     var organization = await _context.Organizations.FindAsync(orgId);
                    //     if (organization != null)
                    //     {
                    //         user.OrganizationId = organization.Id;
                    //     }
                    // }

                    // Create user with default password
                    var result = await _userManager.CreateAsync(user, _configuration["DefaultUserPassword"] ?? "DefaultPassword123!");
                    if (!result.Succeeded)
                    {
                        _logger.LogError($"Failed to create user {dto.Email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                        continue;
                    }

                    // Determine role
                    string roleToAssign = user.Role;

                    // If no role specified, assign based on organization type
                    if (string.IsNullOrEmpty(roleToAssign) && user.OrganizationId.HasValue)
                    {
                        var org = await _context.Organizations.FindAsync(user.OrganizationId.Value);
                        if (org != null)
                        {
                            var orgRoleName = $"{org.Type}Admin";
                            var roleExists = await _roleManager.RoleExistsAsync(orgRoleName);
                            roleToAssign = roleExists ? orgRoleName : "OrganizationUser";
                            
                            // Update user role if it was determined from organization
                            if (user.Role != roleToAssign)
                            {
                                user.Role = roleToAssign;
                                await _userManager.UpdateAsync(user);
                            }
                        }
                    }

                    // Ensure role is not empty
                    if (string.IsNullOrEmpty(roleToAssign))
                    {
                        roleToAssign = "User";
                        user.Role = roleToAssign;
                        await _userManager.UpdateAsync(user);
                    }

                    // Add to role
                    var roleResult = await _userManager.AddToRoleAsync(user, roleToAssign);
                    if (!roleResult.Succeeded)
                    {
                        _logger.LogError($"Failed to add user {dto.Email} to role {roleToAssign}: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
                    }
                    else
                    {
                        _logger.LogInformation($"Assigned user {dto.Email} to role: {roleToAssign}");
                    }

                    count++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error seeding user {dto.Email}");
                    // Continue with next user even if one fails
                }
            }

            _logger.LogInformation($"Successfully seeded {count} users with roles.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding users.");
            throw;
        }
    }

    private async Task SeedIndividualsAsync()
    {
        _logger.LogInformation("Seeding Individuals...");

        try
        {
            var csvPath = Path.Combine(_dataPath, "individuals.csv");
            if (!File.Exists(csvPath))
            {
                _logger.LogWarning("Individuals CSV file not found at {CsvPath}", csvPath);
                return;
            }

            var individualDtos = await _csvReader.ReadCsvAsync<IndividualDto>(csvPath);
            if (!individualDtos?.Any() ?? true)
            {
                _logger.LogWarning("No individual records found in the CSV file.");
                return;
            }

            int count = 0;
            foreach (var dto in individualDtos!)
            {
                try
                {
                    // Skip if individual already exists
                    if (await _context.Individuals.AnyAsync(i => i.Id == Guid.Parse(dto.Id)))
                    {
                        _logger.LogInformation($"Individual with ID {dto.Id} already exists. Skipping...");
                        continue;
                    }

                    // Validate organization ID
                    if (!Guid.TryParse(dto.OrganizationId, out var orgId))
                    {
                        _logger.LogWarning($"Invalid OrganizationId '{dto.OrganizationId}' for individual {dto.FirstName} {dto.LastName}. Skipping...");
                        continue;
                    }

                    // Check if organization exists
                    var organization = await _context.Organizations.FindAsync(orgId);
                    if (organization == null)
                    {
                        _logger.LogWarning($"Organization with ID {dto.OrganizationId} not found for individual {dto.FirstName} {dto.LastName}. Skipping...");
                        continue;
                    }

                    // Create new individual
                    var individual = new Individual
                    {
                        Id = Guid.Parse(dto.Id),
                        FirstName = dto.FirstName ?? string.Empty, // Required field, ensure not null
                        LastName = dto.LastName ?? string.Empty,   // Required field, ensure not null
                        DateOfBirth = dto.DateOfBirth,
                        Gender = dto.Gender,
                        Email = dto.Email ?? string.Empty, // Ensure not null
                        Phone = dto.Phone ?? string.Empty, // Ensure not null
                        Street = dto.Address ?? string.Empty, // Ensure not null
                        City = dto.City ?? string.Empty, // Ensure not null
                        State = dto.State ?? string.Empty, // Ensure not null
                        ZipCode = dto.ZipCode ?? string.Empty, // Ensure not null
                        Country = "United States", // Default country
                        OrganizationId = orgId,
                        CreatedAt = dto.CreatedAt == default ? DateTime.UtcNow : dto.CreatedAt,
                        UpdatedAt = dto.UpdatedAt
                    };

                    _context.Individuals.Add(individual);
                    count++;

                    // Save changes in batches to improve performance
                    if (count % 100 == 0)
                    {
                        await _context.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error seeding individual {dto.FirstName} {dto.LastName}");
                    // Continue with next individual even if one fails
                }
            }

            // Save any remaining changes
            if (_context.ChangeTracker.HasChanges())
            {
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation($"Successfully seeded {count} individuals.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding individuals.");
            throw;
        }
    }

    private async Task SeedEnrollmentCodesAsync()
    {
        _logger.LogInformation("Seeding Enrollment Codes...");

        try
        {
            var csvPath = Path.Combine(_dataPath, "enrollmentcodes.csv");
            if (!File.Exists(csvPath))
            {
                _logger.LogWarning("Enrollment codes CSV file not found at {CsvPath}", csvPath);
                return;
            }

            var codeDtos = await _csvReader.ReadCsvAsync<EnrollmentCodeDto>(csvPath);
            if (!codeDtos?.Any() ?? true)
            {
                _logger.LogWarning("No enrollment code records found in the CSV file.");
                return;
            }

            int count = 0;
            foreach (var dto in codeDtos!)
            {
                try
                {
                    // Skip if enrollment code already exists
                    if (await _context.EnrollmentCodes.AnyAsync(ec => ec.Id == Guid.Parse(dto.Id)))
                    {
                        _logger.LogInformation($"Enrollment code with ID {dto.Id} already exists. Skipping...");
                        continue;
                    }

                    // Validate organization ID
                    if (!Guid.TryParse(dto.OrganizationId, out var orgId))
                    {
                        _logger.LogWarning($"Invalid OrganizationId '{dto.OrganizationId}' for enrollment code {dto.Code}. Skipping...");
                        continue;
                    }

                    // Check if organization exists
                    var organization = await _context.Organizations.FindAsync(orgId);
                    if (organization == null)
                    {
                        _logger.LogWarning($"Organization with ID {dto.OrganizationId} not found for enrollment code {dto.Code}. Skipping...");
                        continue;
                    }

                    // Create new enrollment code
                    var enrollmentCode = new EnrollmentCode
                    {
                        Id = Guid.Parse(dto.Id),
                        Code = dto.Code,
                        OrganizationId = orgId,
                        BeginDate = dto.BeginDate,
                        EndDate = dto.EndDate,
                        IsActive = dto.IsActive,
                        Used = dto.Used,
                        UsedAt = dto.UsedAt,
                        UsedById = !string.IsNullOrEmpty(dto.UsedById) && Guid.TryParse(dto.UsedById, out var usedById) ? 
                            usedById : (Guid?)null
                    };

                    _context.EnrollmentCodes.Add(enrollmentCode);
                    count++;

                    // Save changes in batches to improve performance
                    if (count % 100 == 0)
                    {
                        await _context.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error seeding enrollment code {dto.Code}");
                    // Continue with next code even if one fails
                }
            }

            // Save any remaining changes
            if (_context.ChangeTracker.HasChanges())
            {
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation($"Successfully seeded {count} enrollment codes.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding enrollment codes.");
            throw;
        }
    }

    private async Task SeedContactsAsync()
    {
        _logger.LogInformation("Seeding Contacts...");

        try
        {
            var csvPath = Path.Combine(_dataPath, "contacts.csv");
            if (!File.Exists(csvPath))
            {
                _logger.LogWarning("Contacts CSV file not found at {CsvPath}", csvPath);
                return;
            }

            var contactDtos = await _csvReader.ReadCsvAsync<ContactDto>(csvPath);
            if (!contactDtos?.Any() ?? true)
            {
                _logger.LogWarning("No contact records found in the CSV file.");
                return;
            }

            int count = 0;
            foreach (var dto in contactDtos!)
            {
                try
                {
                    // Skip if contact already exists
                    if (await _context.Contacts.AnyAsync(c => c.Id == Guid.Parse(dto.Id)))
                    {
                        _logger.LogInformation($"Contact with ID {dto.Id} already exists. Skipping...");
                        continue;
                    }

                    // Validate organization ID
                    if (!Guid.TryParse(dto.OrganizationId, out var orgId))
                    {
                        _logger.LogWarning($"Invalid OrganizationId '{dto.OrganizationId}' for contact {dto.FirstName} {dto.LastName}. Skipping...");
                        continue;
                    }

                    // Check if organization exists
                    var organization = await _context.Organizations.FindAsync(orgId);
                    if (organization == null)
                    {
                        _logger.LogWarning($"Organization with ID {dto.OrganizationId} not found for contact {dto.FirstName} {dto.LastName}. Skipping...");
                        continue;
                    }

                    // Create new contact
                    var contact = new Contact
                    {
                        Id = Guid.Parse(dto.Id),
                        FirstName = dto.FirstName ?? string.Empty, // Ensure not null
                        LastName = dto.LastName ?? string.Empty,   // Ensure not null
                        Email = dto.Email ?? string.Empty,         // Ensure not null
                        Phone = dto.Phone ?? string.Empty,         // Ensure not null
                        Department = dto.Department ?? string.Empty, // Ensure not null
                        Position = dto.Position ?? string.Empty,   // Ensure not null
                        IsPrimary = dto.IsPrimary,
                        PreferredContactMethod = dto.PreferredContactMethod ?? string.Empty, // Ensure not null
                        OrganizationId = orgId
                    };

                    _context.Contacts.Add(contact);
                    count++;

                    // Save changes in batches to improve performance
                    if (count % 100 == 0)
                    {
                        await _context.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error seeding contact {dto.FirstName} {dto.LastName}");
                    // Continue with next contact even if one fails
                }
            }

            // Save any remaining changes
            if (_context.ChangeTracker.HasChanges())
            {
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation($"Successfully seeded {count} contacts.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding contacts.");
            throw;
        }
    }

    private async Task SeedIncidentIndividualsAsync()
    {
        _logger.LogInformation("Seeding Incident-Individual relationships...");

        try
        {
            var csvPath = Path.Combine(_dataPath, "incident_individuals.csv");
            if (!File.Exists(csvPath))
            {
                _logger.LogWarning("Incident-Individuals CSV file not found at {CsvPath}", csvPath);
                return;
            }

            var dtos = await _csvReader.ReadCsvAsync<IncidentIndividualDto>(csvPath);
            if (!dtos?.Any() ?? true)
            {
                _logger.LogWarning("No incident-individual relationship records found in the CSV file.");
                return;
            }

            int count = 0;
            foreach (var dto in dtos!)
            {
                try
                {
                    // Skip if the relationship already exists
                    if (await _context.IncidentIndividuals.AnyAsync(ii => 
                        ii.IncidentId == Guid.Parse(dto.IncidentId) && 
                        ii.IndividualId == Guid.Parse(dto.IndividualId)))
                    {
                        _logger.LogInformation($"Incident-Individual relationship for IncidentId {dto.IncidentId} and IndividualId {dto.IndividualId} already exists. Skipping...");
                        continue;
                    }

                    // Validate IncidentId
                    if (!Guid.TryParse(dto.IncidentId, out var incidentId) || 
                        !await _context.Incidents.AnyAsync(i => i.Id == incidentId))
                    {
                        _logger.LogWarning($"Invalid or non-existent IncidentId '{dto.IncidentId}'. Skipping...");
                        continue;
                    }

                    // Validate IndividualId
                    if (!Guid.TryParse(dto.IndividualId, out var individualId) || 
                        !await _context.Individuals.AnyAsync(i => i.Id == individualId))
                    {
                        _logger.LogWarning($"Invalid or non-existent IndividualId '{dto.IndividualId}'. Skipping...");
                        continue;
                    }

                    // Create new incident-individual relationship
                    var incidentIndividual = new IncidentIndividual
                    {
                        Id = Guid.Parse(dto.Id),
                        IncidentId = incidentId,
                        IndividualId = individualId,
                        Role = dto.Role ?? "Unspecified",
                        Description = dto.Description,
                        CreatedAt = dto.CreatedAt == default ? DateTime.UtcNow : dto.CreatedAt,
                        UpdatedAt = dto.UpdatedAt,
                        IsActive = true
                    };

                    _context.IncidentIndividuals.Add(incidentIndividual);
                    count++;

                    // Save changes in batches to improve performance
                    if (count % 50 == 0)
                    {
                        await _context.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error seeding incident-individual relationship for IncidentId {dto.IncidentId} and IndividualId {dto.IndividualId}");
                    // Continue with next relationship even if one fails
                }
            }

            // Save any remaining changes
            if (_context.ChangeTracker.HasChanges())
            {
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation($"Successfully seeded {count} incident-individual relationships.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding incident-individual relationships.");
            throw;
        }
    }

    private async Task SeedIncidentsAsync()
    {
        _logger.LogInformation("Seeding Incidents...");

        try
        {
            var csvPath = Path.Combine(_dataPath, "incidents.csv");
            if (!File.Exists(csvPath))
            {
                _logger.LogWarning("Incidents CSV file not found at {CsvPath}", csvPath);
                return;
            }

            var incidentDtos = await _csvReader.ReadCsvAsync<IncidentDto>(csvPath);
            if (!incidentDtos?.Any() ?? true)
            {
                _logger.LogWarning("No incident records found in the CSV file.");
                return;
            }

            int count = 0;
            foreach (var dto in incidentDtos!)
            {
                try
                {
                    // Skip if incident already exists
                    if (await _context.Incidents.AnyAsync(i => i.Id == Guid.Parse(dto.Id)))
                    {
                        _logger.LogInformation($"Incident with ID {dto.Id} already exists. Skipping...");
                        continue;
                    }

                    // Validate organization ID
                    if (!Guid.TryParse(dto.OrganizationId, out var orgId))
                    {
                        _logger.LogWarning($"Invalid OrganizationId '{dto.OrganizationId}' for incident {dto.IncidentNumber}. Skipping...");
                        continue;
                    }

                    // Check if organization exists
                    var organization = await _context.Organizations.FindAsync(orgId);
                    if (organization == null)
                    {
                        _logger.LogWarning($"Organization with ID {dto.OrganizationId} not found for incident {dto.IncidentNumber}. Skipping...");
                        continue;
                    }

                    // Get reported by user (default to first admin if not found)
                    var reportedByUser = await _userManager.FindByEmailAsync(dto.ReportedBy) ??
                                      await _userManager.FindByEmailAsync("admin@tracker.com") ??
                                      await _userManager.Users.FirstOrDefaultAsync();

                    if (reportedByUser == null)
                    {
                        _logger.LogWarning($"No valid users found to assign as reported by for incident {dto.IncidentNumber}. Skipping...");
                        continue;
                    }

                    // Find the individual record associated with the user
                    var reportedByIndividual = await _context.Individuals
                        .FirstOrDefaultAsync(i => i.Email == reportedByUser.Email);

                    if (reportedByIndividual == null)
                    {
                        _logger.LogWarning($"No individual record found for user {reportedByUser.Email}. Skipping...");
                        continue;
                    }

                    // Create new incident
                    var incident = new Incident
                    {
                        Id = Guid.Parse(dto.Id),
                        Title = dto.Title,
                        Description = dto.Description ?? "No description provided.",
                        Status = dto.Status,
                        Severity = dto.Priority,
                        OrganizationId = orgId,
                        ReportedById = reportedByIndividual.Id,
                        CreatedAt = dto.CreatedAt == default ? DateTime.UtcNow : dto.CreatedAt,
                        UpdatedAt = dto.UpdatedAt,
                        IsActive = true
                    };

                    // Set assigned to if specified
                    if (!string.IsNullOrEmpty(dto.AssignedTo) && 
                        Guid.TryParse(dto.AssignedTo, out var assignedToId) &&
                        await _context.Users.AnyAsync(u => u.Id == assignedToId))
                    {
                        incident.AssignedToId = assignedToId;
                    }

                    _context.Incidents.Add(incident);
                    count++;

                    // Create initial timeline entry
                    var timelineEntry = new IncidentTimeline
                    {
                        Id = Guid.NewGuid(),
                        IncidentId = incident.Id,
                        Event = "Incident Created",
                        Description = $"Incident was created by {reportedByIndividual.FirstName} {reportedByIndividual.LastName}.",
                        UpdatedById = reportedByIndividual.Id,
                        Timestamp = incident.CreatedAt,
                        IsActive = true
                    };

                    _context.IncidentTimelines.Add(timelineEntry);

                    // Save changes in batches to improve performance
                    if (count % 50 == 0)
                    {
                        await _context.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error seeding incident {dto.IncidentNumber}");
                    // Continue with next incident even if one fails
                }
            }

            // Save any remaining changes
            if (_context.ChangeTracker.HasChanges())
            {
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation($"Successfully seeded {count} incidents.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding incidents.");
            throw;
        }
    }
}

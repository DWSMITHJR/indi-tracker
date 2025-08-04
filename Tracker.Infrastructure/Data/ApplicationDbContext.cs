using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Tracker.Infrastructure.Models;

namespace Tracker.Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Organization> Organizations { get; set; }
        public DbSet<Individual> Individuals { get; set; }
        public DbSet<Incident> Incidents { get; set; }
        public DbSet<IncidentIndividual> IncidentIndividuals { get; set; }
        public DbSet<IncidentTimeline> IncidentTimelines { get; set; }
        public DbSet<IncidentAttachment> IncidentAttachments { get; set; }
        public DbSet<Contact> Contacts { get; set; }
        public DbSet<EnrollmentCode> EnrollmentCodes { get; set; }
        public DbSet<LogEntry> Logs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User-Organization many-to-many relationship
            modelBuilder.Entity<User>()
                .HasMany(u => u.Organizations)
                .WithMany(o => o.Users)
                .UsingEntity(j => j.ToTable("UserOrganizations"));

            // Configure Incident-Individual many-to-many relationship
            modelBuilder.Entity<IncidentIndividual>()
                .HasKey(ii => new { ii.IncidentId, ii.IndividualId });

            modelBuilder.Entity<IncidentIndividual>()
                .HasOne(ii => ii.Incident)
                .WithMany(i => i.InvolvedIndividuals)
                .HasForeignKey(ii => ii.IncidentId);

            modelBuilder.Entity<IncidentIndividual>()
                .HasOne(ii => ii.Individual)
                .WithMany(i => i.IncidentInvolvements)
                .HasForeignKey(ii => ii.IndividualId);

            // Configure LogEntry entity
            modelBuilder.Entity<LogEntry>(entity =>
            {
                entity.Property(e => e.Timestamp).IsRequired();
                entity.Property(e => e.Level).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Message).IsRequired();
                entity.Property(e => e.Source).HasMaxLength(255);
                entity.Property(e => e.UserId).HasMaxLength(450);
                
                // Add index on timestamp for better query performance
                entity.HasIndex(e => e.Timestamp);
                
                // Add index on level for filtering
                entity.HasIndex(e => e.Level);
                
                // Add index on user ID for filtering by user
                entity.HasIndex(e => e.UserId);
            });

            // Configure enums and constraints
            modelBuilder.Entity<Incident>()
                .Property(i => i.Status)
                .HasConversion<string>();

            modelBuilder.Entity<Incident>()
                .Property(i => i.Severity)
                .HasConversion<string>();

            modelBuilder.Entity<Individual>()
                .Property(i => i.Status)
                .HasConversion<string>();

            // Configure cascading deletes
            modelBuilder.Entity<Organization>()
                .HasMany(o => o.Individuals)
                .WithOne(i => i.Organization)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Organization>()
                .HasMany(o => o.Contacts)
                .WithOne(c => c.Organization)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Organization>()
                .HasMany(o => o.Incidents)
                .WithOne(i => i.Organization)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Organization>()
                .HasMany(o => o.EnrollmentCodes)
                .WithOne(e => e.Organization)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Incident>()
                .HasMany(i => i.Timeline)
                .WithOne(t => t.Incident)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Incident>()
                .HasMany(i => i.Attachments)
                .WithOne(a => a.Incident)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure indexes
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Organization>()
                .HasIndex(o => o.Name)
                .IsUnique();

            modelBuilder.Entity<EnrollmentCode>()
                .HasIndex(ec => ec.Code)
                .IsUnique();
        }
    }
}

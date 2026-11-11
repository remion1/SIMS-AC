using Microsoft.EntityFrameworkCore;
using Sims.Backend.Models;

namespace Sims.Backend.Data;

public sealed class SimsDbContext(DbContextOptions<SimsDbContext> options) : DbContext(options)
{
    public DbSet<Incident> Incidents => Set<Incident>();
    public DbSet<User>     Users     => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Incident>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Severity)
                  .HasConversion<string>()
                  .HasMaxLength(20);

            entity.Property(e => e.Status)
                  .HasConversion<string>()
                  .HasMaxLength(20);

            entity.Property(e => e.Title)
                  .IsRequired()
                  .HasMaxLength(150);

            entity.Property(e => e.Description)
                  .IsRequired();

            entity.Property(e => e.AffectedSystem)
                  .IsRequired();

            entity.Property(e => e.ReportedBy)
                  .IsRequired();

            entity.Property(e => e.CveNumber)
                  .HasMaxLength(30)
                  .IsRequired(false);

            entity.Property(e => e.AssignedTo)
                  .IsRequired(false);

            entity.HasData(
                new Incident
                {
                    Id            = 1,
                    Timestamp     = new DateTime(2024, 3, 29, 10, 15, 0, DateTimeKind.Utc),
                    Title         = "Critical backdoor in XZ Utils (CVE-2024-3094)",
                    Description   = "Malicious code injected into liblzma 5.6.0/5.6.1 enables " +
                                    "unauthorised remote access via systemd/sshd on affected Linux hosts. " +
                                    "Auth-Server fleet must be patched immediately.",
                    AffectedSystem = "Auth-Server",
                    CveNumber     = "CVE-2024-3094",
                    Severity      = IncidentSeverity.Critical,
                    Status        = IncidentStatus.InProgress,
                    ReportedBy    = "alice.mueller@example.com",
                    AssignedTo    = "bob.schmidt@example.com"
                },
                new Incident
                {
                    Id            = 2,
                    Timestamp     = new DateTime(2024, 4, 5, 8, 0, 0, DateTimeKind.Utc),
                    Title         = "Elevated error rate on Payment Gateway",
                    Description   = "Payment-Gateway reported a 15 % increase in HTTP 500 responses " +
                                    "between 07:45 and 08:00 UTC. Root cause identified as a " +
                                    "misconfigured load-balancer rule after the nightly deployment.",
                    AffectedSystem = "Payment-Gateway",
                    CveNumber     = null,
                    Severity      = IncidentSeverity.High,
                    Status        = IncidentStatus.Escalated,
                    ReportedBy    = "monitoring@example.com",
                    AssignedTo    = "carol.bauer@example.com"
                },
                new Incident
                {
                    Id            = 3,
                    Timestamp     = new DateTime(2024, 4, 10, 14, 30, 0, DateTimeKind.Utc),
                    Title         = "Stale SSL certificate on internal wiki",
                    Description   = "The TLS certificate for the internal Confluence wiki expired " +
                                    "and was replaced within the agreed SLA window. No data exposure.",
                    AffectedSystem = "Internal-Wiki",
                    CveNumber     = null,
                    Severity      = IncidentSeverity.Low,
                    Status        = IncidentStatus.Closed,
                    ReportedBy    = "helpdesk@example.com",
                    AssignedTo    = null
                }
            );
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Username)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.HasIndex(e => e.Username)
                  .IsUnique()
                  .HasDatabaseName("IX_Users_Username");

            entity.Property(e => e.Email)
                  .IsRequired()
                  .HasMaxLength(256);

            entity.Property(e => e.PasswordHash)
                  .IsRequired();

            entity.Property(e => e.Role)
                  .HasConversion<string>()
                  .HasMaxLength(20);

            // admin / Admin1234!  |  analyst / Analyst1234!
            entity.HasData(
                new User
                {
                    Id           = 1,
                    Username     = "admin",
                    Email        = "admin@sims.local",
                    PasswordHash = "$2a$12$ePUMBLnGPXv0N9i6FmCPA.AjdDCd1zZN6OdZsEOJrKRJPWW.pTWCe",
                    Role         = UserRole.Admin,
                    IsActive     = true,
                    CreatedAt    = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new User
                {
                    Id           = 2,
                    Username     = "analyst",
                    Email        = "analyst@sims.local",
                    PasswordHash = "$2a$12$mHsNXFVn1cz9Lm2mXr6cKOr/JlmAVxNmJu3fXsf/7eOPFnPbcPpWC",
                    Role         = UserRole.User,
                    IsActive     = true,
                    CreatedAt    = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );
        });
    }
}

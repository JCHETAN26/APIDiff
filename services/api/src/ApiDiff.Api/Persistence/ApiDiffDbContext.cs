using ApiDiff.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace ApiDiff.Api.Persistence;

/// <summary>
/// The single system-of-record database context (ADR 0003). Owns the schema and
/// migrations for all APIDiff domain entities.
/// </summary>
public class ApiDiffDbContext(DbContextOptions<ApiDiffDbContext> options) : DbContext(options)
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Membership> Memberships => Set<Membership>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Scenario> Scenarios => Set<Scenario>();
    public DbSet<ScenarioCluster> ScenarioClusters => Set<ScenarioCluster>();
    public DbSet<RegressionRun> RegressionRuns => Set<RegressionRun>();
    public DbSet<ReplayResult> ReplayResults => Set<ReplayResult>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Organization>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Slug).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.Slug).IsUnique();
        });

        b.Entity<User>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ExternalSubject).HasMaxLength(255).IsRequired();
            e.Property(x => x.Email).HasMaxLength(320).IsRequired();
            e.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.ExternalSubject).IsUnique();
            e.HasIndex(x => x.Email).IsUnique();
        });

        b.Entity<Membership>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Organization).WithMany(o => o.Memberships)
                .HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany(u => u.Memberships)
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.OrganizationId, x.UserId }).IsUnique();
        });

        b.Entity<Project>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Slug).HasMaxLength(100).IsRequired();
            e.Property(x => x.GitHubRepo).HasMaxLength(255).IsRequired();
            e.Property(x => x.BaselineBaseUrl).HasMaxLength(2048).IsRequired();
            e.HasOne(x => x.Organization).WithMany(o => o.Projects)
                .HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.OrganizationId, x.Slug }).IsUnique();
        });

        b.Entity<Scenario>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Method).HasMaxLength(10).IsRequired();
            e.Property(x => x.Path).HasMaxLength(2048).IsRequired();
            e.Property(x => x.Fingerprint).HasMaxLength(64).IsRequired();
            e.HasOne(x => x.Project).WithMany(p => p.Scenarios)
                .HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Cluster).WithMany(c => c.Members)
                .HasForeignKey(x => x.ClusterId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(x => new { x.ProjectId, x.Fingerprint });
        });

        b.Entity<ScenarioCluster>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Project).WithMany(p => p.Clusters)
                .HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<RegressionRun>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.CommitSha).HasMaxLength(40).IsRequired();
            e.Property(x => x.CandidateBaseUrl).HasMaxLength(2048);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.HasOne(x => x.Project).WithMany(p => p.Runs)
                .HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.ProjectId, x.PullRequestNumber });
        });

        b.Entity<ReplayResult>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Verdict).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.DiffJson).HasColumnType("jsonb");
            e.HasOne(x => x.Run).WithMany(r => r.Results)
                .HasForeignKey(x => x.RunId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Scenario).WithMany()
                .HasForeignKey(x => x.ScenarioId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.RunId, x.ScenarioId }).IsUnique();
        });

        b.Entity<AuditLog>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Action).HasMaxLength(100).IsRequired();
            e.Property(x => x.TargetType).HasMaxLength(100).IsRequired();
            e.Property(x => x.TargetId).HasMaxLength(100).IsRequired();
            e.Property(x => x.MetadataJson).HasColumnType("jsonb");
            e.HasIndex(x => new { x.OrganizationId, x.CreatedAt });
        });
    }
}

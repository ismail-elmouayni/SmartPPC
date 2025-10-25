using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartPPC.Core.Domain;

namespace SmartPPC.Api.Data;

/// <summary>
/// Application database context that includes Identity and SmartPPC domain entities
/// </summary>
public class ApplicationDbContext : IdentityDbContext<User>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Domain entities
    public DbSet<Configuration> Configurations { get; set; }
    public DbSet<GeneralSettings> GeneralSettings { get; set; }
    public DbSet<StationDeclaration> StationDeclarations { get; set; }
    public DbSet<StationPastBuffer> StationPastBuffers { get; set; }
    public DbSet<StationPastOrderAmount> StationPastOrderAmounts { get; set; }
    public DbSet<StationDemandForecast> StationDemandForecasts { get; set; }
    public DbSet<StationInput> StationInputs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User entity configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Address).HasMaxLength(500);
        });

        // Configuration entity
        modelBuilder.Entity<Configuration>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.UserId).IsRequired();
            entity.HasIndex(e => new { e.UserId, e.Name });
            entity.HasIndex(e => new { e.UserId, e.IsActive });

            // Relationship with User
            entity.HasOne(e => e.User)
                .WithMany(u => u.Configurations)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // GeneralSettings entity (1-to-1 with Configuration)
        modelBuilder.Entity<GeneralSettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ConfigurationId).IsUnique();

            entity.HasOne(e => e.Configuration)
                .WithOne(c => c.GeneralSettings)
                .HasForeignKey<GeneralSettings>(e => e.ConfigurationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // StationDeclaration entity
        modelBuilder.Entity<StationDeclaration>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ConfigurationId, e.StationIndex });

            entity.HasOne(e => e.Configuration)
                .WithMany(c => c.StationDeclarations)
                .HasForeignKey(e => e.ConfigurationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // StationPastBuffer entity
        modelBuilder.Entity<StationPastBuffer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.StationDeclarationId, e.Instant });

            entity.HasOne(e => e.StationDeclaration)
                .WithMany(s => s.PastBuffers)
                .HasForeignKey(e => e.StationDeclarationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // StationPastOrderAmount entity
        modelBuilder.Entity<StationPastOrderAmount>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.StationDeclarationId, e.Instant });

            entity.HasOne(e => e.StationDeclaration)
                .WithMany(s => s.PastOrderAmounts)
                .HasForeignKey(e => e.StationDeclarationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // StationDemandForecast entity
        modelBuilder.Entity<StationDemandForecast>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.StationDeclarationId, e.Instant });

            entity.HasOne(e => e.StationDeclaration)
                .WithMany(s => s.DemandForecasts)
                .HasForeignKey(e => e.StationDeclarationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // StationInput entity
        modelBuilder.Entity<StationInput>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.SourceStationDeclarationId, e.TargetStationIndex });

            entity.HasOne(e => e.SourceStationDeclaration)
                .WithMany(s => s.NextStationInputs)
                .HasForeignKey(e => e.SourceStationDeclarationId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

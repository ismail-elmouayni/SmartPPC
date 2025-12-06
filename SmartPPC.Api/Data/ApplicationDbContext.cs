using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartPPC.Core.Domain;
using SmartPPC.Core.ML.Domain;

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

    // ML Forecasting entities
    public DbSet<ForecastTrainingData> ForecastTrainingData { get; set; }
    public DbSet<ForecastModel> ForecastModels { get; set; }
    public DbSet<ForecastPrediction> ForecastPredictions { get; set; }
    public DbSet<ModelMetrics> ModelMetrics { get; set; }

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

        // ============================================================
        // ML Forecasting Entity Configurations
        // ============================================================

        // ForecastTrainingData entity
        modelBuilder.Entity<ForecastTrainingData>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.StationDeclarationId, e.ObservationDate });
            entity.HasIndex(e => new { e.ConfigurationId, e.ObservationDate });
            entity.HasIndex(e => e.ObservationDate); // For date range queries

            entity.Property(e => e.DemandValue).IsRequired();
            entity.Property(e => e.DayOfWeek).IsRequired();
            entity.Property(e => e.Month).IsRequired();
            entity.Property(e => e.Quarter).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();

            // Note: StationDeclarationId stores the station index (int), not the GUID
            // No foreign key relationship since StationDeclaration.Id is Guid but we store int

            // Relationships
            entity.HasOne(e => e.Configuration)
                .WithMany()
                .HasForeignKey(e => e.ConfigurationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ForecastModel entity
        modelBuilder.Entity<ForecastModel>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ConfigurationId, e.IsActive });
            entity.HasIndex(e => e.CreatedAt);

            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ModelType).IsRequired();
            entity.Property(e => e.Version).IsRequired().HasMaxLength(50);
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();

            // ModelData stored as BYTEA in PostgreSQL
            entity.Property(e => e.ModelData).HasColumnType("bytea");

            // Relationship
            entity.HasOne(e => e.Configuration)
                .WithMany()
                .HasForeignKey(e => e.ConfigurationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ForecastPrediction entity
        modelBuilder.Entity<ForecastPrediction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.StationDeclarationId, e.ForecastStartDate });
            entity.HasIndex(e => new { e.ForecastModelId, e.PredictionDate });
            entity.HasIndex(e => e.PredictionDate);

            entity.Property(e => e.PredictionDate).IsRequired();
            entity.Property(e => e.ForecastStartDate).IsRequired();
            entity.Property(e => e.PredictedValues).IsRequired();
            entity.Property(e => e.WasUsedInPlanning).IsRequired();
            entity.Property(e => e.WasOverridden).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();

            // JSON fields stored as JSONB in PostgreSQL for efficient querying
            entity.Property(e => e.PredictedValues).HasColumnType("jsonb");
            entity.Property(e => e.UpperConfidenceInterval).HasColumnType("jsonb");
            entity.Property(e => e.LowerConfidenceInterval).HasColumnType("jsonb");
            entity.Property(e => e.ActualValues).HasColumnType("jsonb");

            // Relationships
            entity.HasOne(e => e.ForecastModel)
                .WithMany()
                .HasForeignKey(e => e.ForecastModelId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent deletion if predictions exist

            // Note: StationDeclarationId stores the station index (int), not the GUID
            // No foreign key relationship since StationDeclaration.Id is Guid but we store int
        });

        // ModelMetrics entity
        modelBuilder.Entity<ModelMetrics>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ForecastModelId, e.EvaluationType });
            entity.HasIndex(e => new { e.ForecastModelId, e.StationDeclarationId });
            entity.HasIndex(e => e.CreatedAt);

            entity.Property(e => e.EvaluationType).IsRequired();
            entity.Property(e => e.MAE).IsRequired();
            entity.Property(e => e.MAPE).IsRequired();
            entity.Property(e => e.RMSE).IsRequired();
            entity.Property(e => e.SampleCount).IsRequired();
            entity.Property(e => e.EvaluationStartDate).IsRequired();
            entity.Property(e => e.EvaluationEndDate).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();

            // JSON field for additional metrics
            entity.Property(e => e.AdditionalMetrics).HasColumnType("jsonb");

            // Relationships
            entity.HasOne(e => e.ForecastModel)
                .WithMany()
                .HasForeignKey(e => e.ForecastModelId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.StationDeclaration)
                .WithMany()
                .HasForeignKey(e => e.StationDeclarationId)
                .OnDelete(DeleteBehavior.SetNull); // Metrics can exist without station (aggregated)
        });
    }
}

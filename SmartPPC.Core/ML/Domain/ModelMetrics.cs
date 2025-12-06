using System;

namespace SmartPPC.Core.ML.Domain;

/// <summary>
/// Represents comprehensive evaluation metrics for a forecasting model.
/// Used to track model performance over time and compare different models.
/// </summary>
public class ModelMetrics
{
    /// <summary>
    /// Unique identifier for this metrics record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Reference to the forecast model being evaluated.
    /// </summary>
    public Guid ForecastModelId { get; set; }

    /// <summary>
    /// Reference to the station (null if metrics are aggregated across all stations).
    /// </summary>
    public Guid? StationDeclarationId { get; set; }

    /// <summary>
    /// Type of evaluation (training, validation, test, production).
    /// </summary>
    public EvaluationType EvaluationType { get; set; }

    /// <summary>
    /// Mean Absolute Error: Average absolute difference between predicted and actual values.
    /// Lower is better. Unit: same as demand (e.g., units).
    /// </summary>
    public float MAE { get; set; }

    /// <summary>
    /// Mean Absolute Percentage Error: Average percentage difference.
    /// Lower is better. Unit: percentage (e.g., 0.15 = 15% error).
    /// </summary>
    public float MAPE { get; set; }

    /// <summary>
    /// Root Mean Squared Error: Square root of average squared errors.
    /// Penalizes larger errors more heavily. Lower is better.
    /// </summary>
    public float RMSE { get; set; }

    /// <summary>
    /// R-squared (coefficient of determination): Proportion of variance explained by the model.
    /// Higher is better. Range: [0, 1] (or negative if model is very poor).
    /// </summary>
    public float? RSquared { get; set; }

    /// <summary>
    /// Mean Forecast Error (bias): Average signed difference (predicted - actual).
    /// Indicates if model tends to over-forecast (positive) or under-forecast (negative).
    /// </summary>
    public float? MeanForecastError { get; set; }

    /// <summary>
    /// Standard deviation of forecast errors.
    /// Measures forecast variability.
    /// </summary>
    public float? ForecastErrorStdDev { get; set; }

    /// <summary>
    /// Tracking signal: Cumulative forecast error / MAD (Mean Absolute Deviation).
    /// Monitors forecast bias. Should stay within ±4 typically.
    /// </summary>
    public float? TrackingSignal { get; set; }

    /// <summary>
    /// Number of data points used in this evaluation.
    /// </summary>
    public int SampleCount { get; set; }

    /// <summary>
    /// Start date of the evaluation period.
    /// </summary>
    public DateTime EvaluationStartDate { get; set; }

    /// <summary>
    /// End date of the evaluation period.
    /// </summary>
    public DateTime EvaluationEndDate { get; set; }

    /// <summary>
    /// Forecast horizon used (number of periods ahead).
    /// </summary>
    public int? ForecastHorizon { get; set; }

    /// <summary>
    /// Additional metrics in JSON format for extensibility.
    /// Can include custom metrics, per-horizon accuracy, etc.
    /// </summary>
    public string? AdditionalMetrics { get; set; }

    /// <summary>
    /// Timestamp when this evaluation was performed.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Navigation property to the forecast model.
    /// </summary>
    public ForecastModel? ForecastModel { get; set; }

    /// <summary>
    /// Navigation property to the station declaration (if station-specific).
    /// </summary>
    public SmartPPC.Core.Domain.StationDeclaration? StationDeclaration { get; set; }
}

/// <summary>
/// Types of model evaluation contexts.
/// </summary>
public enum EvaluationType
{
    /// <summary>
    /// Metrics computed on the training dataset.
    /// </summary>
    Training = 1,

    /// <summary>
    /// Metrics computed on the validation dataset (used during training).
    /// </summary>
    Validation = 2,

    /// <summary>
    /// Metrics computed on a held-out test dataset (post-training).
    /// </summary>
    Test = 3,

    /// <summary>
    /// Metrics computed on actual production forecasts vs realized demand.
    /// </summary>
    Production = 4,

    /// <summary>
    /// Cross-validation metrics (averaged across folds).
    /// </summary>
    CrossValidation = 5
}

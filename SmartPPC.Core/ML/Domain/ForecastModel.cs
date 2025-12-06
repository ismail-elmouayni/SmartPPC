using System;

namespace SmartPPC.Core.ML.Domain;

/// <summary>
/// Represents a trained machine learning model for demand forecasting.
/// Stores model metadata, serialized weights, and performance metrics.
/// </summary>
public class ForecastModel
{
    /// <summary>
    /// Unique identifier for this model.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// User-friendly name for the model (e.g., "LSTM-Attention-v1", "Production Model").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Type of model architecture used.
    /// </summary>
    public ForecastModelType ModelType { get; set; }

    /// <summary>
    /// Reference to the configuration this model was trained for.
    /// </summary>
    public Guid ConfigurationId { get; set; }

    /// <summary>
    /// Serialized model data (ONNX format or ML.NET binary).
    /// Stored as byte array in database.
    /// </summary>
    public byte[]? ModelData { get; set; }

    /// <summary>
    /// Training accuracy metric (e.g., MAPE on training set).
    /// </summary>
    public float? TrainingAccuracy { get; set; }

    /// <summary>
    /// Validation accuracy metric (e.g., MAPE on validation set).
    /// </summary>
    public float? ValidationAccuracy { get; set; }

    /// <summary>
    /// Mean Absolute Error on validation set.
    /// </summary>
    public float? ValidationMAE { get; set; }

    /// <summary>
    /// Root Mean Squared Error on validation set.
    /// </summary>
    public float? ValidationRMSE { get; set; }

    /// <summary>
    /// Number of training samples used.
    /// </summary>
    public int? TrainingSampleCount { get; set; }

    /// <summary>
    /// Number of validation samples used.
    /// </summary>
    public int? ValidationSampleCount { get; set; }

    /// <summary>
    /// JSON string containing model hyperparameters
    /// (e.g., learning rate, hidden units, attention heads).
    /// </summary>
    public string? Hyperparameters { get; set; }

    /// <summary>
    /// Version number for model tracking (e.g., "1.0", "2.3").
    /// </summary>
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// Whether this model is currently active for predictions.
    /// Only one model per configuration should be active at a time.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Timestamp when the model was created/trained.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Timestamp of last update (e.g., when activated/deactivated).
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Optional description or notes about this model.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Navigation property to the configuration.
    /// </summary>
    public SmartPPC.Core.Domain.Configuration? Configuration { get; set; }

    public DateTime TrainingStartDate { get; set; }
    public DateTime TrainingEndDate { get; set; }
    public int LookbackWindow { get; set; }
    public int ForecastHorizon { get; set; }
    public float ValidationMAPE { get; set; }
    public int FeatureCount { get; set; }
}

public enum ForecastModelType
{
    /// <summary>
    /// Long Short-Term Memory network.
    /// </summary>
    LSTM = 1,

    /// <summary>
    /// Gated Recurrent Unit network.
    /// </summary>
    GRU = 2,

    /// <summary>
    /// LSTM with attention mechanism (hybrid approach).
    /// </summary>
    LSTMAttention = 3,

    /// <summary>
    /// Temporal Fusion Transformer.
    /// </summary>
    TemporalFusionTransformer = 4,

    /// <summary>
    /// Simple moving average (baseline/fallback).
    /// </summary>
    MovingAverage = 5,

    /// <summary>
    /// Custom or experimental model type.
    /// </summary>
    Custom = 99
}

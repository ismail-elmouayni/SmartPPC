using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentResults;
using SmartPPC.Core.ML.Domain;

namespace SmartPPC.Core.ML.Services;

/// <summary>
/// Main service interface for demand forecasting using machine learning models.
/// Handles model training, prediction generation, and model lifecycle management.
/// </summary>
public interface IForecastingService
{
    /// <summary>
    /// Trains a new forecasting model for a specific configuration.
    /// </summary>
    /// <param name="configurationId">The configuration ID</param>
    /// <param name="modelType">Type of model to train (LSTM, LSTMAttention, etc.)</param>
    /// <param name="parameters">Training parameters (hyperparameters, data split, etc.)</param>
    /// <returns>The trained forecast model with metadata</returns>
    Task<Result<ForecastModel>> TrainModelAsync(
        Guid configurationId,
        ForecastModelType modelType,
        TrainingParameters parameters);

    /// <summary>
    /// Trains models for individual stations (one model per station).
    /// Useful when stations have significantly different demand patterns.
    /// </summary>
    /// <param name="configurationId">The configuration ID</param>
    /// <param name="stationIds">Collection of station IDs to train models for</param>
    /// <param name="modelType">Type of model to train</param>
    /// <param name="parameters">Training parameters</param>
    /// <returns>Dictionary mapping station ID to trained model</returns>
    Task<Result<Dictionary<int, ForecastModel>>> TrainPerStationModelsAsync(
        Guid configurationId,
        IEnumerable<int> stationIds,
        ForecastModelType modelType,
        TrainingParameters parameters);

    /// <summary>
    /// Generates demand forecast for a specific station using the active model.
    /// </summary>
    /// <param name="stationId">The station declaration ID</param>
    /// <param name="forecastHorizon">Number of periods to forecast ahead</param>
    /// <param name="startDate">Starting date for the forecast (default: now)</param>
    /// <returns>Forecast prediction with confidence intervals</returns>
    Task<Result<ForecastPrediction>> GenerateForecastAsync(
        int stationId,
        int forecastHorizon,
        DateTime? startDate = null);

    /// <summary>
    /// Generates demand forecasts for all stations in a configuration.
    /// Uses the active model(s) for each station.
    /// </summary>
    /// <param name="configurationId">The configuration ID</param>
    /// <param name="forecastHorizon">Number of periods to forecast ahead</param>
    /// <param name="startDate">Starting date for forecasts (default: now)</param>
    /// <returns>Dictionary mapping station ID to forecast prediction</returns>
    Task<Result<Dictionary<int, ForecastPrediction>>> GenerateForecastsForAllStationsAsync(
        Guid configurationId,
        int forecastHorizon,
        DateTime? startDate = null);

    /// <summary>
    /// Generates forecast using a specific model (not necessarily the active one).
    /// Useful for comparing different models.
    /// </summary>
    /// <param name="forecastModelId">The specific model ID to use</param>
    /// <param name="stationId">The station declaration ID</param>
    /// <param name="forecastHorizon">Number of periods to forecast ahead</param>
    /// <param name="startDate">Starting date for the forecast</param>
    /// <returns>Forecast prediction</returns>
    Task<Result<ForecastPrediction>> GenerateForecastWithModelAsync(
        Guid forecastModelId,
        int stationId,
        int forecastHorizon,
        DateTime? startDate = null);

    /// <summary>
    /// Evaluates a model's performance using historical data (backtesting).
    /// </summary>
    /// <param name="forecastModelId">The model to evaluate</param>
    /// <param name="evaluationStartDate">Start date for evaluation period</param>
    /// <param name="evaluationEndDate">End date for evaluation period</param>
    /// <returns>Comprehensive model metrics</returns>
    Task<Result<ModelMetrics>> EvaluateModelAsync(
        Guid forecastModelId,
        DateTime evaluationStartDate,
        DateTime evaluationEndDate);

    /// <summary>
    /// Evaluates model performance by station (station-specific metrics).
    /// </summary>
    /// <param name="forecastModelId">The model to evaluate</param>
    /// <param name="stationId">The station to evaluate for</param>
    /// <param name="evaluationStartDate">Start date for evaluation period</param>
    /// <param name="evaluationEndDate">End date for evaluation period</param>
    /// <returns>Station-specific model metrics</returns>
    Task<Result<ModelMetrics>> EvaluateModelForStationAsync(
        Guid forecastModelId,
        int stationId,
        DateTime evaluationStartDate,
        DateTime evaluationEndDate);

    /// <summary>
    /// Compares performance of multiple models side-by-side.
    /// </summary>
    /// <param name="modelIds">Collection of model IDs to compare</param>
    /// <param name="evaluationStartDate">Start date for comparison period</param>
    /// <param name="evaluationEndDate">End date for comparison period</param>
    /// <returns>List of metrics for each model</returns>
    Task<Result<IEnumerable<ModelMetrics>>> CompareModelsAsync(
        IEnumerable<Guid> modelIds,
        DateTime evaluationStartDate,
        DateTime evaluationEndDate);

    /// <summary>
    /// Activates a specific model for use in production forecasting.
    /// Deactivates any previously active model for the same configuration.
    /// </summary>
    /// <param name="forecastModelId">The model ID to activate</param>
    /// <returns>The activated model</returns>
    Task<Result<ForecastModel>> ActivateModelAsync(Guid forecastModelId);

    /// <summary>
    /// Deactivates a model (e.g., before deleting or replacing).
    /// </summary>
    /// <param name="forecastModelId">The model ID to deactivate</param>
    /// <returns>Success result</returns>
    Task<Result> DeactivateModelAsync(Guid forecastModelId);

    /// <summary>
    /// Gets the currently active model for a configuration.
    /// </summary>
    /// <param name="configurationId">The configuration ID</param>
    /// <returns>The active forecast model, or null if none is active</returns>
    Task<Result<ForecastModel?>> GetActiveModelAsync(Guid configurationId);

    /// <summary>
    /// Lists all models for a configuration.
    /// </summary>
    /// <param name="configurationId">The configuration ID</param>
    /// <param name="includeInactive">Whether to include inactive models</param>
    /// <returns>Collection of forecast models</returns>
    Task<Result<IEnumerable<ForecastModel>>> ListModelsAsync(
        Guid configurationId,
        bool includeInactive = true);

    /// <summary>
    /// Deletes a model and all its associated predictions and metrics.
    /// </summary>
    /// <param name="forecastModelId">The model ID to delete</param>
    /// <returns>Success result</returns>
    Task<Result> DeleteModelAsync(Guid forecastModelId);

    /// <summary>
    /// Retrains an existing model with updated data.
    /// Creates a new version of the model.
    /// </summary>
    /// <param name="forecastModelId">The existing model ID to retrain</param>
    /// <param name="parameters">Training parameters (can be same as original or modified)</param>
    /// <returns>The newly trained model version</returns>
    Task<Result<ForecastModel>> RetrainModelAsync(
        Guid forecastModelId,
        TrainingParameters parameters);

    /// <summary>
    /// Updates actual demand values in predictions for performance tracking.
    /// Called after actual demand is observed to compute prediction accuracy.
    /// </summary>
    /// <param name="predictionId">The prediction ID to update</param>
    /// <param name="actualValues">Array of actual demand values</param>
    /// <returns>Updated prediction with computed metrics (MAE, MAPE)</returns>
    Task<Result<ForecastPrediction>> UpdatePredictionWithActualsAsync(
        Guid predictionId,
        int[] actualValues);

    /// <summary>
    /// Gets predictions for a station within a date range.
    /// </summary>
    /// <param name="stationId">The station declaration ID</param>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <returns>Collection of predictions</returns>
    Task<Result<IEnumerable<ForecastPrediction>>> GetPredictionsAsync(
        int stationId,
        DateTime startDate,
        DateTime endDate);
}

/// <summary>
/// Parameters for training a forecasting model.
/// </summary>
public class TrainingParameters
{
    /// <summary>
    /// Start date for training data.
    /// </summary>
    public DateTime TrainingStartDate { get; set; }

    /// <summary>
    /// End date for training data.
    /// </summary>
    public DateTime TrainingEndDate { get; set; }

    /// <summary>
    /// Proportion of data to use for validation (e.g., 0.2 for 20%).
    /// </summary>
    public float ValidationSplit { get; set; } = 0.2f;

    /// <summary>
    /// Learning rate for model training.
    /// </summary>
    public float LearningRate { get; set; } = 0.001f;

    /// <summary>
    /// Number of training epochs.
    /// </summary>
    public int Epochs { get; set; } = 100;

    /// <summary>
    /// Batch size for training.
    /// </summary>
    public int BatchSize { get; set; } = 32;

    /// <summary>
    /// Number of hidden units in LSTM layers.
    /// </summary>
    public int HiddenUnits { get; set; } = 64;

    /// <summary>
    /// Number of attention heads (for attention-based models).
    /// </summary>
    public int? AttentionHeads { get; set; } = 4;

    /// <summary>
    /// Look-back window: number of past periods to use as input.
    /// </summary>
    public int LookbackWindow { get; set; } = 30;

    /// <summary>
    /// Forecast horizon: number of periods to predict ahead.
    /// </summary>
    public int ForecastHorizon { get; set; } = 14;

    /// <summary>
    /// Dropout rate for regularization (0-1).
    /// </summary>
    public float Dropout { get; set; } = 0.2f;

    /// <summary>
    /// Whether to use early stopping based on validation performance.
    /// </summary>
    public bool UseEarlyStopping { get; set; } = true;

    /// <summary>
    /// Patience for early stopping (epochs without improvement).
    /// </summary>
    public int EarlyStoppingPatience { get; set; } = 10;

    /// <summary>
    /// Random seed for reproducibility.
    /// </summary>
    public int? RandomSeed { get; set; }

    /// <summary>
    /// Additional custom hyperparameters in dictionary form.
    /// </summary>
    public Dictionary<string, object>? CustomParameters { get; set; }
}

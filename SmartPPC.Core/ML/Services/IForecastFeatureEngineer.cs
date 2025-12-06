using System;
using System.Collections.Generic;
using FluentResults;
using SmartPPC.Core.ML.Domain;
using SmartPPC.Core.ML.Features;

namespace SmartPPC.Core.ML.Services;

/// <summary>
/// Service interface for feature engineering in demand forecasting.
/// Transforms raw historical demand data into ML-ready features.
/// </summary>
public interface IForecastFeatureEngineer
{
    /// <summary>
    /// Transforms raw training data into engineered features suitable for ML training.
    /// </summary>
    /// <param name="trainingData">Raw historical demand data</param>
    /// <param name="lookbackWindow">Number of past periods to include as features</param>
    /// <param name="forecastHorizon">Number of periods to predict ahead</param>
    /// <returns>Engineered feature dataset</returns>
    Result<FeatureDataset> EngineerFeatures(
        IEnumerable<ForecastTrainingData> trainingData,
        int lookbackWindow,
        int forecastHorizon);

    /// <summary>
    /// Normalizes features to a standard scale (typically 0-1 or standardized).
    /// </summary>
    /// <param name="dataset">Feature dataset to normalize</param>
    /// <returns>Normalized dataset with scaling parameters</returns>
    Result<NormalizedDataset> NormalizeFeatures(FeatureDataset dataset);

    /// <summary>
    /// Splits dataset into training and validation sets.
    /// </summary>
    /// <param name="dataset">Complete dataset</param>
    /// <param name="validationSplit">Proportion for validation (e.g., 0.2 for 20%)</param>
    /// <param name="useTimeSeriesSplit">If true, uses time-based split (more recent = validation)</param>
    /// <returns>Training and validation datasets</returns>
    Result<(FeatureDataset training, FeatureDataset validation)> SplitDataset(
        FeatureDataset dataset,
        float validationSplit,
        bool useTimeSeriesSplit = true);

    /// <summary>
    /// Creates temporal features from date/time information.
    /// </summary>
    /// <param name="date">Date to extract features from</param>
    /// <returns>Temporal features (day of week, month, seasonality indicators, etc.)</returns>
    TemporalFeatures ExtractTemporalFeatures(DateTime date);

    /// <summary>
    /// Creates lag features from historical demand values.
    /// </summary>
    /// <param name="demandHistory">Historical demand values (ordered by time)</param>
    /// <param name="lags">Lag periods to create (e.g., [1, 7, 14] for 1-day, 1-week, 2-week lags)</param>
    /// <returns>Lag features</returns>
    Result<float[]> CreateLagFeatures(
        IEnumerable<int> demandHistory,
        int[] lags);

    /// <summary>
    /// Creates rolling window statistics (moving averages, std dev, etc.).
    /// </summary>
    /// <param name="demandHistory">Historical demand values</param>
    /// <param name="windows">Window sizes (e.g., [7, 14, 30] for weekly, bi-weekly, monthly)</param>
    /// <returns>Rolling statistics features</returns>
    Result<RollingStatistics[]> CreateRollingFeatures(
        IEnumerable<int> demandHistory,
        int[] windows);

    /// <summary>
    /// Detects and extracts trend component from time series.
    /// </summary>
    /// <param name="demandHistory">Historical demand values</param>
    /// <returns>Trend values and trend strength indicator</returns>
    Result<(float[] trend, float trendStrength)> ExtractTrend(
        IEnumerable<int> demandHistory);

    /// <summary>
    /// Detects and extracts seasonal component from time series.
    /// </summary>
    /// <param name="demandHistory">Historical demand values</param>
    /// <param name="seasonalPeriod">Expected seasonal period (e.g., 7 for weekly)</param>
    /// <returns>Seasonal indices and seasonality strength</returns>
    Result<(float[] seasonal, float seasonalityStrength)> ExtractSeasonality(
        IEnumerable<int> demandHistory,
        int seasonalPeriod);

    /// <summary>
    /// Inverse transforms normalized predictions back to original scale.
    /// </summary>
    /// <param name="normalizedValues">Normalized prediction values</param>
    /// <param name="scalingParams">Scaling parameters from normalization</param>
    /// <returns>Denormalized values in original scale</returns>
    Result<float[]> DenormalizeValues(
        float[] normalizedValues,
        ScalingParameters scalingParams);
}

using System;
using System.Linq;
using FluentResults;
using Microsoft.Extensions.Logging;
using SmartPPC.Core.ML.Domain;
using SmartPPC.Core.ML.Features;

namespace SmartPPC.Core.ML.Models;

/// <summary>
/// Service for evaluating forecasting model performance.
/// </summary>
public class ModelEvaluator
{
    private readonly ILogger<ModelEvaluator> _logger;

    public ModelEvaluator(ILogger<ModelEvaluator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Evaluates a trained model on a dataset.
    /// </summary>
    /// <param name="model">Trained model to evaluate</param>
    /// <param name="dataset">Dataset to evaluate on</param>
    /// <returns>Comprehensive model metrics</returns>
    public Result<ModelMetrics> Evaluate(ITrainedModel model, FeatureDataset dataset)
    {
        try
        {
            _logger.LogInformation("Evaluating model on {SampleCount} samples", dataset.SampleCount);

            var predictions = new float[dataset.SampleCount][];
            var actuals = new float[dataset.SampleCount][];

            // Generate predictions for all samples
            for (int i = 0; i < dataset.SampleCount; i++)
            {
                var sample = dataset.Samples[i];
                var predResult = model.Predict(sample.Features);

                if (predResult.IsFailed)
                {
                    return Result.Fail<ModelMetrics>($"Prediction failed: {predResult.Errors.First().Message}");
                }

                predictions[i] = predResult.Value;
                actuals[i] = sample.Targets;
            }

            // Calculate metrics
            var mae = CalculateMAE(predictions, actuals);
            var mape = CalculateMAPE(predictions, actuals);
            var rmse = CalculateRMSE(predictions, actuals);
            var rSquared = CalculateRSquared(predictions, actuals);
            var mfe = CalculateMeanForecastError(predictions, actuals);
            var stdDev = CalculateForecastErrorStdDev(predictions, actuals, mfe);

            var metrics = new ModelMetrics
            {
                Id = Guid.NewGuid(),
                EvaluationType = EvaluationType.Test,
                MAE = mae,
                MAPE = mape,
                RMSE = rmse,
                RSquared = rSquared,
                MeanForecastError = mfe,
                ForecastErrorStdDev = stdDev,
                SampleCount = dataset.SampleCount,
                EvaluationStartDate = dataset.Samples.First().Timestamp,
                EvaluationEndDate = dataset.Samples.Last().Timestamp,
                ForecastHorizon = dataset.ForecastHorizon,
                CreatedAt = DateTime.UtcNow
            };

            _logger.LogInformation(
                "Evaluation complete: MAE={MAE:F2}, MAPE={MAPE:F2}%, RMSE={RMSE:F2}, R²={R2:F4}",
                mae, mape, rmse, rSquared);

            return Result.Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating model");
            return Result.Fail<ModelMetrics>($"Model evaluation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Calculates Mean Absolute Error.
    /// </summary>
    private float CalculateMAE(float[][] predictions, float[][] actuals)
    {
        float totalError = 0;
        int count = 0;

        for (int i = 0; i < predictions.Length; i++)
        {
            for (int j = 0; j < predictions[i].Length; j++)
            {
                totalError += Math.Abs(predictions[i][j] - actuals[i][j]);
                count++;
            }
        }

        return count > 0 ? totalError / count : 0;
    }

    /// <summary>
    /// Calculates Mean Absolute Percentage Error.
    /// </summary>
    private float CalculateMAPE(float[][] predictions, float[][] actuals)
    {
        float totalPercentageError = 0;
        int count = 0;

        for (int i = 0; i < predictions.Length; i++)
        {
            for (int j = 0; j < predictions[i].Length; j++)
            {
                if (Math.Abs(actuals[i][j]) > 0.01f) // Avoid division by zero
                {
                    totalPercentageError += Math.Abs((actuals[i][j] - predictions[i][j]) / actuals[i][j]);
                    count++;
                }
            }
        }

        return count > 0 ? (totalPercentageError / count) * 100f : 0;
    }

    /// <summary>
    /// Calculates Root Mean Squared Error.
    /// </summary>
    private float CalculateRMSE(float[][] predictions, float[][] actuals)
    {
        float totalSquaredError = 0;
        int count = 0;

        for (int i = 0; i < predictions.Length; i++)
        {
            for (int j = 0; j < predictions[i].Length; j++)
            {
                var error = predictions[i][j] - actuals[i][j];
                totalSquaredError += error * error;
                count++;
            }
        }

        return count > 0 ? (float)Math.Sqrt(totalSquaredError / count) : 0;
    }

    /// <summary>
    /// Calculates R-squared (coefficient of determination).
    /// </summary>
    private float CalculateRSquared(float[][] predictions, float[][] actuals)
    {
        // Flatten arrays
        var allPredictions = predictions.SelectMany(p => p).ToArray();
        var allActuals = actuals.SelectMany(a => a).ToArray();

        if (allActuals.Length == 0) return 0;

        var mean = allActuals.Average();

        var ssTotal = allActuals.Sum(a => (a - mean) * (a - mean));
        var ssResidual = allPredictions.Zip(allActuals, (pred, actual) => (actual - pred) * (actual - pred)).Sum();

        if (ssTotal == 0) return 0;

        return 1 - (ssResidual / ssTotal);
    }

    /// <summary>
    /// Calculates Mean Forecast Error (bias).
    /// </summary>
    private float CalculateMeanForecastError(float[][] predictions, float[][] actuals)
    {
        float totalError = 0;
        int count = 0;

        for (int i = 0; i < predictions.Length; i++)
        {
            for (int j = 0; j < predictions[i].Length; j++)
            {
                totalError += predictions[i][j] - actuals[i][j]; // Signed error
                count++;
            }
        }

        return count > 0 ? totalError / count : 0;
    }

    /// <summary>
    /// Calculates standard deviation of forecast errors.
    /// </summary>
    private float CalculateForecastErrorStdDev(float[][] predictions, float[][] actuals, float meanError)
    {
        float sumSquaredDeviations = 0;
        int count = 0;

        for (int i = 0; i < predictions.Length; i++)
        {
            for (int j = 0; j < predictions[i].Length; j++)
            {
                var error = predictions[i][j] - actuals[i][j];
                var deviation = error - meanError;
                sumSquaredDeviations += deviation * deviation;
                count++;
            }
        }

        return count > 1 ? (float)Math.Sqrt(sumSquaredDeviations / (count - 1)) : 0;
    }

    /// <summary>
    /// Calculates confidence intervals for predictions based on historical error distribution.
    /// </summary>
    /// <param name="predictions">Point predictions</param>
    /// <param name="historicalErrors">Historical forecast errors for calibration</param>
    /// <param name="confidenceLevel">Confidence level (e.g., 0.95 for 95%)</param>
    /// <returns>Tuple of (lower bound, upper bound) arrays</returns>
    public Result<(float[] lowerBound, float[] upperBound)> CalculateConfidenceIntervals(
        float[] predictions,
        float[] historicalErrors,
        float confidenceLevel = 0.95f)
    {
        try
        {
            if (historicalErrors.Length < 10)
            {
                return Result.Fail<(float[], float[])>("Insufficient historical data for confidence intervals");
            }

            // Calculate standard deviation of historical errors
            var meanError = historicalErrors.Average();
            var variance = historicalErrors.Sum(e => (e - meanError) * (e - meanError)) / (historicalErrors.Length - 1);
            var stdDev = (float)Math.Sqrt(variance);

            // Z-score for confidence level (approximate)
            // 95% -> 1.96, 90% -> 1.645, 99% -> 2.576
            float zScore = confidenceLevel switch
            {
                >= 0.99f => 2.576f,
                >= 0.95f => 1.96f,
                >= 0.90f => 1.645f,
                _ => 1.96f
            };

            var margin = zScore * stdDev;

            var lowerBound = predictions.Select(p => p - margin).ToArray();
            var upperBound = predictions.Select(p => p + margin).ToArray();

            return Result.Ok((lowerBound, upperBound));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating confidence intervals");
            return Result.Fail<(float[], float[])>($"Confidence interval calculation failed: {ex.Message}");
        }
    }
}

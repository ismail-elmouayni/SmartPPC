using System;
using System.Collections.Generic;
using System.Linq;
using FluentResults;
using Microsoft.Extensions.Logging;
using SmartPPC.Core.ML.Domain;
using SmartPPC.Core.ML.Features;

namespace SmartPPC.Core.ML.Services;

/// <summary>
/// Implementation of feature engineering service for demand forecasting.
/// </summary>
public class ForecastFeatureEngineer : IForecastFeatureEngineer
{
    private readonly ILogger<ForecastFeatureEngineer> _logger;

    public ForecastFeatureEngineer(ILogger<ForecastFeatureEngineer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public Result<FeatureDataset> EngineerFeatures(
        IEnumerable<ForecastTrainingData> trainingData,
        int lookbackWindow,
        int forecastHorizon)
    {
        try
        {
            var dataList = trainingData.OrderBy(d => d.ObservationDate).ToList();

            if (dataList.Count < lookbackWindow + forecastHorizon)
            {
                return Result.Fail<FeatureDataset>(
                    $"Insufficient data: need at least {lookbackWindow + forecastHorizon} records, got {dataList.Count}");
            }

            var dataset = new FeatureDataset
            {
                LookbackWindow = lookbackWindow,
                ForecastHorizon = forecastHorizon
            };

            // Create samples using sliding window
            for (int i = 0; i <= dataList.Count - lookbackWindow - forecastHorizon; i++)
            {
                var sample = CreateSample(dataList, i, lookbackWindow, forecastHorizon);
                dataset.Samples.Add(sample);
            }

            // Set feature names
            dataset.FeatureNames = GenerateFeatureNames(lookbackWindow);

            _logger.LogInformation(
                "Engineered {SampleCount} samples with {FeatureCount} features each",
                dataset.SampleCount, dataset.FeatureCount);

            return Result.Ok(dataset);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error engineering features");
            return Result.Fail<FeatureDataset>($"Feature engineering failed: {ex.Message}");
        }
    }

    private FeatureSample CreateSample(
        List<ForecastTrainingData> data,
        int startIndex,
        int lookbackWindow,
        int forecastHorizon)
    {
        var lookbackData = data.Skip(startIndex).Take(lookbackWindow).ToList();
        var targetData = data.Skip(startIndex + lookbackWindow).Take(forecastHorizon).ToList();

        var features = new List<float>();

        // 1. Historical demand values (lookback window)
        features.AddRange(lookbackData.Select(d => (float)d.DemandValue));

        // 2. Lag features (key periods: 1, 7, 14 days)
        var demandHistory = lookbackData.Select(d => d.DemandValue).ToList();
        if (demandHistory.Count >= 14)
        {
            features.Add(demandHistory[^1]);  // Lag 1
            if (demandHistory.Count >= 7)
                features.Add(demandHistory[^7]);  // Lag 7
            if (demandHistory.Count >= 14)
                features.Add(demandHistory[^14]); // Lag 14
        }

        // 3. Rolling statistics (7-day and 14-day windows)
        if (demandHistory.Count >= 7)
        {
            var rolling7 = CalculateRollingStats(demandHistory.TakeLast(7));
            features.AddRange(rolling7.ToArray());
        }
        if (demandHistory.Count >= 14)
        {
            var rolling14 = CalculateRollingStats(demandHistory.TakeLast(14));
            features.AddRange(rolling14.ToArray());
        }

        // 4. Temporal features from the forecast start date
        var forecastStartDate = targetData.First().ObservationDate;
        var temporalFeatures = ExtractTemporalFeatures(forecastStartDate);
        features.AddRange(temporalFeatures.ToArray());

        // 5. Trend indicator
        var trend = CalculateTrend(demandHistory);
        features.Add(trend);

        // Target values
        var targets = targetData.Select(d => (float)d.DemandValue).ToArray();

        return new FeatureSample
        {
            Features = features.ToArray(),
            Targets = targets,
            Timestamp = forecastStartDate,
            StationId = lookbackData.First().StationDeclarationId
        };
    }

    private List<string> GenerateFeatureNames(int lookbackWindow)
    {
        var names = new List<string>();

        // Historical demand
        for (int i = 0; i < lookbackWindow; i++)
        {
            names.Add($"demand_t-{lookbackWindow - i}");
        }

        // Lag features
        names.AddRange(new[] { "lag_1", "lag_7", "lag_14" });

        // Rolling statistics (7-day)
        names.AddRange(new[] { "rolling7_mean", "rolling7_std", "rolling7_min", "rolling7_max", "rolling7_cv" });

        // Rolling statistics (14-day)
        names.AddRange(new[] { "rolling14_mean", "rolling14_std", "rolling14_min", "rolling14_max", "rolling14_cv" });

        // Temporal features
        names.AddRange(new[]
        {
            "day_of_week", "day_of_month", "month", "quarter", "week_of_year",
            "is_weekend", "day_sin", "day_cos", "month_sin", "month_cos"
        });

        // Trend
        names.Add("trend");

        return names;
    }

    /// <inheritdoc/>
    public Result<NormalizedDataset> NormalizeFeatures(FeatureDataset dataset)
    {
        try
        {
            var scalingParams = new ScalingParameters
            {
                ScalingType = ScalingType.MinMax
            };

            var featureCount = dataset.FeatureCount;
            scalingParams.MinValues = new float[featureCount];
            scalingParams.MaxValues = new float[featureCount];

            // Calculate min/max for each feature
            for (int f = 0; f < featureCount; f++)
            {
                var values = dataset.Samples.Select(s => s.Features[f]).ToArray();
                scalingParams.MinValues[f] = values.Min();
                scalingParams.MaxValues[f] = values.Max();
            }

            // Normalize features
            var normalizedDataset = new FeatureDataset
            {
                FeatureNames = dataset.FeatureNames,
                LookbackWindow = dataset.LookbackWindow,
                ForecastHorizon = dataset.ForecastHorizon
            };

            foreach (var sample in dataset.Samples)
            {
                var normalizedFeatures = new float[featureCount];
                for (int f = 0; f < featureCount; f++)
                {
                    var range = scalingParams.MaxValues[f] - scalingParams.MinValues[f];
                    if (range > 0)
                    {
                        normalizedFeatures[f] = (sample.Features[f] - scalingParams.MinValues[f]) / range;
                    }
                    else
                    {
                        normalizedFeatures[f] = 0.5f; // Default for constant features
                    }
                }

                normalizedDataset.Samples.Add(new FeatureSample
                {
                    Features = normalizedFeatures,
                    Targets = sample.Targets, // Targets normalized separately if needed
                    Timestamp = sample.Timestamp,
                    StationId = sample.StationId
                });
            }

            // Calculate target scaling parameters
            var allTargets = dataset.Samples.SelectMany(s => s.Targets).ToArray();
            scalingParams.TargetScaling = new TargetScaling
            {
                Min = allTargets.Min(),
                Max = allTargets.Max(),
                Mean = allTargets.Average(),
                StdDev = CalculateStdDev(allTargets)
            };

            _logger.LogInformation("Normalized {Count} samples", normalizedDataset.SampleCount);

            return Result.Ok(new NormalizedDataset
            {
                Dataset = normalizedDataset,
                ScalingParams = scalingParams
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error normalizing features");
            return Result.Fail<NormalizedDataset>($"Normalization failed: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public Result<(FeatureDataset training, FeatureDataset validation)> SplitDataset(
        FeatureDataset dataset,
        float validationSplit,
        bool useTimeSeriesSplit = true)
    {
        try
        {
            var splitIndex = (int)(dataset.SampleCount * (1 - validationSplit));

            IEnumerable<FeatureSample> trainingSamples;
            IEnumerable<FeatureSample> validationSamples;

            if (useTimeSeriesSplit)
            {
                // Time-based split: earlier data for training, recent for validation
                trainingSamples = dataset.Samples.Take(splitIndex);
                validationSamples = dataset.Samples.Skip(splitIndex);
            }
            else
            {
                // Random split (not recommended for time series)
                var random = new Random(42);
                var shuffled = dataset.Samples.OrderBy(_ => random.Next()).ToList();
                trainingSamples = shuffled.Take(splitIndex);
                validationSamples = shuffled.Skip(splitIndex);
            }

            var trainingDataset = new FeatureDataset
            {
                Samples = trainingSamples.ToList(),
                FeatureNames = dataset.FeatureNames,
                LookbackWindow = dataset.LookbackWindow,
                ForecastHorizon = dataset.ForecastHorizon
            };

            var validationDataset = new FeatureDataset
            {
                Samples = validationSamples.ToList(),
                FeatureNames = dataset.FeatureNames,
                LookbackWindow = dataset.LookbackWindow,
                ForecastHorizon = dataset.ForecastHorizon
            };

            _logger.LogInformation(
                "Split dataset: {TrainCount} training, {ValCount} validation",
                trainingDataset.SampleCount, validationDataset.SampleCount);

            return Result.Ok((trainingDataset, validationDataset));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error splitting dataset");
            return Result.Fail<(FeatureDataset, FeatureDataset)>($"Dataset split failed: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public TemporalFeatures ExtractTemporalFeatures(DateTime date)
    {
        var dayOfWeek = (int)date.DayOfWeek;
        if (dayOfWeek == 0) dayOfWeek = 7; // Sunday = 7

        return new TemporalFeatures
        {
            DayOfWeek = dayOfWeek,
            DayOfMonth = date.Day,
            Month = date.Month,
            Quarter = (date.Month - 1) / 3 + 1,
            WeekOfYear = System.Globalization.ISOWeek.GetWeekOfYear(date),
            IsWeekend = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday,
            DayOfWeekSin = (float)Math.Sin(2 * Math.PI * dayOfWeek / 7.0),
            DayOfWeekCos = (float)Math.Cos(2 * Math.PI * dayOfWeek / 7.0),
            MonthSin = (float)Math.Sin(2 * Math.PI * date.Month / 12.0),
            MonthCos = (float)Math.Cos(2 * Math.PI * date.Month / 12.0)
        };
    }

    /// <inheritdoc/>
    public Result<float[]> CreateLagFeatures(IEnumerable<int> demandHistory, int[] lags)
    {
        try
        {
            var history = demandHistory.ToList();
            var lagFeatures = new List<float>();

            foreach (var lag in lags)
            {
                if (history.Count >= lag)
                {
                    lagFeatures.Add(history[^lag]);
                }
                else
                {
                    lagFeatures.Add(0); // Pad with zero if not enough history
                }
            }

            return Result.Ok(lagFeatures.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating lag features");
            return Result.Fail<float[]>($"Lag feature creation failed: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public Result<RollingStatistics[]> CreateRollingFeatures(IEnumerable<int> demandHistory, int[] windows)
    {
        try
        {
            var history = demandHistory.ToList();
            var rollingStats = new List<RollingStatistics>();

            foreach (var window in windows)
            {
                if (history.Count >= window)
                {
                    var windowData = history.TakeLast(window);
                    rollingStats.Add(CalculateRollingStats(windowData));
                }
            }

            return Result.Ok(rollingStats.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating rolling features");
            return Result.Fail<RollingStatistics[]>($"Rolling feature creation failed: {ex.Message}");
        }
    }

    private RollingStatistics CalculateRollingStats(IEnumerable<int> data)
    {
        var values = data.Select(v => (float)v).ToArray();
        var mean = values.Average();
        var stdDev = CalculateStdDev(values);

        return new RollingStatistics
        {
            WindowSize = values.Length,
            Mean = mean,
            StdDev = stdDev,
            Min = values.Min(),
            Max = values.Max()
        };
    }

    private float CalculateStdDev(float[] values)
    {
        if (values.Length <= 1) return 0;

        var mean = values.Average();
        var sumSquaredDiffs = values.Sum(v => (v - mean) * (v - mean));
        return (float)Math.Sqrt(sumSquaredDiffs / (values.Length - 1));
    }

    private float CalculateTrend(List<int> demandHistory)
    {
        if (demandHistory.Count < 2) return 0;

        // Simple linear trend using first and last values
        var first = demandHistory.Take(Math.Min(7, demandHistory.Count)).Average();
        var last = demandHistory.TakeLast(Math.Min(7, demandHistory.Count)).Average();

        return (float)((last - first) / first);
    }

    /// <inheritdoc/>
    public Result<(float[] trend, float trendStrength)> ExtractTrend(IEnumerable<int> demandHistory)
    {
        try
        {
            var history = demandHistory.ToList();
            if (history.Count < 2)
            {
                return Result.Ok((new float[history.Count], 0f));
            }

            // Simple linear regression for trend
            var n = history.Count;
            var x = Enumerable.Range(0, n).Select(i => (float)i).ToArray();
            var y = history.Select(v => (float)v).ToArray();

            var xMean = x.Average();
            var yMean = y.Average();

            var numerator = x.Zip(y, (xi, yi) => (xi - xMean) * (yi - yMean)).Sum();
            var denominator = x.Sum(xi => (xi - xMean) * (xi - xMean));

            var slope = denominator != 0 ? numerator / denominator : 0;
            var intercept = yMean - slope * xMean;

            var trend = x.Select(xi => slope * xi + intercept).ToArray();
            var trendStrength = Math.Abs(slope) / (yMean + 1e-6f); // Normalized by mean

            return Result.Ok((trend, trendStrength));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting trend");
            return Result.Fail<(float[], float)>($"Trend extraction failed: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public Result<(float[] seasonal, float seasonalityStrength)> ExtractSeasonality(
        IEnumerable<int> demandHistory,
        int seasonalPeriod)
    {
        try
        {
            var history = demandHistory.ToList();
            if (history.Count < seasonalPeriod * 2)
            {
                return Result.Ok((new float[history.Count], 0f));
            }

            // Calculate seasonal indices
            var seasonalIndices = new float[seasonalPeriod];
            for (int i = 0; i < seasonalPeriod; i++)
            {
                var seasonValues = history.Where((_, index) => index % seasonalPeriod == i).ToList();
                seasonalIndices[i] = seasonValues.Any() ? (float) seasonValues.Average() : 0;
            }

            // Normalize seasonal indices
            var mean = seasonalIndices.Average();
            if (mean > 0)
            {
                for (int i = 0; i < seasonalPeriod; i++)
                {
                    seasonalIndices[i] /= mean;
                }
            }

            // Extend to full history length
            var seasonal = new float[history.Count];
            for (int i = 0; i < history.Count; i++)
            {
                seasonal[i] = seasonalIndices[i % seasonalPeriod];
            }

            // Calculate seasonality strength
            var seasonalityStrength = CalculateStdDev(seasonalIndices);

            return Result.Ok((seasonal, seasonalityStrength));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting seasonality");
            return Result.Fail<(float[], float)>($"Seasonality extraction failed: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public Result<float[]> DenormalizeValues(float[] normalizedValues, ScalingParameters scalingParams)
    {
        try
        {
            if (scalingParams.TargetScaling == null)
            {
                return Result.Fail<float[]>("Target scaling parameters not available");
            }

            var targetScaling = scalingParams.TargetScaling;
            var denormalized = new float[normalizedValues.Length];

            for (int i = 0; i < normalizedValues.Length; i++)
            {
                switch (scalingParams.ScalingType)
                {
                    case ScalingType.MinMax:
                        denormalized[i] = normalizedValues[i] * (targetScaling.Max - targetScaling.Min) + targetScaling.Min;
                        break;
                    case ScalingType.StandardScaler:
                        denormalized[i] = normalizedValues[i] * targetScaling.StdDev + targetScaling.Mean;
                        break;
                    default:
                        denormalized[i] = normalizedValues[i];
                        break;
                }
            }

            return Result.Ok(denormalized);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error denormalizing values");
            return Result.Fail<float[]>($"Denormalization failed: {ex.Message}");
        }
    }
}

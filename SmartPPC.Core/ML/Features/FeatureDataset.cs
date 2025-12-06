using System;
using System.Collections.Generic;

namespace SmartPPC.Core.ML.Features;

/// <summary>
/// Represents a complete dataset with engineered features for ML training.
/// </summary>
public class FeatureDataset
{
    /// <summary>
    /// Collection of individual samples in the dataset.
    /// </summary>
    public List<FeatureSample> Samples { get; set; } = new ();

    /// <summary>
    /// Names of all features in the dataset.
    /// </summary>
    public List<string> FeatureNames { get; set; } = new ();

    /// <summary>
    /// Number of input features per sample.
    /// </summary>
    public int FeatureCount => FeatureNames.Count;

    /// <summary>
    /// Number of samples in the dataset.
    /// </summary>
    public int SampleCount => Samples.Count;

    /// <summary>
    /// Lookback window size used for feature creation.
    /// </summary>
    public int LookbackWindow { get; set; }

    /// <summary>
    /// Forecast horizon (number of periods to predict).
    /// </summary>
    public int ForecastHorizon { get; set; }
}

/// <summary>
/// Represents a single training sample with features and target values.
/// </summary>
public class FeatureSample
{
    /// <summary>
    /// Input features for this sample.
    /// </summary>
    public float[] Features { get; set; } = Array.Empty<float>();

    /// <summary>
    /// Target values (actual demand for forecast horizon periods).
    /// </summary>
    public float[] Targets { get; set; } = Array.Empty<float>();

    /// <summary>
    /// Timestamp associated with this sample.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Station ID this sample belongs to (optional, for tracking).
    /// </summary>
    public int? StationId { get; set; }
}

/// <summary>
/// Dataset with normalized features and scaling parameters.
/// </summary>
public class NormalizedDataset
{
    /// <summary>
    /// The feature dataset with normalized values.
    /// </summary>
    public FeatureDataset Dataset { get; set; } = new FeatureDataset();

    /// <summary>
    /// Scaling parameters used for normalization (needed for inverse transform).
    /// </summary>
    public ScalingParameters ScalingParams { get; set; } = new ScalingParameters();
}

/// <summary>
/// Parameters for feature scaling/normalization.
/// </summary>
public class ScalingParameters
{
    /// <summary>
    /// Type of scaling applied.
    /// </summary>
    public ScalingType ScalingType { get; set; } = ScalingType.MinMax;

    /// <summary>
    /// Minimum values for each feature (for MinMax scaling).
    /// </summary>
    public float[] MinValues { get; set; } = Array.Empty<float>();

    /// <summary>
    /// Maximum values for each feature (for MinMax scaling).
    /// </summary>
    public float[] MaxValues { get; set; } = Array.Empty<float>();

    /// <summary>
    /// Mean values for each feature (for Z-score standardization).
    /// </summary>
    public float[] MeanValues { get; set; } = Array.Empty<float>();

    /// <summary>
    /// Standard deviation for each feature (for Z-score standardization).
    /// </summary>
    public float[] StdDevValues { get; set; } = Array.Empty<float>();

    /// <summary>
    /// Target value scaling parameters (separate from feature scaling).
    /// </summary>
    public TargetScaling? TargetScaling { get; set; }
}

/// <summary>
/// Scaling parameters specifically for target values (demand).
/// </summary>
public class TargetScaling
{
    public float Min { get; set; }
    public float Max { get; set; }
    public float Mean { get; set; }
    public float StdDev { get; set; }

    /// <summary>
    /// Converts this TargetScaling to a ScalingParameters object.
    /// Useful when denormalization methods expect ScalingParameters format.
    /// </summary>
    /// <param name="scalingType">The type of scaling to use (MinMax or StandardScaler recommended).</param>
    /// <returns>A ScalingParameters object with single-element arrays.</returns>
    public ScalingParameters ToScalingParameters(ScalingType scalingType = ScalingType.MinMax)
    {
        return new ScalingParameters
        {
            ScalingType = scalingType,
            MinValues = new[] { Min },
            MaxValues = new[] { Max },
            MeanValues = new[] { Mean },
            StdDevValues = new[] { StdDev },
            TargetScaling = this
        };
    }
}

/// <summary>
/// Types of feature scaling.
/// </summary>
public enum ScalingType
{
    /// <summary>
    /// No scaling applied.
    /// </summary>
    None = 0,

    /// <summary>
    /// Min-Max scaling to [0, 1] range.
    /// </summary>
    MinMax = 1,

    /// <summary>
    /// Z-score standardization (mean=0, std=1).
    /// </summary>
    StandardScaler = 2,

    /// <summary>
    /// Robust scaling using median and IQR (resistant to outliers).
    /// </summary>
    RobustScaler = 3
}

/// <summary>
/// Temporal features extracted from date/time.
/// </summary>
public class TemporalFeatures
{
    /// <summary>
    /// Day of week (1-7, Monday=1).
    /// </summary>
    public int DayOfWeek { get; set; }

    /// <summary>
    /// Day of month (1-31).
    /// </summary>
    public int DayOfMonth { get; set; }

    /// <summary>
    /// Month (1-12).
    /// </summary>
    public int Month { get; set; }

    /// <summary>
    /// Quarter (1-4).
    /// </summary>
    public int Quarter { get; set; }

    /// <summary>
    /// Week of year (1-53).
    /// </summary>
    public int WeekOfYear { get; set; }

    /// <summary>
    /// Is weekend (Saturday or Sunday).
    /// </summary>
    public bool IsWeekend { get; set; }

    /// <summary>
    /// Sine-encoded day of week for cyclical nature.
    /// </summary>
    public float DayOfWeekSin { get; set; }

    /// <summary>
    /// Cosine-encoded day of week for cyclical nature.
    /// </summary>
    public float DayOfWeekCos { get; set; }

    /// <summary>
    /// Sine-encoded month for annual cyclical pattern.
    /// </summary>
    public float MonthSin { get; set; }

    /// <summary>
    /// Cosine-encoded month for annual cyclical pattern.
    /// </summary>
    public float MonthCos { get; set; }

    /// <summary>
    /// Converts to flat array for ML input.
    /// </summary>
    public float[] ToArray()
    {
        return new float[]
        {
            DayOfWeek,
            DayOfMonth,
            Month,
            Quarter,
            WeekOfYear,
            IsWeekend ? 1f : 0f,
            DayOfWeekSin,
            DayOfWeekCos,
            MonthSin,
            MonthCos
        };
    }
}

/// <summary>
/// Rolling window statistics for a given window size.
/// </summary>
public class RollingStatistics
{
    /// <summary>
    /// Window size used.
    /// </summary>
    public int WindowSize { get; set; }

    /// <summary>
    /// Moving average.
    /// </summary>
    public float Mean { get; set; }

    /// <summary>
    /// Standard deviation within window.
    /// </summary>
    public float StdDev { get; set; }

    /// <summary>
    /// Minimum value in window.
    /// </summary>
    public float Min { get; set; }

    /// <summary>
    /// Maximum value in window.
    /// </summary>
    public float Max { get; set; }

    /// <summary>
    /// Coefficient of variation (StdDev / Mean).
    /// </summary>
    public float CoefficientOfVariation => Mean != 0 ? StdDev / Mean : 0;

    /// <summary>
    /// Converts to flat array for ML input.
    /// </summary>
    public float[] ToArray()
    {
        return new float[] { Mean, StdDev, Min, Max, CoefficientOfVariation };
    }
}

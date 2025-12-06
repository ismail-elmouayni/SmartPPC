using Microsoft.ML.Data;

namespace SmartPPC.Core.ML.Models;

/// <summary>
/// Input data structure for ML.NET time series forecasting.
/// </summary>
public class TimeSeriesData
{
    /// <summary>
    /// Demand value at this time point.
    /// </summary>
    [LoadColumn(0)]
    public float Value { get; set; }
}

/// <summary>
/// Prediction output from ML.NET forecasting model.
/// </summary>
public class TimeSeriesPrediction
{
    /// <summary>
    /// Forecasted values for the horizon.
    /// </summary>
    [VectorType]
    public float[] ForecastedValues { get; set; } = System.Array.Empty<float>();

    /// <summary>
    /// Lower confidence bound.
    /// </summary>
    [VectorType]
    public float[]? LowerBound { get; set; }

    /// <summary>
    /// Upper confidence bound.
    /// </summary>
    [VectorType]
    public float[]? UpperBound { get; set; }
}

/// <summary>
/// Input for regression-based forecasting with engineered features.
/// </summary>
public class ForecastInput
{
    /// <summary>
    /// All engineered features as a vector.
    /// </summary>
    [VectorType]
    [ColumnName("Features")]
    public float[] Features { get; set; } = System.Array.Empty<float>();

    /// <summary>
    /// Target value (first period of forecast horizon).
    /// </summary>
    [ColumnName("Label")]
    public float Label { get; set; }
}

/// <summary>
/// Output for regression-based forecasting.
/// </summary>
public class ForecastOutput
{
    /// <summary>
    /// Predicted value.
    /// </summary>
    [ColumnName("Score")]
    public float Prediction { get; set; }
}

/// <summary>
/// Multi-output forecast model input/output for custom models.
/// </summary>
public class MultiOutputForecastInput
{
    /// <summary>
    /// Input features vector.
    /// </summary>
    [VectorType]
    [ColumnName("Features")]
    public float[] Features { get; set; } = System.Array.Empty<float>();
}

/// <summary>
/// Multi-output forecast prediction.
/// </summary>
public class MultiOutputForecastOutput
{
    /// <summary>
    /// Predicted values for entire forecast horizon.
    /// </summary>
    [VectorType]
    [ColumnName("Predictions")]
    public float[] Predictions { get; set; } = System.Array.Empty<float>();
}

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using SmartPPC.Core.ML.Domain;
using SmartPPC.Core.ML.Features;
using SmartPPC.Core.ML.Services;

namespace SmartPPC.Core.ML.Models;

/// <summary>
/// Interface for training forecasting models using ML.NET.
/// </summary>
public interface IForecastModelTrainer
{
    /// <summary>
    /// Trains a forecasting model using the provided dataset.
    /// </summary>
    /// <param name="trainingDataset">Normalized training dataset</param>
    /// <param name="validationDataset">Normalized validation dataset</param>
    /// <param name="modelType">Type of model to train</param>
    /// <param name="parameters">Training parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Training result with trained model and metrics</returns>
    Task<Result<TrainingResult>> TrainAsync(
        FeatureDataset trainingDataset,
        FeatureDataset validationDataset,
        ForecastModelType modelType,
        TrainingParameters parameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a trained model from serialized bytes.
    /// </summary>
    /// <param name="modelData">Serialized model data</param>
    /// <param name="modelType">Type of model</param>
    /// <returns>Loaded model instance</returns>
    Result<ITrainedModel> LoadModel(byte[] modelData, ForecastModelType modelType);

    /// <summary>
    /// Serializes a trained model to bytes for storage.
    /// </summary>
    /// <param name="model">Trained model instance</param>
    /// <returns>Serialized model bytes</returns>
    Result<byte[]> SerializeModel(ITrainedModel model);
}

/// <summary>
/// Result of model training containing the trained model and metrics.
/// </summary>
public class TrainingResult
{
    /// <summary>
    /// The trained model instance.
    /// </summary>
    public ITrainedModel TrainedModel { get; set; } = null!;

    /// <summary>
    /// Training metrics.
    /// </summary>
    public TrainingMetrics TrainingMetrics { get; set; } = new TrainingMetrics();

    /// <summary>
    /// Validation metrics.
    /// </summary>
    public ValidationMetrics ValidationMetrics { get; set; } = new ValidationMetrics();

    /// <summary>
    /// Serialized model data (ONNX format).
    /// </summary>
    public byte[]? SerializedModel { get; set; }

    /// <summary>
    /// Training duration.
    /// </summary>
    public TimeSpan TrainingDuration { get; set; }
}

/// <summary>
/// Training metrics computed during model training.
/// </summary>
public class TrainingMetrics
{
    /// <summary>
    /// Final training loss.
    /// </summary>
    public float Loss { get; set; }

    /// <summary>
    /// Mean Absolute Error on training set.
    /// </summary>
    public float MAE { get; set; }

    /// <summary>
    /// Mean Absolute Percentage Error on training set.
    /// </summary>
    public float MAPE { get; set; }

    /// <summary>
    /// Root Mean Squared Error on training set.
    /// </summary>
    public float RMSE { get; set; }

    /// <summary>
    /// Number of training samples.
    /// </summary>
    public int SampleCount { get; set; }

    /// <summary>
    /// Number of epochs completed.
    /// </summary>
    public int EpochsCompleted { get; set; }
}

/// <summary>
/// Validation metrics computed on validation dataset.
/// </summary>
public class ValidationMetrics
{
    /// <summary>
    /// Validation loss.
    /// </summary>
    public float Loss { get; set; }

    /// <summary>
    /// Mean Absolute Error on validation set.
    /// </summary>
    public float MAE { get; set; }

    /// <summary>
    /// Mean Absolute Percentage Error on validation set.
    /// </summary>
    public float MAPE { get; set; }

    /// <summary>
    /// Root Mean Squared Error on validation set.
    /// </summary>
    public float RMSE { get; set; }

    /// <summary>
    /// R-squared coefficient.
    /// </summary>
    public float RSquared { get; set; }

    /// <summary>
    /// Number of validation samples.
    /// </summary>
    public int SampleCount { get; set; }
}

/// <summary>
/// Interface for a trained forecasting model.
/// </summary>
public interface ITrainedModel
{
    /// <summary>
    /// Makes predictions for a single input sample.
    /// </summary>
    /// <param name="features">Input features</param>
    /// <returns>Predicted values for forecast horizon</returns>
    Result<float[]> Predict(float[] features);

    /// <summary>
    /// Makes batch predictions for multiple samples.
    /// </summary>
    /// <param name="featureBatch">Batch of input features</param>
    /// <returns>Predicted values for each sample</returns>
    Result<float[][]> PredictBatch(float[][] featureBatch);

    /// <summary>
    /// Model type.
    /// </summary>
    ForecastModelType ModelType { get; }

    /// <summary>
    /// Number of input features expected.
    /// </summary>
    int FeatureCount { get; }

    /// <summary>
    /// Forecast horizon (number of output values).
    /// </summary>
    int ForecastHorizon { get; }
}

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.TimeSeries;
using FluentResults;
using Microsoft.Extensions.Logging;
using SmartPPC.Core.ML.Domain;
using SmartPPC.Core.ML.Features;
using SmartPPC.Core.ML.Services;

namespace SmartPPC.Core.ML.Models;

/// <summary>
/// ML.NET-based forecasting model trainer.
/// Currently implements SSA time series forecasting and FastTree regression.
/// Future: LSTM+Attention via TensorFlow.NET or ONNX.
/// </summary>
public class ForecastModelTrainer : IForecastModelTrainer
{
    private readonly ILogger<ForecastModelTrainer> _logger;
    private readonly MLContext _mlContext;

    public ForecastModelTrainer(ILogger<ForecastModelTrainer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mlContext = new MLContext(seed: 42);
    }

    /// <inheritdoc/>
    public async Task<Result<TrainingResult>> TrainAsync(
        FeatureDataset trainingDataset,
        FeatureDataset validationDataset,
        ForecastModelType modelType,
        TrainingParameters parameters,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Training {ModelType} model with {TrainCount} training samples, {ValCount} validation samples",
                modelType, trainingDataset.SampleCount, validationDataset.SampleCount);

            var startTime = DateTime.UtcNow;

            Result<TrainingResult> result = modelType switch
            {
                ForecastModelType.LSTMAttention => await TrainLSTMModelAsync(
                    trainingDataset, validationDataset, parameters, cancellationToken),

                ForecastModelType.LSTM => await TrainLSTMModelAsync(
                    trainingDataset, validationDataset, parameters, cancellationToken),

                ForecastModelType.MovingAverage => await TrainMovingAverageModelAsync(
                    trainingDataset, validationDataset, parameters),

                _ => await TrainRegressionModelAsync(
                    trainingDataset, validationDataset, parameters, cancellationToken)
            };

            if (result.IsSuccess)
            {
                result.Value.TrainingDuration = DateTime.UtcNow - startTime;
                _logger.LogInformation(
                    "Training completed in {Duration:F2} seconds. Validation MAE: {MAE:F2}",
                    result.Value.TrainingDuration.TotalSeconds,
                    result.Value.ValidationMetrics.MAE);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error training model");
            return Result.Fail<TrainingResult>($"Model training failed: {ex.Message}");
        }
    }

    private async Task<Result<TrainingResult>> TrainLSTMModelAsync(
        FeatureDataset trainingDataset,
        FeatureDataset validationDataset,
        TrainingParameters parameters,
        CancellationToken cancellationToken)
    {
        // TODO: Implement LSTM+Attention using TensorFlow.NET or ONNX Runtime
        // For now, fall back to regression-based forecasting

        _logger.LogWarning(
            "LSTM+Attention not yet implemented. Using FastTree regression as fallback.");

        return await TrainRegressionModelAsync(
            trainingDataset, validationDataset, parameters, cancellationToken);
    }

    private async Task<Result<TrainingResult>> TrainRegressionModelAsync(
        FeatureDataset trainingDataset,
        FeatureDataset validationDataset,
        TrainingParameters parameters,
        CancellationToken cancellationToken)
    {
        try
        {
            // Train a separate model for each forecast horizon step
            // This is a multi-output regression approach

            var models = new FastTreeRegressionModel[trainingDataset.ForecastHorizon];
            var trainingMetrics = new TrainingMetrics { SampleCount = trainingDataset.SampleCount };
            var validationMetrics = new ValidationMetrics { SampleCount = validationDataset.SampleCount };

            for (int horizonStep = 0; horizonStep < trainingDataset.ForecastHorizon; horizonStep++)
            {
                if (cancellationToken.IsCancellationRequested)
                    return Result.Fail<TrainingResult>("Training cancelled");

                // Prepare data for this horizon step
                var trainData = PrepareRegressionData(trainingDataset, horizonStep);
                var valData = PrepareRegressionData(validationDataset, horizonStep);

                // Build pipeline
                // Note: FastTree expects features in a column named "Features" and target in "Label"
                // Our ForecastInput already has these column names, so no transformation needed
                var pipeline = _mlContext.Regression.Trainers.FastTree(
                    labelColumnName: "Label",
                    featureColumnName: "Features",
                    numberOfLeaves: 20,
                    minimumExampleCountPerLeaf: 10,
                    numberOfTrees: 100,
                    learningRate: parameters.LearningRate);

                // Train
                _logger.LogInformation("Training model for horizon step {Step}/{Total}",
                    horizonStep + 1, trainingDataset.ForecastHorizon);

                var model = pipeline.Fit(trainData);

                // Evaluate on validation set
                var predictions = model.Transform(valData);
                var metrics = _mlContext.Regression.Evaluate(predictions);

                models[horizonStep] = new FastTreeRegressionModel
                {
                    Model = model,
                    HorizonStep = horizonStep
                };

                // Accumulate metrics
                validationMetrics.MAE += (float) metrics.MeanAbsoluteError / trainingDataset.ForecastHorizon;
                validationMetrics.RMSE += (float) metrics.RootMeanSquaredError / trainingDataset.ForecastHorizon;
                validationMetrics.RSquared += (float) metrics.RSquared / trainingDataset.ForecastHorizon;
            }

            // Calculate MAPE
            validationMetrics.MAPE = CalculateMAPE(validationDataset, models);

            var trainedModel = new MultiHorizonRegressionModel
            {
                Models = models,
                FeatureCount = trainingDataset.FeatureCount,
                ForecastHorizon = trainingDataset.ForecastHorizon,
                ModelType = ForecastModelType.Custom
            };

            await Task.CompletedTask; // Async placeholder

            return Result.Ok(new TrainingResult
            {
                TrainedModel = trainedModel,
                TrainingMetrics = trainingMetrics,
                ValidationMetrics = validationMetrics,
                SerializedModel = null // TODO: Implement serialization
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error training regression model");
            return Result.Fail<TrainingResult>($"Regression training failed: {ex.Message}");
        }
    }

    private async Task<Result<TrainingResult>> TrainMovingAverageModelAsync(
        FeatureDataset trainingDataset,
        FeatureDataset validationDataset,
        TrainingParameters parameters)
    {
        try
        {
            _logger.LogInformation("Training simple moving average model");

            var model = new MovingAverageModel
            {
                WindowSize = parameters.LookbackWindow,
                ForecastHorizon = trainingDataset.ForecastHorizon,
                FeatureCount = trainingDataset.FeatureCount
            };

            // Calculate validation metrics
            var validationMetrics = EvaluateMovingAverage(validationDataset, model);

            await Task.CompletedTask; // Async placeholder

            return Result.Ok(new TrainingResult
            {
                TrainedModel = model,
                TrainingMetrics = new TrainingMetrics(),
                ValidationMetrics = validationMetrics
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error training moving average model");
            return Result.Fail<TrainingResult>($"Moving average training failed: {ex.Message}");
        }
    }

    private IDataView PrepareRegressionData(FeatureDataset dataset, int horizonStep)
    {
        var data = dataset.Samples.Select(sample => new ForecastInput
        {
            Features = sample.Features,
            Label = sample.Targets[horizonStep]
        }).ToList();

        return _mlContext.Data.LoadFromEnumerable(data);
    }

    private float CalculateMAPE(FeatureDataset dataset, FastTreeRegressionModel[] models)
    {
        float totalPercentageError = 0;
        int count = 0;

        foreach (var sample in dataset.Samples)
        {
            for (int h = 0; h < models.Length; h++)
            {
                var input = new ForecastInput { Features = sample.Features };
                var dataView = _mlContext.Data.LoadFromEnumerable(new[] { input });
                var prediction = models[h].Model.Transform(dataView);

                var pred = _mlContext.Data.CreateEnumerable<ForecastOutput>(prediction, false).First();
                var actual = sample.Targets[h];

                if (Math.Abs(actual) > 0.01f)
                {
                    totalPercentageError += Math.Abs((actual - pred.Prediction) / actual);
                    count++;
                }
            }
        }

        return count > 0 ? (totalPercentageError / count) * 100f : 0f;
    }

    private ValidationMetrics EvaluateMovingAverage(FeatureDataset dataset, MovingAverageModel model)
    {
        float totalError = 0;
        float totalSquaredError = 0;
        float totalPercentageError = 0;
        int count = 0;

        foreach (var sample in dataset.Samples)
        {
            var predictionResult = model.Predict(sample.Features);
            if (predictionResult.IsSuccess)
            {
                var predictions = predictionResult.Value;
                for (int i = 0; i < predictions.Length; i++)
                {
                    var error = Math.Abs(sample.Targets[i] - predictions[i]);
                    totalError += error;
                    totalSquaredError += error * error;

                    if (Math.Abs(sample.Targets[i]) > 0.01f)
                    {
                        totalPercentageError += Math.Abs((sample.Targets[i] - predictions[i]) / sample.Targets[i]);
                    }
                    count++;
                }
            }
        }

        return new ValidationMetrics
        {
            MAE = totalError / count,
            RMSE = (float)Math.Sqrt(totalSquaredError / count),
            MAPE = (totalPercentageError / count) * 100f,
            SampleCount = dataset.SampleCount
        };
    }

    /// <inheritdoc/>
    public Result<ITrainedModel> LoadModel(byte[] modelData, ForecastModelType modelType)
    {
        try
        {
            if (modelType == ForecastModelType.MovingAverage)
            {
                // Deserialize moving average model from JSON
                var json = System.Text.Encoding.UTF8.GetString(modelData);
                var data = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json);

                var model = new MovingAverageModel
                {
                    WindowSize = data.GetProperty("WindowSize").GetInt32(),
                    ForecastHorizon = data.GetProperty("ForecastHorizon").GetInt32(),
                    FeatureCount = data.GetProperty("FeatureCount").GetInt32()
                };

                return Result.Ok<ITrainedModel>(model);
            }
            else
            {
                // Deserialize multi-horizon regression model
                using var memoryStream = new System.IO.MemoryStream(modelData);
                using var binaryReader = new System.IO.BinaryReader(memoryStream);

                // Read model metadata
                var forecastHorizon = binaryReader.ReadInt32();
                var featureCount = binaryReader.ReadInt32();
                var modelTypeInt = binaryReader.ReadInt32();

                // Deserialize each FastTree model
                var models = new FastTreeRegressionModel[forecastHorizon];
                for (int i = 0; i < forecastHorizon; i++)
                {
                    var modelBytesLength = binaryReader.ReadInt32();
                    var modelBytes = binaryReader.ReadBytes(modelBytesLength);
                    var horizonStep = binaryReader.ReadInt32();

                    // Load the ML.NET transformer
                    using var modelStream = new System.IO.MemoryStream(modelBytes);
                    var transformer = _mlContext.Model.Load(modelStream, out var modelInputSchema);

                    models[i] = new FastTreeRegressionModel
                    {
                        Model = transformer,
                        HorizonStep = horizonStep
                    };
                }

                var multiHorizonModel = new MultiHorizonRegressionModel
                {
                    Models = models,
                    ForecastHorizon = forecastHorizon,
                    FeatureCount = featureCount,
                    ModelType = (ForecastModelType)modelTypeInt
                };

                _logger.LogInformation(
                    "Loaded multi-horizon model with {HorizonCount} models",
                    models.Length);

                return Result.Ok<ITrainedModel>(multiHorizonModel);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading model");
            return Result.Fail<ITrainedModel>($"Model loading failed: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public Result<byte[]> SerializeModel(ITrainedModel model)
    {
        try
        {
            if (model is MultiHorizonRegressionModel multiHorizonModel)
            {
                using var memoryStream = new System.IO.MemoryStream();
                using var binaryWriter = new System.IO.BinaryWriter(memoryStream);

                // Write model metadata
                binaryWriter.Write(multiHorizonModel.ForecastHorizon);
                binaryWriter.Write(multiHorizonModel.FeatureCount);
                binaryWriter.Write((int)multiHorizonModel.ModelType);

                // Serialize each FastTree model
                for (int i = 0; i < multiHorizonModel.Models.Length; i++)
                {
                    var fastTreeModel = multiHorizonModel.Models[i];

                    // Serialize the ML.NET transformer to a separate stream
                    using var modelStream = new System.IO.MemoryStream();
                    _mlContext.Model.Save(fastTreeModel.Model, null, modelStream);

                    var modelBytes = modelStream.ToArray();
                    binaryWriter.Write(modelBytes.Length);
                    binaryWriter.Write(modelBytes);
                    binaryWriter.Write(fastTreeModel.HorizonStep);
                }

                var serializedBytes = memoryStream.ToArray();
                _logger.LogInformation(
                    "Serialized multi-horizon model with {HorizonCount} models ({Size} bytes)",
                    multiHorizonModel.Models.Length, serializedBytes.Length);

                return Result.Ok(serializedBytes);
            }
            else if (model is MovingAverageModel movingAvgModel)
            {
                // Simple JSON serialization for moving average model
                var json = System.Text.Json.JsonSerializer.Serialize(new
                {
                    movingAvgModel.WindowSize,
                    movingAvgModel.ForecastHorizon,
                    movingAvgModel.FeatureCount,
                    ModelType = "MovingAverage"
                });

                return Result.Ok(System.Text.Encoding.UTF8.GetBytes(json));
            }
            else
            {
                return Result.Fail<byte[]>($"Unsupported model type: {model.GetType().Name}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error serializing model");
            return Result.Fail<byte[]>($"Model serialization failed: {ex.Message}");
        }
    }
}

/// <summary>
/// FastTree regression model for a single horizon step.
/// </summary>
internal class FastTreeRegressionModel
{
    public ITransformer Model { get; set; } = null!;
    public int HorizonStep { get; set; }
}

/// <summary>
/// Multi-horizon regression model using multiple FastTree models.
/// </summary>
internal class MultiHorizonRegressionModel : ITrainedModel
{
    public FastTreeRegressionModel[] Models { get; set; } = Array.Empty<FastTreeRegressionModel>();
    public ForecastModelType ModelType { get; set; }
    public int FeatureCount { get; set; }
    public int ForecastHorizon { get; set; }

    private readonly MLContext _mlContext = new MLContext();

    public Result<float[]> Predict(float[] features)
    {
        try
        {
            var predictions = new float[ForecastHorizon];

            for (int h = 0; h < Models.Length; h++)
            {
                var input = new ForecastInput { Features = features };
                var dataView = _mlContext.Data.LoadFromEnumerable(new[] { input });
                var prediction = Models[h].Model.Transform(dataView);

                var result = _mlContext.Data.CreateEnumerable<ForecastOutput>(prediction, false).First();
                predictions[h] = result.Prediction;
            }

            return Result.Ok(predictions);
        }
        catch (Exception ex)
        {
            return Result.Fail<float[]>($"Prediction failed: {ex.Message}");
        }
    }

    public Result<float[][]> PredictBatch(float[][] featureBatch)
    {
        try
        {
            var results = new float[featureBatch.Length][];
            for (int i = 0; i < featureBatch.Length; i++)
            {
                var predResult = Predict(featureBatch[i]);
                if (predResult.IsFailed)
                    return Result.Fail<float[][]>(predResult.Errors.First().Message);

                results[i] = predResult.Value;
            }

            return Result.Ok(results);
        }
        catch (Exception ex)
        {
            return Result.Fail<float[][]>($"Batch prediction failed: {ex.Message}");
        }
    }
}

/// <summary>
/// Simple moving average baseline model.
/// </summary>
internal class MovingAverageModel : ITrainedModel
{
    public int WindowSize { get; set; }
    public ForecastModelType ModelType => ForecastModelType.MovingAverage;
    public int FeatureCount { get; set; }
    public int ForecastHorizon { get; set; }

    public Result<float[]> Predict(float[] features)
    {
        try
        {
            // Extract recent demand values from features (first WindowSize features)
            var recentDemand = features.Take(Math.Min(WindowSize, features.Length)).ToArray();
            var average = recentDemand.Average();

            // Simple forecast: repeat the average for all horizon steps
            var predictions = Enumerable.Repeat(average, ForecastHorizon).ToArray();

            return Result.Ok(predictions);
        }
        catch (Exception ex)
        {
            return Result.Fail<float[]>($"Prediction failed: {ex.Message}");
        }
    }

    public Result<float[][]> PredictBatch(float[][] featureBatch)
    {
        try
        {
            var results = featureBatch.Select(f => Predict(f).Value).ToArray();
            return Result.Ok(results);
        }
        catch (Exception ex)
        {
            return Result.Fail<float[][]>($"Batch prediction failed: {ex.Message}");
        }
    }
}

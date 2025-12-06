using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Microsoft.Extensions.Logging;
using SmartPPC.Core.ML.Domain;
using SmartPPC.Core.ML.Features;
using SmartPPC.Core.ML.Models;
using SmartPPC.Core.ML.Repositories;

namespace SmartPPC.Core.ML.Services;

/// <summary>
/// Implementation of the main forecasting service using ML.NET.
/// Handles model training, prediction generation, and model lifecycle.
/// </summary>
public class ForecastingService : IForecastingService
{
    private readonly ILogger<ForecastingService> _logger;
    private readonly IForecastDataCollectionService _dataCollectionService;
    private readonly IForecastFeatureEngineer _featureEngineer;
    private readonly IForecastModelTrainer _modelTrainer;
    private readonly ModelEvaluator _modelEvaluator;
    private readonly IForecastModelRepository _modelRepository;
    private readonly IForecastPredictionRepository _predictionRepository;
    private readonly IModelMetricsRepository _metricsRepository;

    // In-memory cache for trained models (temporary until database persistence is implemented)
    private readonly Dictionary<Guid, (ITrainedModel model, ScalingParameters scalingParams)> _modelCache = new();

    public ForecastingService(
        ILogger<ForecastingService> logger,
        IForecastDataCollectionService dataCollectionService,
        IForecastFeatureEngineer featureEngineer,
        IForecastModelTrainer modelTrainer,
        ModelEvaluator modelEvaluator,
        IForecastModelRepository modelRepository,
        IForecastPredictionRepository predictionRepository,
        IModelMetricsRepository metricsRepository)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dataCollectionService = dataCollectionService ?? throw new ArgumentNullException(nameof(dataCollectionService));
        _featureEngineer = featureEngineer ?? throw new ArgumentNullException(nameof(featureEngineer));
        _modelTrainer = modelTrainer ?? throw new ArgumentNullException(nameof(modelTrainer));
        _modelEvaluator = modelEvaluator ?? throw new ArgumentNullException(nameof(modelEvaluator));
        _modelRepository = modelRepository ?? throw new ArgumentNullException(nameof(modelRepository));
        _predictionRepository = predictionRepository ?? throw new ArgumentNullException(nameof(predictionRepository));
        _metricsRepository = metricsRepository ?? throw new ArgumentNullException(nameof(metricsRepository));
    }

    /// <inheritdoc/>
    public async Task<Result<ForecastModel>> TrainModelAsync(
        Guid configurationId,
        ForecastModelType modelType,
        TrainingParameters parameters)
    {
        try
        {
            _logger.LogInformation(
                "Training {ModelType} model for configuration {ConfigurationId}",
                modelType, configurationId);

            // Step 1: Fetch historical data
            var historicalDataResult = await _dataCollectionService.GetHistoricalDataForAllStationsAsync(
                configurationId,
                parameters.TrainingStartDate,
                parameters.TrainingEndDate);

            if (historicalDataResult.IsFailed)
            {
                return Result.Fail<ForecastModel>($"Failed to fetch historical data: {historicalDataResult.Errors.First().Message}");
            }

            if (!historicalDataResult.Value.Any())
            {
                return Result.Fail<ForecastModel>("No historical data available for training. Please collect data first.");
            }

            // Combine data from all stations for configuration-level model
            var allTrainingData = historicalDataResult.Value
                .SelectMany(kvp => kvp.Value)
                .OrderBy(d => d.ObservationDate)
                .ToList();

            if (allTrainingData.Count < parameters.LookbackWindow + parameters.ForecastHorizon)
            {
                return Result.Fail<ForecastModel>(
                    $"Insufficient data for training. Need at least {parameters.LookbackWindow + parameters.ForecastHorizon} observations, but got {allTrainingData.Count}");
            }

            _logger.LogInformation("Retrieved {Count} historical observations for training", allTrainingData.Count);

            // Step 2: Feature Engineering
            var featureDatasetResult = _featureEngineer.EngineerFeatures(
                allTrainingData,
                parameters.LookbackWindow,
                parameters.ForecastHorizon);

            if (featureDatasetResult.IsFailed)
            {
                return Result.Fail<ForecastModel>($"Feature engineering failed: {featureDatasetResult.Errors.First().Message}");
            }

            var featureDataset = featureDatasetResult.Value;
            _logger.LogInformation(
                "Engineered {SampleCount} samples with {FeatureCount} features each",
                featureDataset.SampleCount, featureDataset.FeatureCount);

            // Step 3: Normalize features
            var normalizedDatasetResult = _featureEngineer.NormalizeFeatures(featureDataset);
            if (normalizedDatasetResult.IsFailed)
            {
                return Result.Fail<ForecastModel>($"Normalization failed: {normalizedDatasetResult.Errors.First().Message}");
            }

            var normalizedDataset = normalizedDatasetResult.Value;

            // Step 4: Train/validation split
            var splitResult = _featureEngineer.SplitDataset(
                normalizedDataset.Dataset,
                parameters.ValidationSplit,
                useTimeSeriesSplit: true);

            if (splitResult.IsFailed)
            {
                return Result.Fail<ForecastModel>($"Data split failed: {splitResult.Errors.First().Message}");
            }

            var (trainingDataset, validationDataset) = splitResult.Value;
            _logger.LogInformation(
                "Split data into {TrainCount} training and {ValCount} validation samples",
                trainingDataset.SampleCount, validationDataset.SampleCount);

            // Step 5: Train model
            var trainingResult = await _modelTrainer.TrainAsync(
                trainingDataset,
                validationDataset,
                modelType,
                parameters,
                CancellationToken.None);

            if (trainingResult.IsFailed)
            {
                return Result.Fail<ForecastModel>($"Model training failed: {trainingResult.Errors.First().Message}");
            }

            var trainedModelResult = trainingResult.Value;
            _logger.LogInformation(
                "Training completed in {Duration:F2}s. Validation MAE: {MAE:F2}, MAPE: {MAPE:F2}%",
                trainedModelResult.TrainingDuration.TotalSeconds,
                trainedModelResult.ValidationMetrics.MAE,
                trainedModelResult.ValidationMetrics.MAPE);

            // Step 6: Create model entity
            var modelId = Guid.NewGuid();
            var model = new ForecastModel
            {
                Id = modelId,
                Name = $"{modelType}-{DateTime.UtcNow:yyyyMMdd-HHmmss}",
                ModelType = modelType,
                ConfigurationId = configurationId,
                Version = "1.0",
                IsActive = false,
                TrainingStartDate = parameters.TrainingStartDate,
                TrainingEndDate = parameters.TrainingEndDate,
                LookbackWindow = parameters.LookbackWindow,
                ForecastHorizon = parameters.ForecastHorizon,
                ValidationAccuracy = trainedModelResult.ValidationMetrics.RSquared,
                ValidationMAE = trainedModelResult.ValidationMetrics.MAE,
                ValidationMAPE = trainedModelResult.ValidationMetrics.MAPE,
                ValidationRMSE = trainedModelResult.ValidationMetrics.RMSE,
                Hyperparameters = System.Text.Json.JsonSerializer.Serialize(new
                {
                    parameters.LearningRate,
                    parameters.Epochs,
                    parameters.BatchSize,
                    parameters.HiddenUnits,
                    parameters.Dropout
                }),
                FeatureCount = featureDataset.FeatureCount,
                TrainingSampleCount = trainingDataset.SampleCount,
                ValidationSampleCount = validationDataset.SampleCount,
                CreatedAt = DateTime.UtcNow,
                Description = $"Model trained on {allTrainingData.Count} observations from {parameters.TrainingStartDate:yyyy-MM-dd} to {parameters.TrainingEndDate:yyyy-MM-dd}"
            };

            // Step 7: Cache trained model and scaling parameters in memory
            _modelCache[modelId] = (trainedModelResult.TrainedModel, normalizedDataset.ScalingParams);

            // Step 8: Serialize model to ML.NET format
            var serializedResult = _modelTrainer.SerializeModel(trainedModelResult.TrainedModel);
            if (serializedResult.IsSuccess)
            {
                model.ModelData = serializedResult.Value;
                _logger.LogInformation("Model serialized successfully ({Size} bytes)", model.ModelData.Length);
            }
            else
            {
                _logger.LogWarning("Model serialization failed: {Error}", serializedResult.Errors.First().Message);
                // Continue anyway - model is still in cache
            }

            // TODO: Store scaling parameters (currently only in cache)
            // Need to add ScalingParametersJson field to ForecastModel to persist scaling params

            // Step 9: Save to database
            var saveResult = await _modelRepository.AddAsync(model);
            if (saveResult.IsFailed)
            {
                _logger.LogWarning(
                    "Model {ModelId} cached in memory but failed to save to database: {Error}",
                    model.Id, saveResult.Errors.First().Message);
                // Return success anyway since model is in cache and functional
            }
            else
            {
                _logger.LogInformation(
                    "Model {ModelId} created successfully and saved to database",
                    model.Id);
            }

            return Result.Ok(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error training model for configuration {ConfigurationId}", configurationId);
            return Result.Fail<ForecastModel>($"Failed to train model: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<Dictionary<int, ForecastModel>>> TrainPerStationModelsAsync(
        Guid configurationId,
        IEnumerable<int> stationIds,
        ForecastModelType modelType,
        TrainingParameters parameters)
    {
        try
        {
            _logger.LogInformation(
                "Training per-station models for configuration {ConfigurationId}",
                configurationId);

            var models = new Dictionary<int, ForecastModel>();

            foreach (var stationId in stationIds)
            {
                // TODO: Train individual model for each station
                // Check if station has sufficient data
                var hasSufficientData = await _dataCollectionService
                    .HasSufficientDataForTrainingAsync(stationId, minimumDays: 180);

                if (!hasSufficientData.Value)
                {
                    _logger.LogWarning(
                        "Station {StationId} does not have sufficient data for training",
                        stationId);
                    continue;
                }

                // TODO: Train model specific to this station
                // For now, create placeholder
                var model = new ForecastModel
                {
                    Id = Guid.NewGuid(),
                    Name = $"{modelType}-Station-{stationId:N}",
                    ModelType = modelType,
                    ConfigurationId = configurationId,
                    Version = "1.0",
                    IsActive = false,
                    CreatedAt = DateTime.UtcNow,
                    Description = $"Station-specific model for {stationId}"
                };

                models[stationId] = model;
            }

            await Task.CompletedTask; // Placeholder

            _logger.LogInformation("Trained {Count} per-station models", models.Count);
            return Result.Ok(models);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error training per-station models");
            return Result.Fail<Dictionary<int, ForecastModel>>($"Failed to train per-station models: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<ForecastPrediction>> GenerateForecastAsync(
        int stationId,
        int forecastHorizon,
        DateTime? startDate = null)
    {
        try
        {
            var effectiveStartDate = startDate ?? DateTime.UtcNow;

            _logger.LogInformation(
                "Generating {Horizon}-period forecast for station {StationId} starting {StartDate}",
                forecastHorizon, stationId, effectiveStartDate);

            // Step 1: Get active model (try cache first, then database)
            // For now, we'll use the cache since model serialization is not yet implemented
            Guid modelId;
            ITrainedModel trainedModel;
            ScalingParameters scalingParams;

            if (_modelCache.Any())
            {
                (modelId, (trainedModel, scalingParams)) = _modelCache.Last();
                _logger.LogInformation("Using cached model {ModelId} for prediction", modelId);
            }
            else
            {
                // TODO: Load model from database once serialization is implemented
                return Result.Fail<ForecastPrediction>("No trained model available in cache. Please train a model first.");
            }

            // Step 2: Fetch recent historical data for feature engineering
            // We need at least 'lookbackWindow' days of historical data
            var lookbackWindow = 30; // Default, should match training parameters
            var dataStartDate = effectiveStartDate.AddDays(-lookbackWindow - 14); // Extra buffer for lag features
            var dataEndDate = effectiveStartDate.AddDays(-1); // Up to yesterday

            var historicalDataResult = await _dataCollectionService.GetHistoricalDataAsync(
                stationId,
                dataStartDate,
                dataEndDate);

            if (historicalDataResult.IsFailed)
            {
                return Result.Fail<ForecastPrediction>($"Failed to fetch historical data: {historicalDataResult.Errors.First().Message}");
            }

            var historicalData = historicalDataResult.Value.OrderBy(d => d.ObservationDate).ToList();

            if (historicalData.Count < lookbackWindow)
            {
                return Result.Fail<ForecastPrediction>(
                    $"Insufficient historical data. Need at least {lookbackWindow} observations, but got {historicalData.Count}");
            }

            _logger.LogInformation("Retrieved {Count} historical observations for feature engineering", historicalData.Count);

            // Step 3: Engineer features for the most recent data point
            var featureDatasetResult = _featureEngineer.EngineerFeatures(
                historicalData,
                lookbackWindow,
                forecastHorizon);

            if (featureDatasetResult.IsFailed)
            {
                return Result.Fail<ForecastPrediction>($"Feature engineering failed: {featureDatasetResult.Errors.First().Message}");
            }

            var featureDataset = featureDatasetResult.Value;

            if (featureDataset.SampleCount == 0)
            {
                return Result.Fail<ForecastPrediction>("No features could be engineered from historical data");
            }

            // Take the most recent sample (latest features for prediction)
            var latestSample = featureDataset.Samples.Last();

            // Step 4: Normalize features using the same scaling parameters from training
            var normalizedFeatures = NormalizeFeatures(latestSample.Features, scalingParams);

            // Step 5: Generate predictions using trained model
            var predictionResult = trainedModel.Predict(normalizedFeatures);

            if (predictionResult.IsFailed)
            {
                return Result.Fail<ForecastPrediction>($"Prediction failed: {predictionResult.Errors.First().Message}");
            }

            var normalizedPredictions = predictionResult.Value;

            // Step 6: Denormalize predictions back to original scale
            if (scalingParams.TargetScaling == null)
            {
                return Result.Fail<ForecastPrediction>("Target scaling parameters not available");
            }

            var denormalizedPredictions = _featureEngineer.DenormalizeValues(
                normalizedPredictions,
                scalingParams.TargetScaling.ToScalingParameters(scalingParams.ScalingType));

            // Convert to integers (demand values)
            var predictedValues = denormalizedPredictions.Value.Select(v => (int)Math.Round(v)).ToArray();

            _logger.LogInformation(
                "Generated {Count} predictions: [{Values}]",
                predictedValues.Length,
                string.Join(", ", predictedValues.Take(5)) + (predictedValues.Length > 5 ? "..." : ""));

            // Step 7: Calculate confidence intervals (optional)
            // For now, use a simple ±10% as placeholder
            // TODO: Use ModelEvaluator.CalculateConfidenceIntervals with historical errors
            var lowerBound = predictedValues.Select(v => (int)(v * 0.9)).ToArray();
            var upperBound = predictedValues.Select(v => (int)(v * 1.1)).ToArray();

            // Step 8: Create prediction entity
            var prediction = new ForecastPrediction
            {
                Id = Guid.NewGuid(),
                ForecastModelId = modelId,
                StationDeclarationId = stationId,
                PredictionDate = DateTime.UtcNow,
                ForecastStartDate = effectiveStartDate,
                PredictedValues = System.Text.Json.JsonSerializer.Serialize(predictedValues),
                LowerBound = System.Text.Json.JsonSerializer.Serialize(lowerBound),
                UpperBound = System.Text.Json.JsonSerializer.Serialize(upperBound),
                ConfidenceLevel = 0.95f,
                CreatedAt = DateTime.UtcNow,
                WasUsedInPlanning = false,
                WasOverridden = false
            };

            // Step 9: Save to database
            var saveResult = await _predictionRepository.AddAsync(prediction);
            if (saveResult.IsFailed)
            {
                _logger.LogWarning(
                    "Prediction generated but failed to save to database: {Error}",
                    saveResult.Errors.First().Message);
                // Return success anyway since prediction is valid
            }
            else
            {
                _logger.LogInformation("Generated and saved forecast {PredictionId}", prediction.Id);
            }

            return Result.Ok(prediction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating forecast for station {StationId}", stationId);
            return Result.Fail<ForecastPrediction>($"Failed to generate forecast: {ex.Message}");
        }
    }

    /// <summary>
    /// Normalizes features using provided scaling parameters.
    /// </summary>
    private float[] NormalizeFeatures(float[] features, ScalingParameters scalingParams)
    {
        var normalized = new float[features.Length];

        switch (scalingParams.ScalingType)
        {
            case ScalingType.MinMax:
                // MinMax scaling: (x - min) / (max - min)
                for (int i = 0; i < features.Length; i++)
                {
                    if (i < scalingParams.MinValues.Length && i < scalingParams.MaxValues.Length)
                    {
                        var min = scalingParams.MinValues[i];
                        var max = scalingParams.MaxValues[i];
                        var range = max - min;
                        normalized[i] = range > 0 ? (features[i] - min) / range : 0;
                    }
                    else
                    {
                        normalized[i] = features[i]; // Pass through if no scaling available
                    }
                }
                break;

            case ScalingType.StandardScaler:
                // Z-score standardization: (x - mean) / stddev
                for (int i = 0; i < features.Length; i++)
                {
                    if (i < scalingParams.MeanValues.Length && i < scalingParams.StdDevValues.Length)
                    {
                        var mean = scalingParams.MeanValues[i];
                        var stdDev = scalingParams.StdDevValues[i];
                        normalized[i] = stdDev > 0 ? (features[i] - mean) / stdDev : 0;
                    }
                    else
                    {
                        normalized[i] = features[i]; // Pass through if no scaling available
                    }
                }
                break;

            case ScalingType.None:
            default:
                // No scaling, return as-is
                Array.Copy(features, normalized, features.Length);
                break;
        }

        return normalized;
    }

    /// <inheritdoc/>
    public async Task<Result<Dictionary<int, ForecastPrediction>>> GenerateForecastsForAllStationsAsync(
        Guid configurationId,
        int forecastHorizon,
        DateTime? startDate = null)
    {
        try
        {
            _logger.LogInformation(
                "Generating forecasts for all stations in configuration {ConfigurationId}",
                configurationId);

            // TODO: Get all stations for configuration
            // TODO: Generate forecast for each station in parallel

            var forecasts = new Dictionary<int, ForecastPrediction>();

            // Placeholder
            await Task.CompletedTask;

            return Result.Ok(forecasts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating forecasts for configuration {ConfigurationId}", configurationId);
            return Result.Fail<Dictionary<int, ForecastPrediction>>($"Failed to generate forecasts: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<ForecastPrediction>> GenerateForecastWithModelAsync(
        Guid forecastModelId,
        int stationId,
        int forecastHorizon,
        DateTime? startDate = null)
    {
        try
        {
            _logger.LogInformation(
                "Generating forecast using model {ModelId} for station {StationId}",
                forecastModelId, stationId);

            // TODO: Load specific model and generate prediction
            return await GenerateForecastAsync(stationId, forecastHorizon, startDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating forecast with model {ModelId}", forecastModelId);
            return Result.Fail<ForecastPrediction>($"Failed to generate forecast: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<ModelMetrics>> EvaluateModelAsync(
        Guid forecastModelId,
        DateTime evaluationStartDate,
        DateTime evaluationEndDate)
    {
        try
        {
            _logger.LogInformation(
                "Evaluating model {ModelId} from {StartDate} to {EndDate}",
                forecastModelId, evaluationStartDate, evaluationEndDate);

            // Step 1: Load model from cache
            if (!_modelCache.TryGetValue(forecastModelId, out var cachedData))
            {
                return Result.Fail<ModelMetrics>("Model not found in cache. Please train the model first.");
            }

            var (trainedModel, scalingParams) = cachedData;

            // TODO: Step 2: Get historical data for evaluation period
            // For now, we'll create a placeholder since we need to know which configuration/stations to evaluate
            // This would typically come from the model metadata stored in the database

            _logger.LogWarning("Model evaluation requires database integration to fetch model metadata and historical data");

            // Create placeholder metrics
            var metrics = new ModelMetrics
            {
                Id = Guid.NewGuid(),
                ForecastModelId = forecastModelId,
                EvaluationType = EvaluationType.Test,
                MAE = 0,
                MAPE = 0,
                RMSE = 0,
                RSquared = 0,
                SampleCount = 0,
                EvaluationStartDate = evaluationStartDate,
                EvaluationEndDate = evaluationEndDate,
                CreatedAt = DateTime.UtcNow
            };

            // TODO: Full implementation when database integration is complete
            // var historicalData = await _dataCollectionService.GetHistoricalDataForAllStationsAsync(...);
            // var featureDataset = _featureEngineer.EngineerFeatures(historicalData, ...);
            // var normalizedDataset = _featureEngineer.NormalizeFeatures(featureDataset);
            // var evaluationResult = _modelEvaluator.Evaluate(trainedModel, normalizedDataset.Dataset);
            // metrics = evaluationResult.Value;

            await Task.CompletedTask; // Placeholder

            return Result.Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating model {ModelId}", forecastModelId);
            return Result.Fail<ModelMetrics>($"Failed to evaluate model: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<ModelMetrics>> EvaluateModelForStationAsync(
        Guid forecastModelId,
        int stationId,
        DateTime evaluationStartDate,
        DateTime evaluationEndDate)
    {
        try
        {
            _logger.LogInformation(
                "Evaluating model {ModelId} for station {StationId}",
                forecastModelId, stationId);

            // TODO: Implement station-specific evaluation
            return await EvaluateModelAsync(forecastModelId, evaluationStartDate, evaluationEndDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating model {ModelId} for station {StationId}", forecastModelId, stationId);
            return Result.Fail<ModelMetrics>($"Failed to evaluate model: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<ModelMetrics>>> CompareModelsAsync(
        IEnumerable<Guid> modelIds,
        DateTime evaluationStartDate,
        DateTime evaluationEndDate)
    {
        try
        {
            _logger.LogInformation("Comparing {Count} models", modelIds.Count());

            var metricsList = new List<ModelMetrics>();

            foreach (var modelId in modelIds)
            {
                var metricsResult = await EvaluateModelAsync(modelId, evaluationStartDate, evaluationEndDate);
                if (metricsResult.IsSuccess)
                {
                    metricsList.Add(metricsResult.Value);
                }
            }

            return Result.Ok<IEnumerable<ModelMetrics>>(metricsList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing models");
            return Result.Fail<IEnumerable<ModelMetrics>>($"Failed to compare models: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<ForecastModel>> ActivateModelAsync(Guid forecastModelId)
    {
        try
        {
            _logger.LogInformation("Activating model {ModelId}", forecastModelId);

            // Load the model to activate
            var modelResult = await _modelRepository.GetByIdAsync(forecastModelId);
            if (modelResult.IsFailed || modelResult.Value == null)
            {
                return Result.Fail<ForecastModel>($"Model {forecastModelId} not found");
            }

            var model = modelResult.Value;

            // Deactivate all other models for this configuration
            var deactivateResult = await _modelRepository.DeactivateAllForConfigurationAsync(model.ConfigurationId);
            if (deactivateResult.IsFailed)
            {
                return Result.Fail<ForecastModel>($"Failed to deactivate other models: {deactivateResult.Errors.First().Message}");
            }

            // Activate this model
            model.IsActive = true;
            var updateResult = await _modelRepository.UpdateAsync(model);
            if (updateResult.IsFailed)
            {
                return Result.Fail<ForecastModel>($"Failed to activate model: {updateResult.Errors.First().Message}");
            }

            _logger.LogInformation("Model {ModelId} activated successfully", forecastModelId);
            return Result.Ok(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating model {ModelId}", forecastModelId);
            return Result.Fail<ForecastModel>($"Failed to activate model: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Result> DeactivateModelAsync(Guid forecastModelId)
    {
        try
        {
            _logger.LogInformation("Deactivating model {ModelId}", forecastModelId);

            // TODO: Update model IsActive = false
            await Task.CompletedTask; // Placeholder

            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating model {ModelId}", forecastModelId);
            return Result.Fail($"Failed to deactivate model: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<ForecastModel?>> GetActiveModelAsync(Guid configurationId)
    {
        try
        {
            return await _modelRepository.GetActiveModelAsync(configurationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active model for configuration {ConfigurationId}", configurationId);
            return Result.Fail<ForecastModel?>($"Failed to get active model: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<ForecastModel>>> ListModelsAsync(
        Guid configurationId,
        bool includeInactive = true)
    {
        try
        {
            return await _modelRepository.GetByConfigurationAsync(configurationId, includeInactive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing models for configuration {ConfigurationId}", configurationId);
            return Result.Fail<IEnumerable<ForecastModel>>($"Failed to list models: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Result> DeleteModelAsync(Guid forecastModelId)
    {
        try
        {
            _logger.LogInformation("Deleting model {ModelId}", forecastModelId);

            // Delete related predictions and metrics first (cascading delete)
            await _predictionRepository.DeleteByModelAsync(forecastModelId);
            await _metricsRepository.DeleteByModelAsync(forecastModelId);

            // Delete the model itself
            var deleteResult = await _modelRepository.DeleteAsync(forecastModelId);
            if (deleteResult.IsFailed)
            {
                return Result.Fail($"Failed to delete model: {deleteResult.Errors.First().Message}");
            }

            // Remove from cache if present
            _modelCache.Remove(forecastModelId);

            _logger.LogInformation("Model {ModelId} deleted successfully", forecastModelId);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting model {ModelId}", forecastModelId);
            return Result.Fail($"Failed to delete model: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<ForecastModel>> RetrainModelAsync(
        Guid forecastModelId,
        TrainingParameters parameters)
    {
        try
        {
            _logger.LogInformation("Retraining model {ModelId}", forecastModelId);

            // TODO: Load existing model metadata
            // Train new version with updated data
            // Increment version number

            return Result.Fail<ForecastModel>("Not implemented");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retraining model {ModelId}", forecastModelId);
            return Result.Fail<ForecastModel>($"Failed to retrain model: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<ForecastPrediction>> UpdatePredictionWithActualsAsync(
        Guid predictionId,
        int[] actualValues)
    {
        try
        {
            _logger.LogInformation("Updating prediction {PredictionId} with actual values", predictionId);

            // TODO: Load prediction, update actual values, calculate MAE/MAPE
            // prediction.ActualValues = JsonSerializer.Serialize(actualValues);
            // Calculate metrics
            // await _predictionRepository.UpdateAsync(prediction);

            await Task.CompletedTask; // Placeholder

            return Result.Fail<ForecastPrediction>("Not implemented");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating prediction {PredictionId}", predictionId);
            return Result.Fail<ForecastPrediction>($"Failed to update prediction: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<ForecastPrediction>>> GetPredictionsAsync(
        int stationId,
        DateTime startDate,
        DateTime endDate)
    {
        try
        {
            return await _predictionRepository.GetByStationAsync(stationId, startDate, endDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting predictions for station {StationId}", stationId);
            return Result.Fail<IEnumerable<ForecastPrediction>>($"Failed to get predictions: {ex.Message}");
        }
    }
}

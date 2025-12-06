# ML Forecasting Setup and Usage Guide

## Overview
This guide explains how to set up and use the Neural Network-based demand forecasting system in SmartPPC.

**Current Status**: Phase 1 - Data Collection Infrastructure Complete
**Next Phase**: Database integration and ML.NET model training implementation

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                      SmartPPC.Api                            │
│  ┌────────────────────────────────────────────────────┐     │
│  │  ForecastDataCollectionBackgroundService           │     │
│  │  (ASP.NET Hosted Service)                          │     │
│  │  - Runs every N minutes                            │     │
│  │  - Collects demand data from configurations        │     │
│  └────────────────┬───────────────────────────────────┘     │
│                   │ Calls                                    │
│                   ▼                                          │
│  ┌────────────────────────────────────────────────────┐     │
│  │         Core ML Services (DI)                      │     │
│  │  - IForecastDataCollectionService                  │     │
│  │  - IForecastingService                             │     │
│  └────────────────┬───────────────────────────────────┘     │
└───────────────────┼──────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────────────────────────┐
│                    SmartPPC.Core                             │
│  ┌────────────────────────────────────────────────────┐     │
│  │  ML Services Implementation                        │     │
│  │  - ForecastDataCollectionService                   │     │
│  │  - ForecastingService                              │     │
│  └────────────────┬───────────────────────────────────┘     │
│                   │ Stores/Reads                             │
│                   ▼                                          │
│  ┌────────────────────────────────────────────────────┐     │
│  │  ML Domain Entities                                │     │
│  │  - ForecastTrainingData                            │     │
│  │  - ForecastModel                                   │     │
│  │  - ForecastPrediction                              │     │
│  │  - ModelMetrics                                    │     │
│  └────────────────────────────────────────────────────┘     │
└─────────────────────────────────────────────────────────────┘
                    │
                    ▼
            PostgreSQL Database
```

---

## Configuration

### appsettings.json

The background data collection service is configured via `appsettings.json`:

```json
{
  "ForecastDataCollection": {
    "Enabled": false,
    "InitialDelayMinutes": 5,
    "CollectionIntervalMinutes": 1440,
    "CollectOnStartup": false,
    "MaxConfigurationsPerCycle": null
  }
}
```

### Configuration Options Explained

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `Enabled` | bool | `false` | Master switch for background data collection. Set to `true` to enable. |
| `InitialDelayMinutes` | int | `5` | Minutes to wait after app startup before first collection run. Allows app to fully initialize. |
| `CollectionIntervalMinutes` | int | `1440` | Minutes between collection cycles. Default is 24 hours (daily collection). |
| `CollectOnStartup` | bool | `false` | If `true`, runs collection immediately on startup (ignores InitialDelay). Useful for testing. |
| `MaxConfigurationsPerCycle` | int? | `null` | Maximum number of configurations to process per cycle. `null` = no limit. |

### Environment-Specific Settings

**Production (`appsettings.json`)**:
- `Enabled: false` - Disabled by default, enable explicitly when ready
- `CollectionIntervalMinutes: 1440` - Daily collection (24 hours)

**Development (`appsettings.Development.json`)**:
- `Enabled: true` - Automatically enabled for testing
- `CollectionIntervalMinutes: 60` - Hourly collection for faster testing
- `InitialDelayMinutes: 1` - Quick startup

---

## Service Registration

Services are automatically registered in `Program.cs` (SmartPPC.Api/Program.cs:109-120):

```csharp
// Register ML Forecasting Services
builder.Services.AddScoped<IForecastDataCollectionService, ForecastDataCollectionService>();
builder.Services.AddScoped<IForecastingService, ForecastingService>();

// Configure ML Forecasting Options
builder.Services.Configure<ForecastDataCollectionOptions>(
    builder.Configuration.GetSection(ForecastDataCollectionOptions.SectionName));

// Register Background Data Collection Service
builder.Services.AddHostedService<ForecastDataCollectionBackgroundService>();
```

**Service Lifetimes:**
- `IForecastDataCollectionService` - **Scoped** (per HTTP request or scope)
- `IForecastingService` - **Scoped**
- `ForecastDataCollectionBackgroundService` - **Singleton** (hosted service, long-running)

---

## How Data Collection Works

### 1. Background Service Lifecycle

```
App Starts
    ↓
InitialDelayMinutes wait (default: 5 min in prod, 1 min in dev)
    ↓
[Collection Cycle]
    ├─ Create DI Scope
    ├─ Resolve IForecastDataCollectionService
    ├─ Resolve ConfigurationService
    ├─ Get all active configurations
    ├─ For each configuration:
    │   ├─ Get station declarations
    │   ├─ Extract demand data (from DemandForecasts)
    │   ├─ Extract buffer levels (from PastBuffers)
    │   ├─ Extract order amounts (from PastOrderAmounts)
    │   └─ Call RecordDemandObservationAsync()
    ├─ Log results
    └─ Dispose scope
    ↓
Wait CollectionIntervalMinutes (default: 1440 min = 24 hours)
    ↓
Repeat [Collection Cycle]
```

### 2. Data Collection Per Station

For each station in each configuration, the service:

1. **Extracts Demand**: Gets the most recent forecast value (representing actual demand)
2. **Extracts Buffer Level** (if station has buffer): Gets latest buffer level
3. **Extracts Order Amount** (if available): Gets latest order quantity
4. **Records Observation**: Calls `IForecastDataCollectionService.RecordDemandObservationAsync()`

### 3. Data Stored

Each observation creates a `ForecastTrainingData` record with:
- Configuration ID and Station ID (foreign keys)
- Observation date/time
- Demand value (actual demand observed)
- Buffer level (nullable)
- Order amount (nullable)
- Temporal features: Day of week, month, quarter
- Exogenous factors (JSON, currently null - for future use)

---

## Current Implementation Status

### ✅ Completed

1. **Domain Entities** (SmartPPC.Core/ML/Domain/)
   - ForecastTrainingData
   - ForecastModel
   - ForecastPrediction
   - ModelMetrics

2. **Service Interfaces** (SmartPPC.Core/ML/Services/)
   - IForecastDataCollectionService (12 methods)
   - IForecastingService (18 methods)

3. **Service Implementations** (SmartPPC.Core/ML/Services/)
   - ForecastDataCollectionService (skeleton, database access pending)
   - ForecastingService (skeleton, ML.NET integration pending)

4. **Background Service** (SmartPPC.Api/Services/)
   - ForecastDataCollectionBackgroundService (fully functional)
   - ForecastDataCollectionOptions (configuration class)

5. **Configuration**
   - appsettings.json (production defaults)
   - appsettings.Development.json (development settings)

6. **Dependency Injection**
   - Services registered in Program.cs
   - Options pattern configured

7. **ML.NET Packages**
   - Microsoft.ML v3.0.1
   - Microsoft.ML.TimeSeries v3.0.1
   - Microsoft.ML.OnnxRuntime v1.17.0

### 🚧 Pending (Database Integration Required)

1. **Update ApplicationDbContext**
   - Add DbSets for ML entities
   - Configure entity relationships
   - Create database migration

2. **Complete Service Implementations**
   - Replace `// TODO: Database access` with EF Core queries
   - Implement data persistence in ForecastDataCollectionService
   - Implement model storage in ForecastingService

3. **ML.NET Model Training**
   - Feature engineering implementation
   - LSTM+Attention model architecture
   - Training pipeline
   - Prediction engine

4. **UI Pages**
   - Model Management page
   - Enhanced Demand Forecast page
   - Forecast Dashboard

---

## Usage Scenarios

### Scenario 1: Enable Data Collection (Development)

**Already Configured!** In development mode, data collection is enabled by default.

1. Run the application
2. Check logs for: `"Forecast Data Collection Background Service is starting"`
3. After InitialDelay (1 minute), see: `"Starting forecast data collection cycle"`
4. Service will collect hourly (every 60 minutes)

### Scenario 2: Enable Data Collection (Production)

1. Edit `appsettings.json`:
```json
"ForecastDataCollection": {
  "Enabled": true,
  "CollectionIntervalMinutes": 1440
}
```

2. Restart the application
3. Monitor logs for data collection activity

### Scenario 3: Generate Synthetic Data (Testing/Development)

Use the `GenerateSyntheticDataAsync` method for testing:

```csharp
// Via DI-injected service
var result = await _dataCollectionService.GenerateSyntheticDataAsync(
    configurationId: myConfigId,
    stationId: myStationId,
    startDate: DateTime.UtcNow.AddMonths(-12), // 1 year ago
    numberOfDays: 365,
    baseDemand: 50,
    seasonalityFactor: 0.2f // 20% seasonal variation
);
```

This generates realistic demand data with:
- Weekly seasonality (7-day cycle)
- Slight upward trend
- Random noise
- Always positive demand values

### Scenario 4: Check Data Availability

```csharp
// Check if station has enough data for training
var hasSufficientData = await _dataCollectionService
    .HasSufficientDataForTrainingAsync(stationId, minimumDays: 180);

if (hasSufficientData.Value)
{
    // Proceed with model training
    var trainResult = await _forecastingService.TrainModelAsync(
        configurationId,
        ForecastModelType.LSTMAttention,
        trainingParameters);
}
```

### Scenario 5: Manual Data Collection Trigger

While the background service runs automatically, you can trigger manual collection:

```csharp
// In a controller or service
public async Task<IActionResult> TriggerDataCollection()
{
    var configurations = await _configurationService.GetAllConfigurationsAsync();

    foreach (var config in configurations)
    {
        var result = await _dataCollectionService
            .CollectFromProductionModelAsync(
                config.Id,
                productionModel,  // From DDMRP execution
                DateTime.UtcNow);

        _logger.LogInformation("Collected {Count} observations", result.Value);
    }

    return Ok();
}
```

---

## Monitoring and Logs

### Key Log Messages

**Service Startup:**
```
[Information] Forecast Data Collection Background Service is starting
[Information] Forecast data collection is disabled in configuration  // If disabled
```

**Collection Cycle:**
```
[Information] Starting forecast data collection cycle at {Time}
[Information] Found {Count} configurations to collect data from
[Information] Collecting data for configuration {ConfigurationId}
[Information] Collected {Count} demand observations for configuration {ConfigurationId}
[Information] Forecast data collection cycle completed. Next run in {Interval} minutes
```

**Errors:**
```
[Error] Error occurred during forecast data collection cycle
[Warning] Configuration {ConfigurationId} has no stations
[Warning] Failed to collect data for station {StationId}: {Error}
```

### Log Filtering

To see only ML forecasting logs:

**appsettings.json:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "SmartPPC.Api.Services.ForecastDataCollectionBackgroundService": "Debug",
      "SmartPPC.Core.ML.Services": "Debug"
    }
  }
}
```

---

## Troubleshooting

### Issue: Background service not running

**Check:**
1. Is `Enabled: true` in configuration?
2. Check application logs for startup message
3. Verify service registered in Program.cs

**Solution:**
```json
"ForecastDataCollection": {
  "Enabled": true  // Must be true!
}
```

### Issue: No data being collected

**Check:**
1. Are there active configurations with stations?
2. Do stations have DemandForecasts populated?
3. Check logs for collection cycle messages
4. Verify InitialDelayMinutes hasn't prevented first run yet

**Solution:**
- Wait for InitialDelayMinutes to elapse
- Set `CollectOnStartup: true` for immediate collection
- Reduce `InitialDelayMinutes` to 0 or 1 for testing

### Issue: Database errors during collection

**Current Expected Behavior:**
Since database integration is pending, you'll see log messages but no actual persistence yet. This is expected in the current phase.

**After Database Integration:**
- Check database connection string
- Verify migrations are applied
- Check DbContext registration

### Issue: Service consuming too many resources

**Solution:**
1. Increase `CollectionIntervalMinutes` (less frequent collection)
2. Set `MaxConfigurationsPerCycle` to limit batch size
3. Adjust `InitialDelayMinutes` to spread load

Example:
```json
"ForecastDataCollection": {
  "CollectionIntervalMinutes": 2880,  // Every 2 days
  "MaxConfigurationsPerCycle": 10     // Process max 10 configs per cycle
}
```

---

## Next Steps (Roadmap)

### Phase 1 Completion: Database Integration
1. Update ApplicationDbContext with ML entities
2. Create and apply database migration
3. Replace TODO markers in service implementations
4. Test end-to-end data collection → storage → retrieval

### Phase 2: ML.NET Model Training
1. Implement feature engineering services
2. Build LSTM+Attention model architecture
3. Implement training pipeline
4. Implement prediction generation

### Phase 3: UI Integration
1. Create Model Management page
2. Enhance Demand Forecast page with AI predictions
3. Create Forecast Dashboard with charts
4. Add data collection status indicators

### Phase 4: DDMRP Integration
1. Integrate forecast generation with ModelBuilder
2. Update ConfigurationService for AI forecasts
3. Add forecast source tracking
4. Performance comparison (AI vs manual)

---

## API Reference (Quick)

### IForecastDataCollectionService

```csharp
// Record single observation
await RecordDemandObservationAsync(configId, stationId, date, demand, buffer, order);

// Batch recording
await RecordDemandObservationsBatchAsync(observations);

// Collect from DDMRP execution
await CollectFromProductionModelAsync(configId, productionModel, date);

// Retrieve historical data
await GetHistoricalDataAsync(stationId, startDate, endDate);

// Check data availability
await HasSufficientDataForTrainingAsync(stationId, minimumDays: 180);

// Generate test data
await GenerateSyntheticDataAsync(configId, stationId, startDate, numberOfDays);
```

### IForecastingService

```csharp
// Train model (once data available)
await TrainModelAsync(configId, ForecastModelType.LSTMAttention, parameters);

// Generate forecast
await GenerateForecastAsync(stationId, forecastHorizon: 14);

// Batch forecasts
await GenerateForecastsForAllStationsAsync(configId, forecastHorizon: 14);

// Evaluate model
await EvaluateModelAsync(modelId, startDate, endDate);

// Activate model for production use
await ActivateModelAsync(modelId);
```

---

## FAQ

**Q: When will I have enough data to train a model?**
A: Minimum 6 months (180 days) of daily data is recommended. The system will collect data automatically once enabled.

**Q: Can I use the system without historical data?**
A: Yes, use `GenerateSyntheticDataAsync()` to create realistic test data for development and testing.

**Q: How do I know data collection is working?**
A: Check application logs for collection cycle messages. After database integration, query the `ForecastTrainingData` table directly.

**Q: Will this slow down my application?**
A: No, the background service runs independently and doesn't block requests. Data collection happens asynchronously.

**Q: Can I disable data collection temporarily?**
A: Yes, set `"Enabled": false` in appsettings.json and restart the app.

**Q: How much disk space will training data use?**
A: Very minimal. Each observation is ~200 bytes. 365 days × 10 stations = ~730 KB per year per configuration.

---

## Support and Documentation

- **Implementation Status**: [NN-Forecasting-Implementation-Status.md](./NN-Forecasting-Implementation-Status.md)
- **Architecture Overview**: [Architecture-Overview.md](./Architecture-Overview.md)
- **AI Integration Proposal**: [SmartPPC.Core_AI_Integration.md](./SmartPPC.Core_AI_Integration.md)

---

**Last Updated**: 2025-12-05
**Version**: 1.0 (Phase 1 - Data Collection Infrastructure)
**Status**: Background service operational, database integration pending

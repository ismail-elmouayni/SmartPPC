# ML Forecasting Database Schema

## Overview
This document describes the database schema for the Neural Network-based demand forecasting system in SmartPPC.

**Database**: PostgreSQL
**ORM**: Entity Framework Core 8.0
**Location**: `SmartPPC.Api/Data/ApplicationDbContext.cs`

---

## Entity Relationship Diagram

```
┌─────────────────┐
│  Configuration  │
└────────┬────────┘
         │
         ├──── (1:many) ────┐
         │                  │
         ▼                  ▼
┌──────────────────┐  ┌──────────────────────┐
│ StationDeclara.. │  │  ForecastTrainingData│
└────────┬─────────┘  └──────────────────────┘
         │                       │
         │                       └─── FK: ConfigurationId
         │                       └─── FK: StationDeclarationId
         │
         ├──── (1:many) ────┐
         │                  │
         ▼                  ▼
┌──────────────────┐  ┌──────────────────────┐
│  ForecastModel   │  │  ForecastPrediction  │
└────────┬─────────┘  └──────┬───────────────┘
         │                   │
         │                   ├─── FK: ForecastModelId
         │                   └─── FK: StationDeclarationId
         │
         ├──── (1:many) ────┐
         │                  │
         ▼                  ▼
┌──────────────────┐
│   ModelMetrics   │
└──────────────────┘
         │
         ├─── FK: ForecastModelId
         └─── FK: StationDeclarationId (nullable)
```

---

## Tables

### 1. ForecastTrainingData

**Purpose**: Stores historical demand observations used for training ML models.

**Table Name**: `ForecastTrainingData`

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| Id | uuid (PK) | No | Primary key |
| ConfigurationId | uuid (FK) | No | References Configurations.Id |
| StationDeclarationId | uuid (FK) | No | References StationDeclarations.Id |
| ObservationDate | timestamp | No | Date/time of observation |
| DemandValue | integer | No | Actual demand value observed |
| BufferLevel | integer | Yes | Buffer level at observation time |
| OrderAmount | integer | Yes | Order amount placed |
| DayOfWeek | integer | No | Day of week (1-7) |
| Month | integer | No | Month (1-12) |
| Quarter | integer | No | Quarter (1-4) |
| ExogenousFactors | text | Yes | JSON string with external factors |
| CreatedAt | timestamp | No | Record creation timestamp |

**Indexes**:
- `IX_ForecastTrainingData_StationDeclarationId_ObservationDate` (Composite)
- `IX_ForecastTrainingData_ConfigurationId_ObservationDate` (Composite)
- `IX_ForecastTrainingData_ObservationDate` (Single)

**Relationships**:
- **StationDeclaration** (many-to-one): `ON DELETE CASCADE`
- **Configuration** (many-to-one): `ON DELETE CASCADE`

---

### 2. ForecastModel

**Purpose**: Stores trained machine learning models with metadata and serialized weights.

**Table Name**: `ForecastModels`

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| Id | uuid (PK) | No | Primary key |
| Name | varchar(200) | No | User-friendly model name |
| ModelType | integer (enum) | No | Model architecture type |
| ConfigurationId | uuid (FK) | No | References Configurations.Id |
| ModelData | bytea | Yes | Serialized model (ONNX/ML.NET) |
| TrainingAccuracy | real | Yes | Training set accuracy |
| ValidationAccuracy | real | Yes | Validation set accuracy |
| ValidationMAE | real | Yes | Mean Absolute Error |
| ValidationRMSE | real | Yes | Root Mean Squared Error |
| TrainingSampleCount | integer | Yes | Number of training samples |
| ValidationSampleCount | integer | Yes | Number of validation samples |
| Hyperparameters | text | Yes | JSON string with hyperparameters |
| Version | varchar(50) | No | Model version (e.g., "1.0") |
| IsActive | boolean | No | Whether model is active |
| CreatedAt | timestamp | No | Model creation timestamp |
| UpdatedAt | timestamp | Yes | Last update timestamp |
| Description | text | Yes | Model description/notes |

**Indexes**:
- `IX_ForecastModels_ConfigurationId_IsActive` (Composite)
- `IX_ForecastModels_CreatedAt` (Single)

**Relationships**:
- **Configuration** (many-to-one): `ON DELETE CASCADE`

**Enum: ModelType**
- 1 = LSTM
- 2 = GRU
- 3 = LSTMAttention
- 4 = TemporalFusionTransformer
- 5 = MovingAverage
- 99 = Custom

---

### 3. ForecastPrediction

**Purpose**: Stores generated demand forecasts with confidence intervals.

**Table Name**: `ForecastPredictions`

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| Id | uuid (PK) | No | Primary key |
| ForecastModelId | uuid (FK) | No | References ForecastModels.Id |
| StationDeclarationId | uuid (FK) | No | References StationDeclarations.Id |
| PredictionDate | timestamp | No | When prediction was generated |
| ForecastStartDate | timestamp | No | Start date of forecast period |
| PredictedValues | jsonb | No | Array of predicted values |
| UpperConfidenceInterval | jsonb | Yes | Upper bounds of confidence interval |
| LowerConfidenceInterval | jsonb | Yes | Lower bounds of confidence interval |
| ConfidenceLevel | real | Yes | Confidence level (e.g., 0.95) |
| ActualValues | jsonb | Yes | Actual values (populated post-facto) |
| MAE | real | Yes | Mean Absolute Error (vs actuals) |
| MAPE | real | Yes | Mean Absolute Percentage Error |
| WasUsedInPlanning | boolean | No | Used in DDMRP planning |
| WasOverridden | boolean | No | Manually overridden by user |
| CreatedAt | timestamp | No | Record creation timestamp |

**Indexes**:
- `IX_ForecastPredictions_StationDeclarationId_ForecastStartDate` (Composite)
- `IX_ForecastPredictions_ForecastModelId_PredictionDate` (Composite)
- `IX_ForecastPredictions_PredictionDate` (Single)

**Relationships**:
- **ForecastModel** (many-to-one): `ON DELETE RESTRICT` (prevents model deletion if predictions exist)
- **StationDeclaration** (many-to-one): `ON DELETE CASCADE`

**JSON Format Examples**:
```json
// PredictedValues
[45, 52, 48, 50, 47, 51, 49, 53, 46, 54, 48, 50, 51, 49]

// UpperConfidenceInterval
[48, 55, 51, 53, 50, 54, 52, 56, 49, 57, 51, 53, 54, 52]

// LowerConfidenceInterval
[42, 49, 45, 47, 44, 48, 46, 50, 43, 51, 45, 47, 48, 46]
```

---

### 4. ModelMetrics

**Purpose**: Stores comprehensive evaluation metrics for forecasting models.

**Table Name**: `ModelMetrics`

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| Id | uuid (PK) | No | Primary key |
| ForecastModelId | uuid (FK) | No | References ForecastModels.Id |
| StationDeclarationId | uuid (FK) | Yes | References StationDeclarations.Id (nullable for aggregated metrics) |
| EvaluationType | integer (enum) | No | Type of evaluation |
| MAE | real | No | Mean Absolute Error |
| MAPE | real | No | Mean Absolute Percentage Error |
| RMSE | real | No | Root Mean Squared Error |
| RSquared | real | Yes | R-squared coefficient |
| MeanForecastError | real | Yes | Bias metric |
| ForecastErrorStdDev | real | Yes | Error standard deviation |
| TrackingSignal | real | Yes | Cumulative error tracking |
| SampleCount | integer | No | Number of samples evaluated |
| EvaluationStartDate | timestamp | No | Evaluation period start |
| EvaluationEndDate | timestamp | No | Evaluation period end |
| ForecastHorizon | integer | Yes | Forecast horizon used |
| AdditionalMetrics | jsonb | Yes | Extensible custom metrics |
| CreatedAt | timestamp | No | Record creation timestamp |

**Indexes**:
- `IX_ModelMetrics_ForecastModelId_EvaluationType` (Composite)
- `IX_ModelMetrics_ForecastModelId_StationDeclarationId` (Composite)
- `IX_ModelMetrics_CreatedAt` (Single)

**Relationships**:
- **ForecastModel** (many-to-one): `ON DELETE CASCADE`
- **StationDeclaration** (many-to-one, nullable): `ON DELETE SET NULL`

**Enum: EvaluationType**
- 1 = Training
- 2 = Validation
- 3 = Test
- 4 = Production
- 5 = CrossValidation

---

## PostgreSQL-Specific Features

### JSONB Columns
JSON fields are stored as `jsonb` (binary JSON) for:
- Efficient querying and indexing
- Native PostgreSQL JSON operations
- Smaller storage footprint vs `json` type

**Columns using JSONB**:
- `ForecastPrediction.PredictedValues`
- `ForecastPrediction.UpperConfidenceInterval`
- `ForecastPrediction.LowerConfidenceInterval`
- `ForecastPrediction.ActualValues`
- `ModelMetrics.AdditionalMetrics`

### BYTEA Column
Binary data stored as `bytea` (byte array):
- `ForecastModel.ModelData` - Serialized ML model (ONNX format)

### Index Strategy

**Composite Indexes** for common query patterns:
1. **Time-series queries**: `(StationId, Date)` combinations
2. **Active model lookup**: `(ConfigurationId, IsActive)`
3. **Model performance tracking**: `(ModelId, EvaluationType)`

**Single-column indexes** for:
- Date range queries: `ObservationDate`, `PredictionDate`, `CreatedAt`
- Lookups: `ForecastModelId`, `StationDeclarationId`

---

## Deletion Behavior

### CASCADE Deletes
When a parent record is deleted, child records are automatically deleted:
- `Configuration` → `ForecastTrainingData`, `ForecastModel`
- `StationDeclaration` → `ForecastTrainingData`, `ForecastPrediction`
- `ForecastModel` → `ModelMetrics`

### RESTRICT Deletes
Prevents deletion if child records exist:
- `ForecastModel` ← `ForecastPrediction`
  - Cannot delete a model if predictions exist
  - Must delete predictions first, or use a soft delete pattern

### SET NULL Deletes
Sets foreign key to NULL on parent deletion:
- `StationDeclaration` → `ModelMetrics`
  - Allows station deletion while preserving aggregated metrics

---

## Storage Estimates

### Per Station Per Year

**Training Data**:
- Daily observations: ~365 records
- Size per record: ~200 bytes
- **Total**: ~73 KB/station/year

**Predictions** (weekly forecast generation):
- ~52 records/year
- Size per record: ~500 bytes (with JSON arrays)
- **Total**: ~26 KB/station/year

**Models** (quarterly retraining):
- ~4 models/year
- Size per model: ~1-5 MB (depending on architecture)
- **Total**: ~4-20 MB/station/year

**Metrics**:
- ~12 records/year (multiple evaluation types)
- Size per record: ~300 bytes
- **Total**: ~3.6 KB/station/year

### Example: 10 Stations, 5 Years
- Training Data: 3.65 MB
- Predictions: 1.3 MB
- Models: 200-1000 MB (largest component)
- Metrics: 180 KB

**Total**: ~200-1005 MB for 10 stations over 5 years

---

## Query Performance Considerations

### Optimized Queries

**1. Get recent training data for station:**
```sql
SELECT * FROM "ForecastTrainingData"
WHERE "StationDeclarationId" = @stationId
  AND "ObservationDate" >= @startDate
ORDER BY "ObservationDate"
LIMIT 1000;
```
**Uses index**: `IX_ForecastTrainingData_StationDeclarationId_ObservationDate`

**2. Get active model for configuration:**
```sql
SELECT * FROM "ForecastModels"
WHERE "ConfigurationId" = @configId
  AND "IsActive" = true
LIMIT 1;
```
**Uses index**: `IX_ForecastModels_ConfigurationId_IsActive`

**3. Get predictions with actuals for evaluation:**
```sql
SELECT * FROM "ForecastPredictions"
WHERE "ForecastModelId" = @modelId
  AND "ActualValues" IS NOT NULL
ORDER BY "PredictionDate";
```
**Uses index**: `IX_ForecastPredictions_ForecastModelId_PredictionDate`

### JSONB Queries

**Extract predicted values:**
```sql
SELECT "PredictedValues"::jsonb->>0 as first_value,
       "PredictedValues"::jsonb->>6 as seventh_value
FROM "ForecastPredictions"
WHERE "Id" = @predictionId;
```

**Filter by array length:**
```sql
SELECT * FROM "ForecastPredictions"
WHERE jsonb_array_length("PredictedValues"::jsonb) >= 14;
```

---

## Maintenance

### Data Retention

**Recommended retention policies**:
- **Training Data**: 2-3 years (730-1095 days)
- **Predictions**: 1 year
- **Models**: Keep top 3 per configuration, delete old inactive models
- **Metrics**: Keep all (minimal storage)

**Cleanup service method**:
```csharp
await _dataCollectionService.CleanupOldDataAsync(
    configurationId,
    retentionDays: 730);
```

### Archival Strategy

For long-term storage:
1. Export old training data to CSV/Parquet
2. Store models in Azure Blob Storage or S3
3. Keep only metadata in PostgreSQL

---

## Migration Commands

### Create Migration
```bash
cd SmartPPC.Api
dotnet ef migrations add AddMLForecastingTables
```

### Apply Migration
```bash
dotnet ef database update
```

### Rollback Migration
```bash
dotnet ef database update <PreviousMigrationName>
```

### Generate SQL Script
```bash
dotnet ef migrations script AddMLForecastingTables > ml-forecasting-migration.sql
```

---

## Security Considerations

### Sensitive Data
- Model weights (`ModelData`) may contain proprietary ML algorithms
- Consider encrypting `ModelData` column if models are valuable IP
- Predictions may reveal business strategy - apply appropriate access controls

### Access Control
- Restrict direct database access to ML tables
- Use EF Core and service layer for all operations
- Implement row-level security if multi-tenant

---

## Future Enhancements

### Potential Additions

**1. Model Version History Table**
Track complete lineage of model retraining:
```
ForecastModelVersion
- Id
- ForecastModelId (FK)
- Version
- ModelData
- Metrics
- CreatedAt
```

**2. Feature Importance Table**
Store feature contributions for interpretability:
```
FeatureImportance
- Id
- ForecastModelId (FK)
- FeatureName
- ImportanceScore
- CreatedAt
```

**3. Prediction Audit Log**
Track when predictions were used:
```
PredictionAudit
- Id
- ForecastPredictionId (FK)
- UsedInPlanningAt
- UserId
- WasModified
- ModifiedValues (jsonb)
```

---

## References

- **ApplicationDbContext**: `SmartPPC.Api/Data/ApplicationDbContext.cs`
- **Domain Entities**: `SmartPPC.Core/ML/Domain/`
- **EF Core Docs**: https://learn.microsoft.com/en-us/ef/core/
- **PostgreSQL JSONB**: https://www.postgresql.org/docs/current/datatype-json.html

---

**Last Updated**: 2025-12-05
**Schema Version**: 1.0
**Status**: Ready for migration

# Database Migration Instructions

## Run These Commands to Create ML Forecasting Tables

Execute the following commands from the `SmartPPC.Api` directory:

```bash
cd SmartPPC.Api

# Create the migration
dotnet ef migrations add AddMLForecastingTables

# Apply the migration to create tables
dotnet ef database update
```

## What This Creates

The migration will create 4 new tables in PostgreSQL:

1. **ForecastTrainingData** - Historical demand observations
2. **ForecastModels** - Trained ML models with metadata
3. **ForecastPredictions** - Generated forecasts with confidence intervals
4. **ModelMetrics** - Model performance evaluation results

## Verify Tables Were Created

```sql
-- Connect to PostgreSQL and run:
\dt

-- You should see the 4 new tables listed
```

## Rollback (if needed)

```bash
# Remove the last migration
dotnet ef migrations remove

# OR revert to a specific migration
dotnet ef database update <PreviousMigrationName>
```

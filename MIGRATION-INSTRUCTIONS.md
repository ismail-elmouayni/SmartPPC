# Database Migration Instructions - ML Forecasting Tables

## Prerequisites

✅ ApplicationDbContext has been updated with ML entities
✅ Entity relationships configured
✅ Ready to create and apply migration

---

## Step 1: Create the Migration

Navigate to the API project directory and create the migration:

```bash
cd SmartPPC.Api
dotnet ef migrations add AddMLForecastingTables -o Data/Migrations
```

**What this does**:
- Generates migration files in `SmartPPC.Api/Data/Migrations/`
- Creates `[timestamp]_AddMLForecastingTables.cs` with `Up()` and `Down()` methods
- Creates `ApplicationDbContextModelSnapshot.cs` update

---

## Step 2: Review the Migration

Open the generated migration file and verify it contains:

**Tables to be created**:
- `ForecastTrainingData`
- `ForecastModels`
- `ForecastPredictions`
- `ModelMetrics`

**Indexes to be created**:
- Composite indexes on `(StationDeclarationId, ObservationDate)`
- Composite indexes on `(ConfigurationId, IsActive)`
- Single indexes on date columns
- And more (see ML-Database-Schema.md)

**Foreign keys to be created**:
- `ForecastTrainingData` → `StationDeclarations`, `Configurations`
- `ForecastModels` → `Configurations`
- `ForecastPredictions` → `ForecastModels`, `StationDeclarations`
- `ModelMetrics` → `ForecastModels`, `StationDeclarations`

---

## Step 3: Apply the Migration

### Option A: Update Database Directly

```bash
dotnet ef database update
```

This applies the migration to the database specified in your connection string.

### Option B: Generate SQL Script (Recommended for Production)

```bash
dotnet ef migrations script --output ml-forecasting-migration.sql
```

Then review the SQL script before applying it manually to your database.

**Or generate script for specific migration**:
```bash
dotnet ef migrations script <PreviousMigration> AddMLForecastingTables --output ml-forecasting-migration.sql
```

---

## Step 4: Verify Migration

After applying the migration, verify the tables were created:

```sql
-- PostgreSQL
SELECT table_name
FROM information_schema.tables
WHERE table_schema = 'public'
  AND table_name LIKE '%Forecast%';
```

Expected results:
- `ForecastTrainingData`
- `ForecastModels`
- `ForecastPredictions`
- `ModelMetrics`

**Check indexes**:
```sql
SELECT tablename, indexname
FROM pg_indexes
WHERE tablename LIKE '%Forecast%';
```

---

## Step 5: Test Data Flow

### Test 1: Synthetic Data Generation

```csharp
// In a test or controller
var result = await _dataCollectionService.GenerateSyntheticDataAsync(
    configurationId: testConfigId,
    stationId: testStationId,
    startDate: DateTime.UtcNow.AddMonths(-6),
    numberOfDays: 180,
    baseDemand: 50,
    seasonalityFactor: 0.2f
);

// Should return 180 (number of records created)
```

**Verify in database**:
```sql
SELECT COUNT(*), MIN("ObservationDate"), MAX("ObservationDate")
FROM "ForecastTrainingData"
WHERE "StationDeclarationId" = '<test-station-guid>';
```

### Test 2: Background Service Data Collection

1. Enable data collection in `appsettings.Development.json`:
```json
"ForecastDataCollection": {
  "Enabled": true,
  "CollectOnStartup": true,
  "InitialDelayMinutes": 0
}
```

2. Run the application and check logs:
```
[Information] Forecast Data Collection Background Service is starting
[Information] Starting forecast data collection cycle
[Information] Collected {Count} demand observations
```

3. Verify in database:
```sql
SELECT * FROM "ForecastTrainingData"
ORDER BY "CreatedAt" DESC
LIMIT 10;
```

---

## Troubleshooting

### Issue: Migration fails with "relation already exists"

**Cause**: Tables may already exist from a previous attempt.

**Solution**:
```bash
# Option 1: Remove the migration and try again
dotnet ef migrations remove

# Option 2: Drop the tables manually
# (CAUTION: This deletes data!)
```

### Issue: "No DbContext was found"

**Cause**: Wrong directory or missing EF Core tools.

**Solution**:
```bash
# Ensure you're in SmartPPC.Api directory
cd SmartPPC.Api

# Install EF Core tools if needed
dotnet tool install --global dotnet-ef

# Or update
dotnet tool update --global dotnet-ef
```

### Issue: Connection string error

**Cause**: Database not accessible or connection string incorrect.

**Solution**:
1. Verify PostgreSQL is running
2. Check connection string in `appsettings.json`
3. Test connection:
```bash
psql -h localhost -p 5432 -U smartppc_user -d smartppc
```

### Issue: Aspire database connection

**Cause**: Using Aspire-managed PostgreSQL with different connection method.

**Solution**:
The migration uses the connection string from `appsettings.json`. Make sure:
1. PostgreSQL container is running (via Aspire or docker-compose)
2. Connection string matches the actual database
3. If using Aspire, temporarily use explicit connection string for migration

---

## Rollback Procedure

If you need to rollback the migration:

### Option 1: Rollback to previous migration
```bash
dotnet ef database update <PreviousMigrationName>
```

### Option 2: Remove migration entirely
```bash
# Remove the migration file (before applying to database)
dotnet ef migrations remove
```

### Option 3: Manual SQL rollback
```sql
-- Drop tables in reverse order of dependencies
DROP TABLE IF EXISTS "ModelMetrics";
DROP TABLE IF EXISTS "ForecastPredictions";
DROP TABLE IF EXISTS "ForecastModels";
DROP TABLE IF EXISTS "ForecastTrainingData";
```

---

## Post-Migration Checklist

- [ ] Migration created successfully
- [ ] Migration reviewed (Up and Down methods)
- [ ] Database updated (migration applied)
- [ ] Tables visible in database
- [ ] Indexes created correctly
- [ ] Foreign keys configured
- [ ] Synthetic data test passed
- [ ] Background service collecting data
- [ ] Queries returning expected results

---

## Next Steps After Migration

1. **Update Service Implementations**
   - Replace `// TODO: Database access` in `ForecastDataCollectionService`
   - Replace `// TODO: Database access` in `ForecastingService`
   - Test all CRUD operations

2. **Test End-to-End Flow**
   - Data collection → Storage → Retrieval
   - Model training skeleton → Storage
   - Prediction generation skeleton → Storage

3. **Enable Data Collection**
   - Set `"Enabled": true` in production configuration
   - Monitor logs for collection activity
   - Verify data accumulation over time

4. **Proceed to Phase 2**
   - Implement ML.NET model training
   - Build LSTM+Attention architecture
   - Create prediction engine

---

## References

- **Database Schema Documentation**: `Documentation/ML-Database-Schema.md`
- **Setup Guide**: `Documentation/ML-Forecasting-Setup-Guide.md`
- **Implementation Status**: `Documentation/NN-Forecasting-Implementation-Status.md`

---

**Created**: 2025-12-05
**Target Database**: PostgreSQL (via Aspire)
**EF Core Version**: 8.0

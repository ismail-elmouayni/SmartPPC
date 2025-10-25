# Database Migrations Instructions

## Prerequisites
1. Ensure Docker is running
2. Start PostgreSQL database:
   ```bash
   docker-compose up -d postgres
   ```

## Generate Initial Migration

From the solution root directory, run:

```bash
cd SmartPPC.Api
dotnet ef migrations add InitialCreate --output-dir Data/Migrations
```

## Apply Migrations to Database

```bash
dotnet ef database update
```

## Verify Migration

Check if the tables were created:
```bash
docker exec -it smartppc_postgres psql -U smartppc_user -d smartppc_dev -c "\dt"
```

## Additional Commands

### Create a new migration (after model changes):
```bash
dotnet ef migrations add <MigrationName> --output-dir Data/Migrations
```

### Rollback to a previous migration:
```bash
dotnet ef database update <PreviousMigrationName>
```

### Remove last migration (if not applied):
```bash
dotnet ef migrations remove
```

### Generate SQL script without applying:
```bash
dotnet ef migrations script -o migration.sql
```

## Expected Tables

After running migrations, you should see the following tables:
- AspNetUsers (extended with FirstName, LastName, Phone, Address)
- AspNetRoles
- AspNetUserRoles
- AspNetUserClaims
- AspNetUserLogins
- AspNetUserTokens
- AspNetRoleClaims
- Configurations
- GeneralSettings
- StationDeclarations
- StationPastBuffers
- StationPastOrderAmounts
- StationDemandForecasts
- StationInputs

## Troubleshooting

### Connection Issues
If you get connection errors, check:
1. PostgreSQL is running: `docker ps | grep postgres`
2. Connection string matches docker-compose.yml settings
3. Port 5432 is not already in use

### Migration Conflicts
If you encounter migration conflicts:
1. Delete the Migrations folder
2. Drop and recreate the database
3. Generate migrations again

# pgAdmin User Guide

pgAdmin is a web-based database management tool for PostgreSQL. This guide covers how to use pgAdmin with SmartPPC.

---

## Accessing pgAdmin

| Environment | URL | Default Credentials |
|-------------|-----|---------------------|
| Docker Compose | http://localhost:5050 | `admin@smartppc.com` / `YourPgAdminPassword!` |
| Kubernetes | http://localhost:30050 | See `deploy/k8s/secrets.yaml` |

---

## First-Time Setup: Connect to PostgreSQL

### Step 1: Login to pgAdmin

1. Open your browser and go to **http://localhost:5050**
2. Enter your credentials:
   - **Email**: `admin@smartppc.com`
   - **Password**: `YourPgAdminPassword!` (or value from `.env`)

### Step 2: Register the PostgreSQL Server

1. In the left panel, right-click **"Servers"**
2. Select **"Register"** → **"Server..."**

3. **General** tab:
   - **Name**: `SmartPPC` (or any friendly name)

4. **Connection** tab:
   | Field | Docker Compose | Kubernetes |
   |-------|----------------|------------|
   | **Host** | `postgres` | `postgres.smartppc.svc.cluster.local` |
   | **Port** | `5432` | `5432` |
   | **Maintenance database** | `smartppc` | `smartppc` |
   | **Username** | `smartppc_user` | `smartppc_user` |
   | **Password** | (from `.env` file) | (from `secrets.yaml`) |

5. ✅ Check **"Save password"** (optional)
6. Click **"Save"**

---

## Common Tasks

### Browse Tables

1. Expand **Servers** → **SmartPPC** → **Databases** → **smartppc**
2. Expand **Schemas** → **public** → **Tables**
3. Right-click a table → **View/Edit Data** → **All Rows**

### Run SQL Queries

1. Right-click on **smartppc** database
2. Select **"Query Tool"**
3. Type your SQL in the editor
4. Click **▶ Execute** (or press `F5`)

**Example queries:**

```sql
-- List all tables
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public';

-- Count rows in a table
SELECT COUNT(*) FROM "Users";

-- View table structure
SELECT column_name, data_type, is_nullable
FROM information_schema.columns
WHERE table_name = 'Users';
```

### Export Query Results

1. Run your query in the Query Tool
2. Click the **Download** button (📥) in the results panel
3. Choose format: CSV, JSON, or Text

### Export Entire Table

1. Right-click the table → **Import/Export Data...**
2. Select **"Export"** tab
3. Choose:
   - **Filename**: Path to save
   - **Format**: CSV, Text, or Binary
4. Click **OK**

### Import Data

1. Right-click the table → **Import/Export Data...**
2. Select **"Import"** tab
3. Configure:
   - **Filename**: Path to your data file
   - **Format**: Match your file format
   - **Header**: Yes (if first row contains headers)
4. Click **OK**

---

## Database Backup & Restore

### Backup Database

1. Right-click **smartppc** database → **Backup...**
2. Configure:
   - **Filename**: `/var/lib/pgadmin/backup_smartppc.sql`
   - **Format**: Plain (SQL) or Custom
   - **Encoding**: UTF8
3. Click **Backup**

**Via Command Line (recommended for production):**

```powershell
# Docker Compose
docker exec smartppc_postgres pg_dump -U smartppc_user smartppc > backup.sql

# Kubernetes
kubectl exec -n smartppc postgres-0 -- pg_dump -U smartppc_user smartppc > backup.sql
```

### Restore Database

1. Right-click **smartppc** database → **Restore...**
2. Select your backup file
3. Click **Restore**

**Via Command Line:**

```powershell
# Docker Compose
Get-Content backup.sql | docker exec -i smartppc_postgres psql -U smartppc_user -d smartppc

# Kubernetes
Get-Content backup.sql | kubectl exec -i -n smartppc postgres-0 -- psql -U smartppc_user -d smartppc
```

---

## Monitoring & Maintenance

### View Active Connections

1. Expand **SmartPPC** server
2. Click **"Dashboard"** tab
3. View:
   - **Server activity**: Active sessions
   - **Database activity**: Queries per database

**Or via SQL:**

```sql
SELECT pid, usename, application_name, client_addr, state, query
FROM pg_stat_activity
WHERE datname = 'smartppc';
```

### Check Database Size

```sql
SELECT pg_size_pretty(pg_database_size('smartppc')) AS database_size;
```

### Check Table Sizes

```sql
SELECT 
    tablename,
    pg_size_pretty(pg_total_relation_size(schemaname || '.' || tablename)) AS size
FROM pg_tables
WHERE schemaname = 'public'
ORDER BY pg_total_relation_size(schemaname || '.' || tablename) DESC;
```

### Vacuum & Analyze (Maintenance)

```sql
-- Reclaim storage and update statistics
VACUUM ANALYZE;

-- Full vacuum (more aggressive, locks table)
VACUUM FULL;
```

---

## Entity Framework Migrations

SmartPPC uses Entity Framework Core for database migrations.

### View Applied Migrations

```sql
SELECT * FROM "__EFMigrationsHistory" ORDER BY "MigrationId";
```

### Check Current Schema

1. Expand **Schemas** → **public**
2. Browse **Tables**, **Sequences**, **Views**

---

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `F5` | Execute query |
| `Ctrl + S` | Save query |
| `Ctrl + Space` | Autocomplete |
| `Ctrl + /` | Comment/uncomment |
| `Ctrl + Shift + F` | Format SQL |

---

## Troubleshooting

### Cannot connect to server

1. Verify PostgreSQL container is running:
   ```powershell
   docker ps | Select-String postgres
   ```

2. Check if using correct hostname:
   - From pgAdmin container: use `postgres` (service name)
   - From host machine: use `localhost`

### "Password authentication failed"

1. Check password in `.env` file or `secrets.yaml`
2. Ensure no extra spaces in password
3. Try recreating the connection

### pgAdmin is slow

1. Limit rows when viewing data:
   - Right-click table → **View/Edit Data** → **First 100 Rows**
2. Close unused query tabs
3. Increase Docker memory allocation

### Connection refused

Ensure PostgreSQL is healthy:

```powershell
# Docker Compose
docker exec smartppc_postgres pg_isready -U smartppc_user -d smartppc

# Kubernetes
kubectl exec -n smartppc postgres-0 -- pg_isready -U smartppc_user -d smartppc
```

---

## Useful Links

- [pgAdmin Documentation](https://www.pgadmin.org/docs/)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- [Entity Framework Core - PostgreSQL](https://www.npgsql.org/efcore/)

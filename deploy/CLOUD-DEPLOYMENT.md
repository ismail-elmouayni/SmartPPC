# SmartPPC Cloud Deployment Guide

This guide covers deploying SmartPPC to cloud platforms for demos and production.

## Quick Comparison

| Platform | Free Tier | PostgreSQL | GitHub CI/CD | Monthly Cost |
|----------|-----------|------------|--------------|--------------|
| **Render** ⭐ | 750h/month | 90 days free | ✅ Native | $0-7 |
| Railway | $5 trial | Included | ✅ Native | $5-10 |
| Fly.io | Generous | Included | Manual setup | $0-5 |

---

## 🚀 Option 1: Render.com (Recommended for Demo)

### Prerequisites
- GitHub account with SmartPPC repo
- Render.com account (free)

### Deployment Steps

#### Step 1: Sign Up
1. Go to [render.com](https://render.com)
2. Click **"Get Started for Free"**
3. Sign up with **GitHub** (enables auto-deploy)

#### Step 2: Create PostgreSQL Database
1. Dashboard → **"New +"** → **"PostgreSQL"**
2. Configure:
   - **Name**: `smartppc-db`
   - **Database**: `smartppc`
   - **User**: `smartppc_user`
   - **Region**: Frankfurt (or closest to you)
   - **Plan**: **Free**
3. Click **"Create Database"**
4. Wait for creation, then go to **"Info"** tab
5. Copy **"Internal Database URL"** - looks like:
   ```
   postgres://smartppc_user:password@host/smartppc
   ```

#### Step 3: Convert Connection String
Render uses `postgres://` format, but .NET needs Npgsql format. Convert:

**From Render:**
```
postgres://smartppc_user:PASSWORD@HOSTNAME:5432/smartppc
```

**To Npgsql (for .NET):**
```
Host=HOSTNAME;Port=5432;Database=smartppc;Username=smartppc_user;Password=PASSWORD
```

#### Step 4: Create Web Service
1. Dashboard → **"New +"** → **"Web Service"**
2. Select **"Build and deploy from a Git repository"**
3. Connect your GitHub repo (authorize if needed)
4. Select the SmartPPC repository
5. Configure:
   | Setting | Value |
   |---------|-------|
   | **Name** | `smartppc-api` |
   | **Region** | Same as database |
   | **Branch** | `main` |
   | **Runtime** | Docker |
   | **Dockerfile Path** | `SmartPPC.Api/Dockerfile` |
   | **Instance Type** | Free |

#### Step 5: Set Environment Variables
Scroll to **"Environment Variables"** and add:

| Key | Value |
|-----|-------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__smartppc` | `Host=YOUR_DB_HOST;Port=5432;Database=smartppc;Username=smartppc_user;Password=YOUR_PASSWORD` |

> ⚠️ Replace with your actual database host and password from Step 2
> 
> **Note:** The app uses `ConnectionStrings__smartppc` (from .NET Aspire configuration)

#### Step 6: Deploy
1. Click **"Create Web Service"**
2. Render will:
   - Clone your repo
   - Build the Docker image
   - Deploy the container
3. Wait 3-5 minutes for deployment
4. Access via the provided `.onrender.com` URL

### Auto-Deploy (CI/CD)
Every push to `main` triggers automatic deployment:
- Render monitors your GitHub repo
- New commits auto-trigger builds
- Zero-downtime deployments

### Free Tier Limitations
- **Spin down**: Service sleeps after 15 minutes of inactivity
- **Cold start**: First request after sleep takes 30-60 seconds
- **Database**: Free PostgreSQL expires after 90 days (then $7/month)

---

## 🚀 Option 2: Railway.app

### Deployment Steps

#### Step 1: Sign Up
1. Go to [railway.app](https://railway.app)
2. Sign up with GitHub

#### Step 2: Create Project
1. Click **"New Project"**
2. Select **"Deploy from GitHub repo"**
3. Select SmartPPC repository

#### Step 3: Add PostgreSQL
1. In project, click **"+ New"**
2. Select **"Database"** → **"Add PostgreSQL"**
3. Railway provisions database automatically

#### Step 4: Configure Web Service
1. Click on your service
2. Go to **"Settings"** → **"Build"**
3. Set:
   - **Builder**: Dockerfile
   - **Dockerfile Path**: `SmartPPC.Api/Dockerfile`
   - **Watch Paths**: `/`

#### Step 5: Environment Variables
Go to **"Variables"** tab and add:

```
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__smartppc=${{Postgres.DATABASE_URL}}
```

> Railway auto-injects `${{Postgres.DATABASE_URL}}` from the PostgreSQL service
> **Note:** The connection string format may need conversion (see Render section)

#### Step 6: Generate Domain
1. Go to **"Settings"** → **"Networking"**
2. Click **"Generate Domain"**
3. Access via `.railway.app` URL

### Cost
- **Trial**: $5 free credits
- **After trial**: ~$5-10/month depending on usage

---

## 🚀 Option 3: Fly.io

### Prerequisites
- Install Fly CLI: `powershell -Command "iwr https://fly.io/install.ps1 -useb | iex"`

### Deployment Steps

```bash
# Login
fly auth login

# Create app
fly launch --name smartppc-api --dockerfile SmartPPC.Api/Dockerfile

# Create PostgreSQL
fly postgres create --name smartppc-db

# Attach database
fly postgres attach smartppc-db --app smartppc-api

# Set secrets
fly secrets set ASPNETCORE_ENVIRONMENT=Production --app smartppc-api

# Deploy
fly deploy
```

---

## 📋 Troubleshooting

### Common Issues

#### Database Connection Fails
- Verify connection string format (Npgsql, not URI)
- Ensure web service and database are in the same region
- Use **Internal** database URL, not External

#### Build Fails
- Check Dockerfile path is correct
- Ensure all project files are committed
- Check build logs for missing dependencies

#### Health Check Fails
- Ensure `/health` endpoint exists
- Verify `ASPNETCORE_URLS` is set to `http://+:8080`
- Check application logs for startup errors

### Useful Commands

**Render CLI:**
```bash
# Install
npm install -g @render-cli/cli

# Login
render login

# View logs
render logs --service smartppc-api
```

**Railway CLI:**
```bash
# Install
npm install -g @railway/cli

# Login
railway login

# View logs
railway logs
```

---

## 🔧 Infrastructure as Code

### render.yaml (included in repo)
The `render.yaml` file in the root defines infrastructure. When connected to Render:
1. Go to **"Blueprints"** in dashboard
2. Connect your repo
3. Render auto-provisions all services defined in `render.yaml`

---

## 💡 Tips for Demo

1. **Keep it warm**: For demos, access the URL periodically to prevent spin-down
2. **Use caching**: Browser caching reduces cold-start impact
3. **Upgrade when needed**: Before important demos, consider temporary upgrade to paid tier

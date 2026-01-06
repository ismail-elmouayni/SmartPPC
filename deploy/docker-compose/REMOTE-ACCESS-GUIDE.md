# SmartPPC Remote Access Guide

## Security Overview

This guide covers how to securely expose SmartPPC to the internet with proper protection.

## 🔐 Security Measures Implemented

1. **HTTPS/SSL encryption** - All traffic encrypted
2. **Rate limiting** - Prevents brute force attacks (10 requests/second per IP)
3. **Security headers** - Protects against common web vulnerabilities
4. **Reverse proxy** - Hides internal architecture
5. **Application authentication** - Built-in ASP.NET Core Identity

## 📋 Prerequisites

- Domain name (recommended) or Dynamic DNS
- Port forwarding access on your router
- SSL certificate (Let's Encrypt free or self-signed for testing)

---

## Option 1: Port Forwarding with Reverse Proxy (Recommended)

### Step 1: Generate SSL Certificates

#### A. Self-Signed Certificate (for testing only)
```powershell
# Create SSL directory
New-Item -ItemType Directory -Path "F:\Repos\SmartPPC\deploy\docker-compose\nginx\ssl" -Force

# Generate self-signed certificate
cd F:\Repos\SmartPPC\deploy\docker-compose\nginx\ssl
openssl req -x509 -nodes -days 365 -newkey rsa:2048 `
  -keyout key.pem -out cert.pem `
  -subj "/CN=smartppc.local"
```

#### B. Let's Encrypt Certificate (for production)
Use Certbot with DNS challenge or HTTP challenge. Add this to docker-compose.public.yml:
```yaml
  certbot:
    image: certbot/certbot
    container_name: smartppc_certbot
    volumes:
      - ./nginx/ssl:/etc/letsencrypt
      - ./nginx/www:/var/www/certbot
    command: certonly --webroot --webroot-path=/var/www/certbot --email your@email.com --agree-tos --no-eff-email -d yourdomain.com
```

### Step 2: Update Environment Variables

Edit `.env`:
```env
DOMAIN_NAME=yourdomain.com  # or your public IP
```

### Step 3: Start Services with Nginx

```powershell
cd F:\Repos\SmartPPC\deploy\docker-compose

# Start main services
docker-compose -f docker-compose.prod.yml up -d

# Start Nginx reverse proxy
docker-compose -f docker-compose.public.yml up -d
```

### Step 4: Configure Router Port Forwarding

Log into your router (usually http://192.168.1.1) and forward:
- **External Port 80** → **Internal IP 192.168.1.31:80** (HTTP)
- **External Port 443** → **Internal IP 192.168.1.31:443** (HTTPS)

### Step 5: Set Up Dynamic DNS (if no static IP)

If you don't have a static public IP, use a DDNS service:
- **No-IP** (free): https://www.noip.com
- **DuckDNS** (free): https://www.duckdns.org
- **DynDNS**
- Your router may have built-in DDNS support

### Step 6: Access Remotely

After setup, access from anywhere:
- `https://yourdomain.com` or `https://your-ddns-hostname.duckdns.org`

---

## Option 2: VPN Access (Most Secure)

Instead of exposing SmartPPC directly, use a VPN:

### WireGuard VPN Setup

```yaml
# Add to docker-compose.public.yml
  wireguard:
    image: linuxserver/wireguard
    container_name: smartppc_vpn
    cap_add:
      - NET_ADMIN
      - SYS_MODULE
    environment:
      - PUID=1000
      - PGID=1000
      - TZ=Europe/Paris
      - SERVERURL=auto
      - SERVERPORT=51820
      - PEERS=5  # Number of VPN clients
      - PEERDNS=auto
    volumes:
      - ./wireguard:/config
    ports:
      - 51820:51820/udp
    restart: unless-stopped
```

**Advantages:**
- ✅ No web application exposed to internet
- ✅ Encrypted tunnel
- ✅ Access entire local network
- ✅ Multiple device support

**Router Setup:**
- Forward **UDP Port 51820** → **192.168.1.31:51820**

---

## Option 3: Cloudflare Tunnel (Zero Trust)

No port forwarding needed! Uses Cloudflare's network.

### Setup Cloudflare Tunnel

1. Sign up for Cloudflare (free)
2. Add your domain
3. Install cloudflared:

```yaml
# Add to docker-compose.public.yml
  cloudflared:
    image: cloudflare/cloudflared:latest
    container_name: smartppc_cloudflared
    command: tunnel --no-autoupdate run --token YOUR_TUNNEL_TOKEN
    restart: unless-stopped
    networks:
      - smartppc_network
```

**Advantages:**
- ✅ No port forwarding needed
- ✅ DDoS protection
- ✅ Free SSL certificates
- ✅ Cloudflare security features

---

## Option 4: Tailscale (Easiest VPN)

Super simple personal VPN - no configuration needed!

1. Install Tailscale on your PC: https://tailscale.com/download
2. Install Tailscale on devices you want to access from
3. Access SmartPPC via Tailscale IP (e.g., `http://100.x.x.x:8080`)

**Advantages:**
- ✅ Zero configuration
- ✅ Free for personal use
- ✅ Works behind any firewall/NAT
- ✅ Extremely secure

---

## 🛡️ Additional Security Recommendations

### 1. Disable PostgreSQL External Access
In `docker-compose.prod.yml`, remove postgres port exposure:
```yaml
postgres:
  # Remove or comment out:
  # ports:
  #   - "${POSTGRES_PORT:-5432}:5432"
```

### 2. Strong Passwords
Update `.env`:
```env
POSTGRES_PASSWORD=YourVeryStrongPasswordHere123!@#
```

### 3. Enable Fail2Ban (Optional)
Add fail2ban to ban IPs after failed login attempts.

### 4. Regular Updates
```powershell
docker-compose pull
docker-compose up -d
```

### 5. Backup Database Regularly
```powershell
docker exec smartppc_postgres pg_dump -U smartppc_user smartppc > backup_$(Get-Date -Format 'yyyyMMdd').sql
```

### 6. Monitor Logs
```powershell
docker-compose logs -f
```

---

## 🎯 Recommended Setup by Use Case

| Use Case | Recommended Solution | Security Level |
|----------|---------------------|----------------|
| Family/Friends access | Tailscale VPN | ⭐⭐⭐⭐⭐ |
| Small team | WireGuard VPN | ⭐⭐⭐⭐⭐ |
| Public demo/showcase | Nginx + Let's Encrypt | ⭐⭐⭐⭐ |
| Business/Production | Cloudflare Tunnel | ⭐⭐⭐⭐⭐ |
| Testing only | Self-signed cert + Port forward | ⭐⭐ |

---

## 🚨 Security Warnings

❌ **DO NOT:**
- Expose database (PostgreSQL) port directly to internet
- Use weak passwords
- Disable authentication
- Use self-signed certificates in production
- Forget to update regularly

✅ **DO:**
- Use HTTPS/SSL always
- Enable rate limiting
- Use strong passwords
- Regular backups
- Monitor access logs
- Keep Docker images updated
- Use VPN when possible

---

## 📞 Support

For security concerns, check:
- Docker security docs: https://docs.docker.com/engine/security/
- OWASP guidelines: https://owasp.org/
- Let's Encrypt: https://letsencrypt.org/

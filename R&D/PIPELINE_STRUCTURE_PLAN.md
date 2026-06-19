# Pipeline Structure Refactor — VM1 / VM2 / VM3 Organization

## Current State

```
Pipeline/
├─ local/               ← Local dev Docker Compose (all services)
├─ OracleVM/           ← Production deployment (mixed VM1 + VM2 config)
│  ├─ docker-compose.prod.yml
│  ├─ docker-compose.monitoring.yml
│  ├─ init-db.sh
│  ├─ nginx.conf
│  ├─ prometheus.yml
│  └─ ... (other configs)
└─ ...
```

**Problem**: `OracleVM/` mixes configs for VM1 (backend) and VM2 (monitoring). No space for VM3 (Python services).

---

## New Structure

```
Pipeline/
├─ local/                          ← Local dev (unchanged)
│
├─ OracleVM/
│  ├─ VM1/                         ← BE services (Docker) + PostgreSQL + Redis
│  │  ├─ docker-compose.yml        ← .NET services: gateway, user, fishdex, aquahome
│  │  ├─ services/
│  │  │  ├─ nginx.conf             ← Reverse proxy :80/:443
│  │  │  ├─ prometheus.yml         ← Prometheus config (scrape BE endpoints)
│  │  │  └─ init-db.sh             ← Init PostgreSQL databases
│  │  ├─ data/
│  │  │  ├─ postgresql/            ← PostgreSQL persist volume (mount)
│  │  │  ├─ redis/                 ← Redis persist volume (mount)
│  │  │  └─ .env                   ← DB passwords, JWT keys
│  │  └─ README.md
│  │
│  ├─ VM2/                         ← Monitoring (Docker)
│  │  ├─ docker-compose.yml        ← Grafana, Loki, Prometheus
│  │  ├─ monitoring/
│  │  │  ├─ grafana.ini
│  │  │  ├─ loki-config.yml
│  │  │  ├─ promtail-config.yml    ← Logs from VM1 → Loki
│  │  │  └─ datasources.yml
│  │  ├─ data/
│  │  │  └─ grafana/               ← Grafana persist volume
│  │  └─ README.md
│  │
│  ├─ VM3/                         ← AI Services (Python)
│  │  ├─ docker-compose.yml        ← embedding_service + image_search_service
│  │  ├─ embedding_service/
│  │  │  ├─ main.py                ← FastAPI app
│  │  │  ├─ requirements.txt        ← sentence-transformers, fastapi, uvicorn
│  │  │  ├─ Dockerfile             ← Python 3.11, arm64-compatible
│  │  │  ├─ docker-compose.yml     ← Expose :8000
│  │  │  ├─ test/
│  │  │  │  └─ test_embedding.py
│  │  │  └─ models/                ← .gitignore (models/ not committed)
│  │  │     └─ .gitignore
│  │  ├─ image_search_service/
│  │  │  ├─ main.py                ← FastAPI app
│  │  │  ├─ requirements.txt        ← openclip, fastapi, pillow, uvicorn
│  │  │  ├─ Dockerfile
│  │  │  ├─ docker-compose.yml     ← Expose :8001
│  │  │  ├─ test/
│  │  │  │  ├─ test_clip.py
│  │  │  │  └─ test_images/        ← Sample images for testing
│  │  │  └─ models/                ← .gitignore (models/ not committed)
│  │  │     └─ .gitignore
│  │  ├─ data/
│  │  │  └─ .gitkeep
│  │  ├─ .env                      ← Optional (for local testing)
│  │  └─ README.md
│  │
│  ├─ shared/                      ← Shared across VMs
│  │  ├─ .env.example              ← Template for all VMs
│  │  ├─ DEPLOY_CHECKLIST.md       ← Production deploy steps
│  │  └─ README.md                 ← Overall architecture diagram
│  │
│  └─ ARCHITECTURE.md              ← High-level overview (replaces root README for OracleVM)
│
└─ ...
```

---

## Migration Steps

### Phase 1: Reorganize Existing Files (VM1 + VM2)

1. **Create VM1 folder**:
   ```bash
   mkdir -p Pipeline/OracleVM/VM1/services
   mkdir -p Pipeline/OracleVM/VM1/data
   ```

2. **Move VM1-related files**:
   ```bash
   # From Pipeline/OracleVM/ → Pipeline/OracleVM/VM1/
   mv Pipeline/OracleVM/docker-compose.prod.yml Pipeline/OracleVM/VM1/docker-compose.yml
   mv Pipeline/OracleVM/init-db.sh Pipeline/OracleVM/VM1/services/
   mv Pipeline/OracleVM/nginx.conf Pipeline/OracleVM/VM1/services/
   mv Pipeline/OracleVM/prometheus.yml Pipeline/OracleVM/VM1/services/
   ```

3. **Create VM2 folder**:
   ```bash
   mkdir -p Pipeline/OracleVM/VM2/monitoring
   mkdir -p Pipeline/OracleVM/VM2/data
   ```

4. **Move VM2-related files**:
   ```bash
   # From Pipeline/OracleVM/ → Pipeline/OracleVM/VM2/
   mv Pipeline/OracleVM/docker-compose.monitoring.yml Pipeline/OracleVM/VM2/docker-compose.yml
   mv Pipeline/OracleVM/loki-config.yml Pipeline/OracleVM/VM2/monitoring/
   mv Pipeline/OracleVM/promtail-config.yml Pipeline/OracleVM/VM2/monitoring/
   ```

5. **Create shared folder**:
   ```bash
   mkdir -p Pipeline/OracleVM/shared
   mv Pipeline/OracleVM/.env.example Pipeline/OracleVM/shared/
   mv Pipeline/OracleVM/DEPLOY_CHECKLIST.md Pipeline/OracleVM/shared/
   ```

### Phase 2: Create VM3 Structure

```bash
mkdir -p Pipeline/OracleVM/VM3/embedding_service
mkdir -p Pipeline/OracleVM/VM3/embedding_service/test
mkdir -p Pipeline/OracleVM/VM3/embedding_service/models
mkdir -p Pipeline/OracleVM/VM3/image_search_service
mkdir -p Pipeline/OracleVM/VM3/image_search_service/test
mkdir -p Pipeline/OracleVM/VM3/image_search_service/models
mkdir -p Pipeline/OracleVM/VM3/data
```

### Phase 3: Scaffold VM3 Services (from story breakdown)

See Story 2.1 + 4.1 deliverables in `option_a_story_breakdown.md`.

---

## File Changes Needed

### 1. `Pipeline/OracleVM/VM1/docker-compose.yml`

**Current**: `docker-compose.prod.yml` (all services + monitoring)

**New**: Only backend services
```yaml
version: '3.8'
services:
  nginx:
    image: nginx:alpine
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./services/nginx.conf:/etc/nginx/nginx.conf:ro
      - ./services/ssl/:/etc/nginx/ssl/:ro
    networks:
      - fishlover_prod

  api_gateway:
    build:
      context: ../../../BackEndProject
      dockerfile: Dockerfile
    ports:
      - "5000:5000"
    env_file: ./shared/.env
    environment:
      ASPNETCORE_ENVIRONMENT: Production
    networks:
      - fishlover_prod

  user_management:
    build:
      context: ../../../BackEndProject
      dockerfile: Dockerfile
    ports:
      - "8080:8080"
    env_file: ./shared/.env
    networks:
      - fishlover_prod

  # ... fishdex, aquahome services

  postgres:
    image: pgvector/pgvector:pg16
    ports:
      - "5432:5432"
    environment:
      POSTGRES_INITDB_ARGS: "-c listen_addresses='*'"
    volumes:
      - ./data/postgresql:/var/lib/postgresql/data
      - ./services/init-db.sh:/docker-entrypoint-initdb.d/init-db.sh
    networks:
      - fishlover_prod

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    volumes:
      - ./data/redis:/data
    command: redis-server --requirepass ${REDIS_PASSWORD}
    networks:
      - fishlover_prod

  prometheus:
    image: prom/prometheus:latest
    ports:
      - "9090:9090"
    volumes:
      - ./services/prometheus.yml:/etc/prometheus/prometheus.yml:ro
    networks:
      - fishlover_prod

networks:
  fishlover_prod:
    driver: bridge
```

### 2. `Pipeline/OracleVM/VM2/docker-compose.yml`

**Current**: `docker-compose.monitoring.yml`

**New**: Only monitoring (Grafana + Loki)
```yaml
version: '3.8'
services:
  grafana:
    image: grafana/grafana:latest
    ports:
      - "3000:3000"
    volumes:
      - ./monitoring/grafana.ini:/etc/grafana/grafana.ini:ro
      - ./data/grafana:/var/lib/grafana
    networks:
      - fishlover_prod

  loki:
    image: grafana/loki:latest
    ports:
      - "3100:3100"
    volumes:
      - ./monitoring/loki-config.yml:/etc/loki/local-config.yml:ro
    networks:
      - fishlover_prod

  promtail:
    image: grafana/promtail:latest
    volumes:
      - ./monitoring/promtail-config.yml:/etc/promtail/config.yml:ro
      - /var/log:/var/log:ro
    command: -config.file=/etc/promtail/config.yml
    networks:
      - fishlover_prod

networks:
  fishlover_prod:
    external: true  # Connect to VM1 network
```

### 3. `Pipeline/OracleVM/VM3/docker-compose.yml`

**New file**:
```yaml
version: '3.8'
services:
  embedding_service:
    build:
      context: ./embedding_service
      dockerfile: Dockerfile
    ports:
      - "8000:8000"
    environment:
      PYTHONUNBUFFERED: 1
      PORT: 8000
    volumes:
      - ./embedding_service/models:/app/models
    networks:
      - fishlover_prod
    restart: unless-stopped

  image_search_service:
    build:
      context: ./image_search_service
      dockerfile: Dockerfile
    ports:
      - "8001:8001"
    environment:
      PYTHONUNBUFFERED: 1
      PORT: 8001
    volumes:
      - ./image_search_service/models:/app/models
    networks:
      - fishlover_prod
    restart: unless-stopped

networks:
  fishlover_prod:
    external: true  # Connect to VM1 network
```

### 4. `.gitignore` Updates

Add to root `.gitignore`:
```
# VM3 AI service models (too large, download at runtime)
Pipeline/OracleVM/VM3/embedding_service/models/
Pipeline/OracleVM/VM3/image_search_service/models/

# VM1/VM2/VM3 data volumes
Pipeline/OracleVM/VM*/data/
Pipeline/OracleVM/*/data/
```

### 5. `Pipeline/OracleVM/shared/.env.example`

Template for all VMs:
```bash
# === VM1: Backend Services ===
POSTGRES_USER=fishlover_admin
POSTGRES_PASSWORD=...
REDIS_PASSWORD=...
JWT_SECRET_KEY=...
GROQ_API_KEY=...

# === VM2: Monitoring ===
GRAFANA_ADMIN_PASSWORD=...

# === VM3: AI Services ===
EMBEDDING_MODEL=all-MiniLM-L6-v2
CLIP_MODEL=ViT-B-32
```

### 6. `Pipeline/OracleVM/ARCHITECTURE.md`

Replace existing docs with diagram showing 3 VMs:
```
┌─────────────────────────────────────────┐
│ FE (Cloudflare Pages)                   │
└──────────────────┬──────────────────────┘
                   │ HTTPS
┌──────────────────▼──────────────────────┐
│ VM1: Backend (90GB, 12GB RAM)           │
├──────────────────────────────────────────┤
│ nginx :80/:443                           │
│ Ocelot :5000                             │
│ UserMgmt :8080                           │
│ FishDex :8081                            │
│ AquaHome :8082                           │
│ PostgreSQL :5432                         │
│ Redis :6379                              │
│ Prometheus :9090                         │
└──────────────────┬──────────────────────┘
                   │ Docker bridge
       ┌───────────┴───────────┐
       │                       │
┌──────▼──────────┐    ┌──────▼──────────┐
│ VM2: Monitoring │    │ VM3: AI Services│
│ (60GB, 6GB RAM) │    │ (TBD, 6GB RAM)  │
├─────────────────┤    ├─────────────────┤
│ Grafana :3000   │    │ Embedding :8000 │
│ Loki :3100      │    │ Image Search... │
│ Promtail        │    │ :8001           │
└─────────────────┘    └─────────────────┘
```

---

## Deployment Instructions (Updated)

### Initial Setup (One-time)

```bash
# VM1
cd Pipeline/OracleVM/VM1
source ../shared/.env
docker-compose up -d

# VM2
cd Pipeline/OracleVM/VM2
source ../shared/.env
docker-compose up -d

# VM3 (after Story 2.1 + 4.1 complete)
cd Pipeline/OracleVM/VM3
source ../shared/.env
docker-compose up -d
```

### Health Check

```bash
# VM1: Backend
curl https://api.fishlover.org/api/health

# VM2: Monitoring
curl http://VM2_IP:3000 (Grafana)

# VM3: AI Services
curl http://VM3_IP:8000/health (embedding)
curl http://VM3_IP:8001/health (image search)
```

---

## Git Commit Plan

```bash
# 1. Create new directories (commit empty)
git add -A Pipeline/OracleVM/VM*/ Pipeline/OracleVM/shared/
git commit -m "refactor: restructure Pipeline/OracleVM for VM1/VM2/VM3"

# 2. Move existing files (preserve history)
git mv Pipeline/OracleVM/docker-compose.prod.yml Pipeline/OracleVM/VM1/docker-compose.yml
# (repeat for other files)
git commit -m "refactor: move VM1 configs to Pipeline/OracleVM/VM1/"
git commit -m "refactor: move VM2 configs to Pipeline/OracleVM/VM2/"

# 3. Update .gitignore
git add .gitignore
git commit -m "chore: add AI service models to .gitignore"
```

---

## Timeline

- ✅ Phase 1 (organize VM1 + VM2): 1 day (CLI commands)
- ✅ Phase 2 (scaffold VM3): <1 day (mkdir + touch files)
- ⏳ Phase 3 (implement VM3 services): 8-10 days (Stories 2.1 + 4.1)

**Recommendation**: Do Phase 1-2 now, Phase 3 when starting Story 2.1.

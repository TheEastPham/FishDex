# VM1 — Backend Services

**Specs**: 90GB disk, 12GB RAM (Oracle Always Free)

## Services

- **nginx** — Reverse proxy (SSL termination, rate limiting)
- **Ocelot API Gateway** — .NET gateway :5000
- **UserManagement** — Auth service :8080
- **FishDex** — Species data service :8081
- **AquaHome** — Aquarium orchestration :8082
- **PostgreSQL 16 + pgvector** — Primary database :5432
- **Redis 7** — Cache + session store :6379
- **Prometheus** — Metrics scraper :9090

## Quick Start

```bash
cd Pipeline/OracleVM/VM1
source ../shared/.env
docker-compose up -d
```

## Health Check

```bash
curl http://localhost:5000/health              # Gateway
curl http://localhost:8080/health              # UserManagement
curl http://localhost:8081/health              # FishDex
curl http://localhost:8082/health              # AquaHome
curl http://localhost:9090                     # Prometheus
```

## Volumes

- `data/postgresql/` — PostgreSQL data (mount to `/var/lib/postgresql/data`)
- `data/redis/` — Redis RDB (mount to `/data`)

## Network

All services on `fishlover_prod` bridge network. VM2 + VM3 connect via `external: true`.

## Configuration

- `.env` sourced from `../shared/.env`
- `services/init-db.sh` — auto-run on first postgres startup
- `services/prometheus.yml` — scrape targets

See `../shared/DEPLOY_CHECKLIST.md` for production deployment steps.

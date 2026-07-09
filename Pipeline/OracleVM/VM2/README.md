# VM2 — Monitoring

**Specs**: 60GB disk, 6GB RAM (Oracle Always Free)

## Services

- **Grafana** — Dashboard :3000
- **Loki** — Log aggregation :3100
- **Promtail** — Log shipper (tails VM1 logs)

## Quick Start

```bash
cd Pipeline/OracleVM/VM2
source ../shared/.env
docker-compose up -d
```

## Access

- **Grafana**: http://VM2_IP:3000 (default user: admin)
- **Loki API**: http://VM2_IP:3100

## Data Sources (in Grafana)

Add these data sources:
1. **Prometheus** — http://VM1_IP:9090 (metrics from VM1)
2. **Loki** — http://localhost:3100 (logs from VM1)

## Volumes

- `data/grafana/` — Dashboards + settings (mount to `/var/lib/grafana`)

## Network

Connects to `fishlover_prod` bridge network (created by VM1).

```bash
# VM2 needs VM1 running first
docker network ls | grep fishlover_prod
```

## Configuration

- `monitoring/loki-config.yml` — Log storage config
- `monitoring/promtail-config.yml` — Log collection (scrapes `/var/log` from VM1)

See `../shared/DEPLOY_CHECKLIST.md` for dashboard setup.

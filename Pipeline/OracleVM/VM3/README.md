# VM3 — AI Services (v2.0)

**Specs**: TBD, 6GB RAM (Oracle Always Free)

**Status**: 🚧 Under development (Stories 2.1 + 4.1)

## Services (TBD)

- **Embedding Service** — Python FastAPI :8000 (all-MiniLM-L6-v2)
- **Image Search Service** — Python FastAPI :8001 (CLIP ViT-B/32)

## Quick Start (When Ready)

```bash
cd Pipeline/OracleVM/VM3
source ../shared/.env
docker-compose up -d
```

## Health Check

```bash
curl http://localhost:8000/health              # Embedding service
curl http://localhost:8001/health              # Image search service
```

## Directory Structure

```
VM3/
├─ embedding_service/
│  ├─ main.py                  ← FastAPI app (implement in Story 2.1)
│  ├─ requirements.txt          ← Dependencies (Story 2.1)
│  ├─ Dockerfile                ← Build image (Story 2.1)
│  ├─ docker-compose.yml        ← Service config (Story 2.1)
│  ├─ test/
│  │  └─ test_embedding.py      ← Unit tests (Story 2.1)
│  ├─ models/                   ← Download at runtime (not committed)
│  │  └─ .gitignore
│  └─ .gitignore
│
├─ image_search_service/
│  ├─ main.py                  ← FastAPI app (implement in Story 4.1)
│  ├─ requirements.txt          ← Dependencies (Story 4.1)
│  ├─ Dockerfile                ← Build image (Story 4.1)
│  ├─ docker-compose.yml        ← Service config (Story 4.1)
│  ├─ test/
│  │  ├─ test_clip.py           ← Unit tests (Story 4.1)
│  │  └─ test_images/           ← Sample images
│  ├─ models/                   ← Download at runtime (not committed)
│  │  └─ .gitignore
│  └─ .gitignore
│
├─ data/                        ← Volume mount (for future use)
├─ docker-compose.yml           ← Orchestrate both services
├─ .env                         ← Environment (optional, for local testing)
└─ README.md                    ← This file
```

## Models (Not Committed)

Models are downloaded at container startup:
- `embedding_service/models/` — all-MiniLM-L6-v2 (~120MB, auto-downloaded)
- `image_search_service/models/` — CLIP ViT-B/32 (~450MB, auto-downloaded)

See `.gitignore` in each service directory.

## Network

Connects to `fishlover_prod` bridge network.

```bash
# VM3 needs VM1 running first
docker network ls | grep fishlover_prod
```

## Implementation

- **Story 2.1**: Implement embedding service (FastAPI + sentence-transformers)
- **Story 4.1**: Implement image search service (FastAPI + OpenCLIP)

See `R&D/option_a_story_breakdown.md` for detailed tasks.

## Monitoring

Both services expose Prometheus metrics on `/metrics`:
- `http://localhost:8000/metrics` (embedding service)
- `http://localhost:8001/metrics` (image search service)

Add to Prometheus scrape targets in VM1.

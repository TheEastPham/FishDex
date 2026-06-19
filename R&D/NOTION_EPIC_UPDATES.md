# Notion Epic Updates — Align with Option A Breakdown

## Current State vs. New Breakdown

### Epic 2: AI Q&A Service

**Current Notion stories** (từ 2026-06-12):
```
2.0: Embedding Ingestion (chuyển từ Story 1.3)
2.1: Project Scaffolding
2.2: RAG Query Pipeline
2.3: LLM Provider (abstraction — Claude + OpenAI)
2.4: Response Caching
2.5: FE — AI Chat Widget
2.6: AquaHome BFF Proxy
```

**New breakdown** (based on Option A):
```
2.0: Embedding Ingestion (SAME — populates pgvector with all-MiniLM-L6-v2)
2.1: Embedding Service on VM3 (RENAMED from Scaffolding → focus on service delivery)
2.2: RAG Pipeline Integration (RENAMED from RAG Query Pipeline → broader scope)
2.3: Groq LLM Provider (CHANGED from abstraction → Groq-specific)
2.4: Caching & Optimization (SAME — Redis caching)
2.5: Frontend Chat Widget (SAME)
2.6: BFF Proxy (SAME)
```

**Key Changes**:

| Aspect | Old | New | Reason |
|--------|-----|-----|--------|
| **Story 2.1** | Scaffolding (new .NET microservice) | Embedding Service (Python FastAPI on VM3) | Embedding is utility, not standalone microservice. Host on VM3 with CLIP for resource efficiency. |
| **Story 2.3** | LLM Provider abstraction (ILlmProvider for Claude + OpenAI) | Groq API integration (focus on Groq, fallback ready) | Option A uses Groq free tier. Abstraction premature; implement Groq first, fallback path as separate branch. |
| **VM Deployment** | Single VM prod | VM1 (BE) + VM2 (Grafana) + VM3 (Python services) | New architecture requires Python FastAPI for embedding + image search. |

---

## Notion Epic 2 Update Checklist

**Page**: https://app.notion.com/p/36d159daab448196b99ce28d68e929b3

### Content to Update

1. **Replace Design Reference**:
   - Old: `Planning&Design/Design/system-context.html` → Tab "Data Flow — RAG"
   - New: Link to `R&D/option_a_architecture.md` (Markdown diagram)

2. **Replace key design decisions**:
   ```markdown
   OLD:
   - Service độc lập, không expose qua Gateway
   - ILlmProvider interface pluggable
   
   NEW:
   - Embedding service: Python FastAPI on VM3 :8000 (not .NET microservice)
   - Groq API as primary LLM (30 RPM free tier) + Gemini fallback
   - Rate limit monitoring: log to Grafana, alert if >25 RPM
   - VM3 constraint: 6GB RAM for embedding (200MB) + CLIP (450MB) + inference (1-2GB)
   ```

3. **Remove old resources** (not applicable):
   - Remove Claude C# SDK links (no Claude integration in v2.0 plan)
   - Remove Anthropic resources (using Groq instead)

4. **Update Story 2.1** description:
   ```markdown
   OLD:
   Story 2.1: Project Scaffolding
   - [ ] Tạo project `FishLover.AIQnA.API`
   - [ ] Docker Compose stack
   
   NEW:
   Story 2.1: Embedding Service on VM3
   - [ ] Python FastAPI service + sentence-transformers all-MiniLM-L6-v2
   - [ ] POST /embed endpoint (text → 384-d vector)
   - [ ] Prometheus metrics + Grafana dashboard
   - [ ] Load test: 100 concurrent requests, <100ms latency p95
   - [ ] Integrate with Ocelot gateway (/embeddings/*)
   ```

5. **Update Story 2.3** description:
   ```markdown
   OLD:
   Story 2.3: LLM Provider (Pluggable)
   - [ ] Abstract ILlmProvider interface
   - [ ] Implement Claude adapter + OpenAI adapter
   - [ ] Config-driven provider selection
   
   NEW:
   Story 2.3: Groq LLM Provider Integration
   - [ ] Implement GroqLlmProvider (OpenAI-compatible API)
   - [ ] Rate limit handling (log 429, return 503 to FE)
   - [ ] Groq API key: appsettings + env var (Docker)
   - [ ] Fallback: Keep Ollama Gemma 2B option (separate branch, ready for standby)
   - [ ] Load test: verify 30 RPM limit, latency <1.5s p95
   ```

6. **Add new "Dependencies" section**:
   ```markdown
   ## Dependencies (Critical Path)
   
   Story 2.0 (embedding ingestion) → blocks 2.1 + 2.2
   Story 2.1 (embedding service) → blocks 2.2
   Story 2.3 (Groq LLM) → must start in parallel with 2.2
   
   Timeline: 29-41 days total (5-6 weeks for MVP)
   - Week 1: 2.0 + 2.1 + 4.1 (parallel)
   - Week 2: 2.2 + 2.3 + 4.2 + 4.3 (parallel)
   - Week 3-4: 2.5 + 4.4 (parallel)
   ```

---

## Notion Epic 4 Update Checklist

**Page**: https://app.notion.com/p/36d159daab4481ef99e7e7e460dcebc1

### Content to Update

1. **Replace Design Reference**:
   - New: Link to `R&D/option_a_architecture.md` (Image Search flow)

2. **Update key design decisions**:
   ```markdown
   OLD:
   - New microservice FishLover.ImageSearch.API
   - CLIP model batch embed pipeline
   
   NEW:
   - CLIP service: Python FastAPI on VM3 :8001 (not .NET microservice)
   - Model: CLIP ViT-B/32 (512-d vectors)
   - Backfill: 70k species images with embeddings (~4h batch job)
   - Image search query latency: <700ms p95
   - VM3 memory: CLIP (450MB) + embedding (200MB) shared, ~3.5GB total
   ```

3. **Update Story 4.1** description:
   ```markdown
   OLD:
   Story 4.1: Project Scaffolding
   - [ ] Tạo project FishLover.ImageSearch.API
   
   NEW:
   Story 4.1: CLIP Model Service on VM3
   - [ ] Python FastAPI service + OpenCLIP ViT-B/32
   - [ ] POST /search/text endpoint (text → 512-d CLIP vector)
   - [ ] POST /search/image endpoint (image file → 512-d CLIP vector)
   - [ ] Image preprocessing: resize to 224x224
   - [ ] Prometheus metrics + load test
   - [ ] Integrate with Ocelot gateway (/image-search/*)
   ```

4. **Add new Story 4.2 description**:
   ```markdown
   Story 4.2: pgvector Image Index
   - [ ] Add ClipEmbedding column to SpeciesMedia entity (512-d vector)
   - [ ] EF Core migration
   - [ ] Create HNSW index on species_media.clip_embedding
   - [ ] Backfill script: query ~70k species images, embed via CLIP service, upsert
   - [ ] Test backfill: monitor memory, latency; run off-hours (expect ~4h)
   ```

5. **Update Story 4.3** description:
   ```markdown
   OLD:
   Story 4.3: Similarity Search API
   - [ ] POST /image/search endpoint
   
   NEW:
   Story 4.3: Image Search API (FishDex Service)
   - [ ] ImageSearchService in FishDex.Domain
   - [ ] POST /fishdex/v1/search/image endpoint (text or image upload)
   - [ ] Orchestrate: embed → pgvector search → presigned URLs
   - [ ] Response: top-10 species with similarity scores
   - [ ] Load test: <700ms p95 latency
   ```

---

## Summary: What Changed

| Story | Old Focus | New Focus | Why |
|-------|-----------|-----------|-----|
| 2.0 | Embedding choice + batch upsert | Same (no change) | Still valid |
| 2.1 | .NET microservice scaffolding | Python FastAPI on VM3 | Option A uses VM3 for utility services |
| 2.2 | RAG query pipeline | RAG orchestration (AquaHome) | More specific scope |
| 2.3 | LLM abstraction layer | Groq integration | Concrete implementation, fallback ready |
| 2.4 | Redis caching | Redis caching + latency optimization | Same |
| 2.5 | FE chat widget | FE chat widget | Same |
| 2.6 | BFF proxy | BFF proxy | Same |
| 4.1 | .NET microservice scaffolding | Python FastAPI on VM3 | Option A uses VM3 |
| 4.2 | Batch embedding animals | pgvector index creation | Clearer responsibility split |
| 4.3 | Image search API | Image search API (broader) | Same |
| 4.4 | FE image upload UI | FE image search UI | Same |

---

## Notion Table View Update

In **"By Release"** view, Epic 2 + Epic 4 should show:

```
Release: v2.0 (AI)
├─ Epic 2: AI Q&A Service
│  ├─ Story 2.0: Embedding Ingestion (3-4 days, blocker)
│  ├─ Story 2.1: Embedding Service on VM3 (4-5 days, blocker)
│  ├─ Story 2.2: RAG Pipeline (4-5 days, after 2.1)
│  ├─ Story 2.3: Groq LLM Integration (2-3 days, parallel with 2.2)
│  ├─ Story 2.4: Caching (1-2 days)
│  ├─ Story 2.5: FE Chat Widget (3-4 days, after 2.2)
│  └─ Story 2.6: BFF Proxy (included in 2.2)
│
├─ Epic 4: Image Search Service
│  ├─ Story 4.1: CLIP Service on VM3 (4-5 days, parallel with 2.1)
│  ├─ Story 4.2: pgvector Image Index (2-3 days, after 4.1 backfill)
│  ├─ Story 4.3: Image Search API (3-4 days, after 4.2)
│  └─ Story 4.4: FE Image Search UI (2-3 days, after 4.3)
│
└─ Total: 29-41 days (5-6 weeks MVP)
```

---

## How to Apply

1. Open Epic 2 page (link above)
2. Update title (optional: add "Groq + VM3" suffix)
3. Rewrite "Key design decisions" section
4. Update each Story description (copy-paste from table above)
5. Repeat for Epic 4
6. Create child pages for each Story (with Acceptance Criteria from breakdown)
7. Link R&D docs at top of each epic

💡 **Automation**: Could use Claude + Notion API to batch update, but manual is safer to preserve existing comments/discussions.

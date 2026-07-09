# Option A — Architecture & Flow Diagrams

## System Architecture (Option A)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           FrontEnd (React 19)                              │
│                         Cloudflare Pages :443                              │
└──────────────────────────────┬──────────────────────────────────────────────┘
                               │ HTTPS: api.fishlover.org
                               ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                   🔷 Ocelot API Gateway (VM1)                              │
│                         .NET 9 :5000                                       │
│                                                                            │
│  Routes:  /user/v1/** → UserManagement :8080                             │
│           /fishdex/v1/** → FishDex :8081                                 │
│           /aquahome/v1/** → AquaHome :8082                               │
│           /ai/** → AquaHome :8082/api/ai (BFF proxy)                     │
│           /embeddings/** → VM3 :8000 (new)                               │
│           /image-search/** → VM3 :8001 (new)                             │
└──────────────────────────────┬──────────────────────────────────────────────┘
                               │
        ┌──────────────────────┼──────────────────────┐
        │                      │                      │
        ▼                      ▼                      ▼
   ┌─────────────┐        ┌──────────┐         ┌────────────┐
   │UserManage   │        │FishDex   │         │AquaHome    │
   │:8080        │        │:8081     │         │:8082       │
   │.NET 9       │        │.NET 9    │         │.NET 9      │
   └──────┬──────┘        └────┬─────┘         └─────┬──────┘
          │                    │                     │
          │                    │        ┌────────────┼────────────┐
          │                    │        │            │            │
          ▼                    ▼        ▼            ▼            ▼
   ┌───────────────────────────────────────────────────────────────────┐
   │         PostgreSQL 16 + pgvector (VM1)  :5432                   │
   │  ├─ usermanagement_db (users, roles, invites)                   │
   │  ├─ fishdex_db (species, media, embeddings, chunks)             │
   │  └─ aquahome_db (aquariums, fish, quotas)                       │
   └───────────────────────────────────────────────────────────────────┘
        ▲
        │
        └─── pgvector queries (nearest neighbor search for RAG)

                                ★ NEW SERVICES (VM3) ★

┌─────────────────────────────────────────────────────────────────────────────┐
│                    🔷 VM3 (6GB RAM) — AI Services                           │
│                         Oracle ARM64                                         │
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │  Service 1: Embedding Service :8000 (Python FastAPI)              │   │
│  │  ├─ Model: all-MiniLM-L6-v2 (22.7M params, ~120MB)                │   │
│  │  ├─ Endpoint: POST /embed                                         │   │
│  │  │         Input: {"text": "..."}                                 │   │
│  │  │         Output: {"embedding": [0.1, 0.2, ...], shape: 384}    │   │
│  │  ├─ Latency: ~30-50ms per request (CPU, batch=32)                │   │
│  │  ├─ Memory: ~200MB at runtime (loaded once)                      │   │
│  │  └─ Scaling: Process embedding requests in batches               │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │  Service 2: Image Search Service :8001 (Python FastAPI)           │   │
│  │  ├─ Model: CLIP ViT-B/32 (151M params total, ~338MB)             │   │
│  │  │         Vision encoder: ViT-B/32, Text encoder: 512-d         │   │
│  │  ├─ Endpoints:                                                    │   │
│  │  │    POST /search/text → embed query text                       │   │
│  │  │    POST /search/image → embed image bytes (Vision encoder)    │   │
│  │  ├─ Latency:                                                      │   │
│  │  │    Text embedding: ~40ms                                       │   │
│  │  │    Image embedding: ~150-200ms (Vision encoder)               │   │
│  │  ├─ Memory: ~450MB at runtime (loaded once)                      │   │
│  │  └─ Output: CLIP embeddings (512-d vectors)                      │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │  Orchestration                                                      │   │
│  │  ├─ Both services auto-load models on startup (~2s)               │   │
│  │  ├─ Total runtime memory: ~2.5-3 GB (models + inference)          │   │
│  │  ├─ Graceful degradation: Service 1 fails → image search offline  │   │
│  │  └─ Monitoring: Prometheus metrics (latency, errors, OOM)         │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                    🔷 External LLM API (Groq)                               │
│                                                                              │
│  ├─ Endpoint: https://api.groq.com/openai/v1/chat/completions            │
│  ├─ Model: llama-3.1-70b-versatile (or other variants)                   │
│  ├─ Rate Limit: 30 req/min, 6k tokens/min, 14.4k requests/day            │
│  ├─ Latency: ~500-1000ms (depends on prompt size)                        │
│  ├─ Cost: FREE (rate-limited, no charge)                                 │
│  └─ Fallback: Google Gemini 3 Flash (10 RPM, 1.5k req/day)              │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                  🔷 External Storage (Cloudflare R2)                        │
│                                                                              │
│  ├─ Purpose: Store FishBase species images (already in use)                │
│  ├─ Free Tier: 10GB/month (per FishDex:HandleGetPresignedUrlAsync)        │
│  └─ CLIP Image Search: Pulls presigned URLs from R2                       │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Data Flow: RAG Pipeline (Epic 2)

```
User Query (Web)
    │
    ├─ "What fish can live in 23°C water?"
    │
    ▼
┌─────────────────────────────┐
│  AquaHome BFF (/ai/chat)    │  • Validate user aquarium ownership
│  :8082                      │  • Extract water params from aquarium DB
└──────────┬──────────────────┘
           │
           ├─ 1. Call Embedding Service (VM3 :8000)
           │      POST /embed {"text": "What fish can live in 23°C water?"}
           │
           ▼
    ┌─────────────────────┐
    │ VM3 :8000           │ all-MiniLM-L6-v2
    │ Embedding Service   │ Produces: 384-d vector
    │                     │ Latency: 30-50ms
    └────────────┬────────┘
                 │ Returns embedding
                 │
                 ├─ 2. Query PostgreSQL pgvector (VM1)
                 │      SELECT species_chunks, distance
                 │      FROM species_chunks
                 │      WHERE species_chunks.embedding <-> ? < 0.5
                 │      ORDER BY distance
                 │      LIMIT 5
                 │
                 ▼
    ┌────────────────────────────┐
    │ PostgreSQL pgvector (VM1)  │ • Semantic search top-5 chunks
    │ :5432                      │ • Returns: metadata + text chunks
    │ fishdex_db                 │ • Latency: 50-100ms
    └────────────┬───────────────┘
                 │ Returns top-5 species chunks + metadata
                 │
                 ├─ 3. Construct RAG prompt:
                 │      System: "You are an aquarium expert..."
                 │      Context: [species_chunk_1, species_chunk_2, ...]
                 │      User Query: "What fish can live in 23°C?"
                 │
                 ├─ 4. Call Groq API (external)
                 │      POST https://api.groq.com/openai/v1/chat/completions
                 │      Model: "llama-3.1-70b-versatile"
                 │      Max tokens: 512
                 │
                 ▼
    ┌──────────────────────────┐
    │ Groq API (Rate-limited)  │ • Process RAG prompt
    │ https://api.groq.com     │ • Generate answer
    │ llama-3.1-70b            │ • Latency: 500-1000ms
    └────────────┬─────────────┘
                 │ Returns generated answer
                 │
                 ├─ 5. Stream response to FE
                 │      - Cache in Redis (optional, for repeated queries)
                 │      - Log to Grafana (metrics + latency tracking)
                 │
                 ▼
    ┌──────────────────────┐
    │  FrontEnd Chat UI    │ Display answer to user
    └──────────────────────┘

Total Latency Breakdown:
├─ Embedding (VM3):   30-50ms
├─ pgvector search:   50-100ms
├─ Groq LLM:          500-1000ms (dominant)
├─ Network overhead:  100-200ms
└─ TOTAL:             ~700-1350ms per query (acceptable for chat UX)
```

---

## Data Flow: Image Search Pipeline (Epic 4)

```
User Input (FE)
    │
    ├─ Image file (JPG/PNG) or text query
    │
    ▼
┌──────────────────────────────┐
│  FishDex Image Search API    │  POST /fishdex/v1/search/image
│  :8081                       │  or /fishdex/v1/search/text
└──────────┬───────────────────┘
           │
           ├─ Case 1: User uploads image
           │
           ├─ 1. Call Image Search Service (VM3 :8001)
           │      POST /search/image
           │      Content-Type: multipart/form-data
           │      File: [image bytes]
           │
           ▼
    ┌─────────────────────┐
    │ VM3 :8001           │ CLIP ViT-B/32 Vision encoder
    │ Image Search        │ Input: image → [512-d vector]
    │                     │ Latency: 150-200ms (Vision)
    └────────────┬────────┘
                 │
                 ├─ Case 2: User enters text query
                 │
                 ├─ 1. Call Image Search Service (VM3 :8001)
                 │      POST /search/text
                 │      {"text": "yellow fish with stripes"}
                 │
                 ▼
    ┌─────────────────────┐
    │ VM3 :8001           │ CLIP ViT-B/32 Text encoder
    │ Image Search        │ Input: text → [512-d vector]
    │                     │ Latency: 40-50ms (Text)
    └────────────┬────────┘
                 │
                 ├─ 2. Query PostgreSQL pgvector
                 │      SELECT species, media_url, distance
                 │      FROM species_media_embeddings
                 │      WHERE embeddings <-> ? < 0.3
                 │      ORDER BY distance
                 │      LIMIT 10
                 │
                 ▼
    ┌────────────────────────────┐
    │ PostgreSQL pgvector (VM1)  │ CLIP-space semantic search
    │ :5432                      │ Returns: top-10 species images
    │ fishdex_db                 │ Latency: 50-100ms
    └────────────┬───────────────┘
                 │
                 ├─ 3. Generate presigned URLs (from R2)
                 │      For each result:
                 │      Call S3StorageService.GetPresignedUrlAsync()
                 │      Use: pic.ObjectKey = {SpecCode}/{Id}{ext}
                 │
                 ▼
    ┌──────────────────────────┐
    │ Cloudflare R2            │ Pre-signed URLs (1-hour TTL)
    │ S3-compatible Object     │ FE can download images directly
    │ Storage                  │ No server-side image serving
    └────────────┬─────────────┘
                 │
                 ├─ 4. Return results to FE
                 │      [{
                 │        species_id: "...",
                 │        species_name: "...",
                 │        image_url: "https://...[presigned]",
                 │        similarity_score: 0.92,
                 │        source: "FishBase"
                 │      }, ...]
                 │
                 ▼
    ┌──────────────────────┐
    │  FrontEnd Grid View  │ Display image search results
    └──────────────────────┘

Total Latency Breakdown:
├─ Image embedding (VM3):    150-200ms (image) or 40-50ms (text)
├─ pgvector search:           50-100ms
├─ Generate presigned URLs:   100-200ms (batch R2 API calls)
├─ Network overhead:          100-150ms
└─ TOTAL:                     ~400-650ms per search (good UX)
```

---

## Service Deployment Map

| Service | Port | VM | Tech | Startup | Memory | Scaling |
|---------|------|-----|------|---------|--------|---------|
| Ocelot Gateway | 5000 | VM1 | .NET 9 | 2s | 300MB | Stateless |
| UserManagement | 8080 | VM1 | .NET 9 | 3s | 400MB | Stateless |
| FishDex | 8081 | VM1 | .NET 9 | 3s | 500MB | Stateless |
| AquaHome | 8082 | VM1 | .NET 9 | 3s | 500MB | Stateless |
| **Embedding Svc** | **8000** | **VM3** | **Python FastAPI** | **~2s** | **~200MB (loaded)** | **Batch inference** |
| **Image Search Svc** | **8001** | **VM3** | **Python FastAPI** | **~2s** | **~450MB (loaded)** | **Batch inference** |

---

## Risk & Mitigation

### VM3 Memory Pressure

| Scenario | Impact | Mitigation |
|----------|--------|-----------|
| OOM on embedding service | 🔴 High (embedding offline, RAG broken) | Monitor RSS in Grafana; auto-restart if >80% |
| OOM on image search | 🟠 Medium (image search offline) | Independent process; auto-restart doesn't affect embedding |
| Concurrent spike (both models + inference) | 🟡 Low | Models load separately; inference queue prevents overload |

### Groq Rate Limit

| Scenario | Impact | Mitigation |
|----------|--------|-----------|
| Hit 30 RPM limit | 🟠 Medium (429 returned, UX degrades) | Add queue + retry in BFF; monitor dashboard |
| Groq API outage | 🔴 High (RAG offline) | Keep Option B (Ollama) ready; can switch in 1 week |
| Query too large (>6k tokens) | 🟡 Low | Truncate context chunks; enforce max chunk size |

### Image Search Scaling

| Issue | Solution |
|-------|----------|
| CLIP inference bottleneck (200ms per image) | Batch requests (e.g., 32 images/batch) or queue model |
| Large image files (e.g., 10MB upload) | Resize in FE before upload; max 5MB server-side |
| R2 presigned URL generation | Parallel batch generation using Task.WhenAll() |

---

## Summary

**Option A is safe & scalable for MVP:**
- ✅ No surprise charges (Groq returns 429, doesn't bill)
- ✅ VM3 has breathing room (2.5GB headroom)
- ✅ Latency acceptable for chat UX (~700-1350ms)
- ✅ Image search fast (~400-650ms)
- ⚠️ Groq rate limit (30 RPM) is constraint; monitor usage
- ⚠️ Fallback to Ollama ready if needed

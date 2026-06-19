# Option A — Story & Task Breakdown

## Overview

**Epic 2 (RAG Q&A)** + **Epic 4 (Image Search)** split into 10 stories.
- **Phase**: v2.0 (AI) — starts after v1.0 foundation
- **Constraint**: VM3 (6GB RAM) must host embedding + image search services
- **External**: Groq API (free, rate-limited) for LLM inference

---

## ⚡ Critical Path (Dependency Chain)

```
Foundation (v1.0 done)
    ↓
Story 2.0: Embedding Ingestion ← Prerequisite (populates pgvector)
    ↓
Story 2.1: Embedding Service (VM3) ← Must exist before RAG
    ↓
Story 4.1: CLIP Model Service (VM3) ← Parallel with Story 2.1
    ↓
Story 2.2: RAG Pipeline ← Depends on 2.1 + Groq setup
    ↓
Story 4.2: pgvector Image Index ← Parallel with 2.2
    ↓
Story 2.3: Groq LLM Provider ← Parallel with 2.2
    ↓
Story 4.3: Image Search API ← Depends on 4.1 + 4.2
    ↓
Story 2.5: FE Chat Widget + Story 4.4: FE Image Search ← Final UI layer
```

---

## Epic 2 — AI Q&A Service (v2.0)

### Story 2.0: Embedding Data Ingestion ⭐ (PREREQUISITE)

**Objective**: Populate `species_chunks` table with embeddings using `all-MiniLM-L6-v2`.

**Scope**:
- Read FishBase preprocessor output (8,883 chunks JSONL from v1.0)
- Batch embed via `all-MiniLM-L6-v2` (384-d vectors)
- Upsert into PostgreSQL `species_chunks` table with `pgvector` column
- Build HNSW index for semantic search

**Deliverables**:
1. Script: `BackEndProject/FishDex/FishDex.EFCore/Scripts/seed_embeddings.py` (Python)
   - Reads `Pipeline/data/species_chunks.jsonl`
   - Batch embeds (32 chunks/batch) via `sentence-transformers`
   - Inserts via EF Core bulk insert or raw SQL
   - Logs progress + errors to Grafana

2. EF Core migration: Add `embedding` column (vector type) to `SpeciesChunk` entity

3. PostgreSQL index:
   ```sql
   CREATE INDEX ON species_chunks USING hnsw (embedding vector_cosine_ops);
   ```

**Tasks**:
- [ ] Create `SpeciesChunk.Embedding` property (nullable vector, 384 dims)
- [ ] Write EF migration
- [ ] Write Python seed script (with batch processing + error handling)
- [ ] Test locally: embed 100 samples → check quality in pgvector
- [ ] Run in Docker: embed all 8,883 chunks (~5-10 min)
- [ ] Verify HNSW index built + search performance (<100ms per query)

**Acceptance Criteria**:
- ✅ All 8,883 chunks have embeddings in PostgreSQL
- ✅ HNSW index present + functional
- ✅ Semantic search test: query "aquarium 23°C fish" → returns relevant species chunks
- ✅ pgvector distance metric: `<->` (cosine distance) < 0.5 for relevant results

**Estimated**: 3-4 days (most time on testing + index tuning)

---

### Story 2.1: Embedding Service on VM3 ⭐ (BLOCKER)

**Objective**: Standalone HTTP service wrapping `all-MiniLM-L6-v2` for on-demand embeddings.

**Architecture**:
```
Client → API Gateway :5000/embeddings/embed → VM3 :8000 (FastAPI)
```

**Scope**:
- Python FastAPI service with `sentence-transformers` model
- Endpoint: `POST /embed` — takes text, returns 384-d vector
- Batch processing: Queue multiple requests, process in batches (32)
- Memory: ~200MB at runtime (model loaded once)
- No persistence (stateless)

**Deliverables**:
1. Directory: `Pipeline/vm3/embedding_service/`
   ```
   ├─ main.py                    # FastAPI app
   ├─ requirements.txt           # sentence-transformers, fastapi, uvicorn
   ├─ docker/Dockerfile          # Python 3.11 slim, arm64-compatible
   ├─ docker-compose.yml         # Expose :8000
   └─ test/test_embedding.py     # Unit tests
   ```

2. FastAPI endpoints:
   ```python
   POST /health → {"status": "ok", "model": "all-MiniLM-L6-v2", "memory_mb": 200}
   POST /embed
     Request: {"text": "What is a goldfish?", "normalize": true}
     Response: {"embedding": [0.1, 0.2, ...], "shape": 384, "latency_ms": 42}
   POST /batch-embed
     Request: {"texts": ["...", "...", ...]}
     Response: {"embeddings": [[...], [...], ...], "latency_ms": 150}
   ```

3. Ocelot route (in ApiGateway):
   ```json
   {
     "DownstreamPathTemplate": "/api/v1/embeddings/{everything}",
     "DownstreamScheme": "http",
     "DownstreamHostAndPorts": [{"Host": "vm3", "Port": 8000}],
     "UpstreamPathTemplate": "/embeddings/{everything}"
   }
   ```

**Tasks**:
- [ ] Init FastAPI project + requirements.txt
- [ ] Load model at startup (measure latency + memory)
- [ ] Implement `/embed` + `/batch-embed` endpoints
- [ ] Add request validation (max text length 2000 chars)
- [ ] Add response normalization (L2 norm optional)
- [ ] Prometheus metrics: request count, latency histogram, errors
- [ ] Unit tests (fixtures for model)
- [ ] Dockerfile (multi-stage, optimize for ARM64)
- [ ] docker-compose.yml (expose :8000 internally)
- [ ] Load test: 100 concurrent /embed requests → measure latency + memory
- [ ] Add to Ocelot routes (gateway :5000/embeddings/*)

**Acceptance Criteria**:
- ✅ Service starts in <2s, loads model in <1.5s
- ✅ Single /embed request: <100ms latency (p95)
- ✅ Memory stable: ~200MB after first request
- ✅ 100 concurrent requests: <2s total, no OOM
- ✅ Docker image: <2GB (arm64), runs on VM3 OK
- ✅ Prometheus metrics visible in Grafana

**Estimated**: 4-5 days (most time on performance tuning + testing)

---

### Story 2.2: RAG Pipeline Integration ⭐

**Objective**: AquaHome BFF orchestrates embedding → pgvector search → Groq LLM.

**Architecture**:
```
FE Chat Query → AquaHome :8082/api/ai/chat
    ↓
1. Call VM3 :8000/embed (embed user query)
2. Query PostgreSQL pgvector (top-5 species chunks)
3. Construct RAG prompt (system + context + user query)
4. Call Groq API (llama-3.1-70b-versatile)
5. Stream response to FE (or return complete)
```

**Scope**:
- Service: `AquaHome/AquaHome.Domain/Services/AIRagService.cs`
- HTTP client: Call VM3 embedding service
- Groq client: Call Groq API (OpenAI-compatible)
- Prompt engineering: System prompt + context retrieval + user query
- Response streaming (optional, v2.1+)

**Deliverables**:
1. Domain service: `AIRagService`
   ```csharp
   public class AIRagService {
       private readonly IHttpClientFactory _httpClientFactory;
       private readonly IGroqApiClient _groqClient;
       private readonly AppDbContext _dbContext;
       private readonly ICurrentUserSession _userSession;

       public async Task<string> ChatAsync(string userQuery, Guid aquariumId)
           → Validate aquarium ownership
           → Embed query (call VM3)
           → Search pgvector (top-5)
           → Build context prompt
           → Call Groq API
           → Return response

       private string BuildRagPrompt(List<SpeciesChunk> chunks, string query)
   }
   ```

2. HTTP client wrapper: `Infrastructure/ExternalServices/GroqApiClient.cs`
   ```csharp
   public interface IGroqApiClient {
       Task<ChatCompletionResponse> CreateChatCompletionAsync(
           List<ChatMessage> messages, 
           string model = "llama-3.1-70b-versatile",
           int maxTokens = 512
       );
   }
   ```

3. Controller: `AquaHome.API/Controllers/AiController.cs`
   ```csharp
   [HttpPost("api/ai/chat")]
   [Authorize]
   public async Task<IActionResult> ChatAsync([FromBody] ChatRequest req)
       → req.aquarium_id, req.query
       → call AIRagService.ChatAsync()
       → return ChatResponse { response, latency_ms, sources }
   ```

4. Configuration:
   - `appsettings.json`:
     ```json
     {
       "ExternalServices": {
         "EmbeddingService": {"Url": "http://vm3:8000"},
         "GroqApi": {"Url": "https://api.groq.com", "ApiKey": "..."}
       }
     }
     ```

5. Prompt template (in Domain/Helper/PromptTemplates.cs):
   ```
   System: "You are an expert aquarium consultant. Answer based on provided fish data."
   Context: [species_chunk_1] ... [species_chunk_5]
   User: "{query}"
   ```

**Tasks**:
- [ ] Create IGroqApiClient interface + mock implementation
- [ ] Implement GroqApiClient (OpenAI-compatible SDK)
- [ ] Create AIRagService with orchestration logic
- [ ] Design/test prompt template (quality tuning)
- [ ] Add AiController endpoint
- [ ] Add configuration to appsettings.json + appsettings.Docker.json
- [ ] DI wiring in AquaHome module (Autofac)
- [ ] Request validation (max query length, aquarium ownership)
- [ ] Error handling: embedding service down, Groq API timeout, rate limit
- [ ] Logging: log all calls to Grafana (latency, tokens used)
- [ ] Unit tests: mock embedding service + Groq API
- [ ] Integration test: end-to-end with Docker containers

**Acceptance Criteria**:
- ✅ E2E test: user query → response in <2s (p95 latency)
- ✅ Top-5 species chunks are relevant (manual quality check)
- ✅ Groq response is sensible (manual inspection)
- ✅ Handles embedding service down: returns 503
- ✅ Handles Groq API down: returns 503 + optional fallback
- ✅ Rate limit graceful: returns 429 with retry header
- ✅ All latency metrics logged to Grafana

**Estimated**: 4-5 days (most time on prompt tuning + error handling)

---

### Story 2.3: Groq LLM Provider Integration

**Objective**: Integrate Groq API as primary LLM, with fallback to Gemini.

**Scope**:
- Groq API setup (get free API key, set rate limits in dashboard)
- Implement `ILlmProvider` interface (strategy pattern for swap)
- Primary: Groq (fast, free, 30 RPM)
- Fallback: Google Gemini 3 Flash (if Groq 429 or timeout)
- Request/response mapping to OpenAI-compatible format

**Deliverables**:
1. Interface: `Domain/Services/ILlmProvider.cs`
   ```csharp
   public interface ILlmProvider {
       Task<string> GenerateAsync(List<ChatMessage> messages, int maxTokens);
       string Name { get; } // "groq" | "gemini"
   }
   ```

2. Implementation: `Infrastructure/ExternalServices/GroqLlmProvider.cs`
   ```csharp
   public class GroqLlmProvider : ILlmProvider {
       private readonly HttpClient _httpClient;
       private readonly string _apiKey;
       
       public async Task<string> GenerateAsync(List<ChatMessage> messages, int maxTokens) {
           var request = new {
               model = "llama-3.1-70b-versatile",
               messages = messages.Select(m => new { role = m.Role, content = m.Content }),
               max_tokens = maxTokens
           };
           var resp = await _httpClient.PostAsJsonAsync("https://api.groq.com/openai/v1/chat/completions", request);
           // Parse + return
       }
   }
   ```

3. Fallback wrapper: `Infrastructure/ExternalServices/ResilientLlmProvider.cs`
   ```csharp
   public class ResilientLlmProvider : ILlmProvider {
       private readonly GroqLlmProvider _groq;
       private readonly GeminiLlmProvider _gemini;
       
       public async Task<string> GenerateAsync(...) {
           try {
               return await _groq.GenerateAsync(...);
           } catch (HttpRequestException ex) when (ex.StatusCode == 429 || ex.StatusCode == 500) {
               // Fall back to Gemini
               return await _gemini.GenerateAsync(...);
           }
       }
   }
   ```

**Tasks**:
- [ ] Create ILlmProvider interface
- [ ] Implement GroqLlmProvider
- [ ] Implement GeminiLlmProvider (optional fallback)
- [ ] Create ResilientLlmProvider wrapper
- [ ] Add Groq API key to appsettings + Key Vault (prod)
- [ ] Configure in Autofac (bind ResilientLlmProvider as default)
- [ ] Rate limit monitoring: log requests/minute to Grafana
- [ ] Circuit breaker: auto-switch to fallback if Groq 429 threshold hit
- [ ] Unit tests: mock HTTP responses
- [ ] Integration test: actual Groq API call (costs ~$0 in free tier)

**Acceptance Criteria**:
- ✅ Groq API key configured + request succeeds
- ✅ Rate limit test: 31 consecutive requests → 1 gets 429 (expected)
- ✅ Fallback test: simulate Groq 500 → uses Gemini
- ✅ Circuit breaker: after 3 consecutive 429s, auto-switch to Gemini
- ✅ Latency logged (Groq ~500-1000ms, Gemini ~1-2s)

**Estimated**: 2-3 days

---

### Story 2.4: Caching & Response Optimization

**Objective**: Cache RAG responses for repeated queries, optimize token usage.

**Scope**:
- Redis cache for chat responses (key: hash of user query + aquarium ID)
- TTL: 24 hours (since fish data doesn't change often)
- Token optimization: truncate context chunks if total > 2k tokens
- Latency reduction: return cached response in <10ms

**Deliverables**:
1. Cache service: `Infrastructure/Caching/RagResponseCache.cs`
   ```csharp
   public class RagResponseCache {
       private readonly IDistributedCache _cache;
       
       public async Task<string?> GetAsync(string query, Guid aquariumId) {
           var key = $"rag:query:{MD5(query)}:{aquariumId}";
           return await _cache.GetStringAsync(key);
       }
       
       public async Task SetAsync(string query, Guid aquariumId, string response) {
           var key = $"rag:query:{MD5(query)}:{aquariumId}";
           await _cache.SetStringAsync(key, response, new DistributedCacheEntryOptions {
               AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
           });
       }
   }
   ```

2. Cache hit logging: log cache hits/misses to Grafana

**Tasks**:
- [ ] Design cache key strategy
- [ ] Implement RagResponseCache
- [ ] DI wiring (inject into AIRagService)
- [ ] Check cache before embedding/Groq calls
- [ ] Unit tests: cache hit/miss, TTL expiration
- [ ] Load test: cache hit latency <20ms

**Acceptance Criteria**:
- ✅ Repeated query returns from cache in <20ms
- ✅ Cache misses trigger full pipeline
- ✅ Cache invalidation after 24h
- ✅ Manual cache clear endpoint (admin only) for testing

**Estimated**: 1-2 days

---

### Story 2.5: Frontend Chat Widget

**Objective**: Add chat UI component to AquaHome FE for RAG interaction.

**Scope**:
- Chat component: message history, user input, streaming response
- Integration with `/api/ai/chat` endpoint
- Mobile-first (iPhone 12+ 390px)
- Loading state, error messages, retry logic

**Deliverables**:
1. Component: `FrontEnd/src/components/AiChatWidget.tsx`
   ```tsx
   export const AiChatWidget: React.FC<{ aquariumId: string }> = ({ aquariumId }) => {
       const [messages, setMessages] = useState<ChatMessage[]>([]);
       const [input, setInput] = useState("");
       const [loading, setLoading] = useState(false);
       
       const sendMessage = async (query: string) => {
           setLoading(true);
           try {
               const resp = await fetch(`${VITE_GATEWAY_URL}/api/ai/chat`, {
                   method: "POST",
                   body: JSON.stringify({ aquarium_id: aquariumId, query })
               });
               const data = await resp.json();
               setMessages([...messages, { role: "assistant", content: data.response }]);
           } finally {
               setLoading(false);
           }
       };
       
       return (
           <div className="chat-widget">
               <div className="messages">{messages.map(m => ...)}</div>
               <input value={input} onChange={e => setInput(e.target.value)} />
               <button onClick={() => sendMessage(input)}>Send</button>
           </div>
       );
   };
   ```

2. Styles: Mobile responsive, dark mode optional

**Tasks**:
- [ ] Create AiChatWidget component
- [ ] Add chat UI library (e.g., react-chat-window or custom)
- [ ] Integrate with /api/ai/chat endpoint
- [ ] Error handling (network error, server error)
- [ ] Retry logic (if Groq rate limit, show message)
- [ ] Loading skeleton / spinner
- [ ] Responsive design (mobile-first, 390px+)
- [ ] Accessibility (ARIA labels, keyboard navigation)
- [ ] Unit tests (mocked API)
- [ ] E2E test (with Docker backend)

**Acceptance Criteria**:
- ✅ Chat UI visible in AquaHome aquarium detail page
- ✅ Send message → get response in <2s
- ✅ Message history persists during session
- ✅ Mobile responsive on iPhone 12 (390px)
- ✅ Error messages clear (rate limit, network, etc.)
- ✅ Accessibility: keyboard navigation works

**Estimated**: 3-4 days (most time on UX/mobile responsiveness)

---

## Epic 4 — Image Search Service (v2.0)

### Story 4.1: CLIP Model Integration on VM3 ⭐ (BLOCKER)

**Objective**: Standalone HTTP service wrapping CLIP ViT-B/32 for image/text embedding.

**Architecture**:
```
Client → API Gateway :5000/image-search/* → VM3 :8001 (FastAPI)
```

**Scope**:
- Python FastAPI service with `clip-ViT-B-32` model
- Two endpoints: `/search/text` (embed text) + `/search/image` (embed image)
- Image handling: resize to 224x224, convert to tensor
- Memory: ~450MB at runtime (model loaded once)
- No persistence (stateless)

**Deliverables**:
1. Directory: `Pipeline/vm3/image_search_service/`
   ```
   ├─ main.py                    # FastAPI app, CLIP loader
   ├─ requirements.txt           # clip, fastapi, pillow, uvicorn
   ├─ docker/Dockerfile          # Python 3.11, arm64-compatible
   ├─ docker-compose.yml         # Expose :8001
   └─ test/test_clip.py          # Unit tests
   ```

2. FastAPI endpoints:
   ```python
   POST /health → {"status": "ok", "model": "clip-ViT-B/32", "memory_mb": 450}
   POST /search/text
     Request: {"text": "yellow fish with stripes"}
     Response: {"embedding": [0.1, 0.2, ...], "shape": 512, "latency_ms": 45}
   POST /search/image
     Request: form-data { "file": <image.jpg> }
     Response: {"embedding": [0.1, 0.2, ...], "shape": 512, "latency_ms": 180}
   ```

3. Ocelot route:
   ```json
   {
     "DownstreamPathTemplate": "/api/v1/image-search/{everything}",
     "DownstreamScheme": "http",
     "DownstreamHostAndPorts": [{"Host": "vm3", "Port": 8001}],
     "UpstreamPathTemplate": "/image-search/{everything}"
   }
   ```

**Tasks**:
- [ ] Init FastAPI project
- [ ] Load CLIP model at startup + measure latency
- [ ] Implement `/search/text` endpoint
- [ ] Implement `/search/image` endpoint (handle file upload, validate size)
- [ ] Image preprocessing: resize to 224x224, normalize
- [ ] Request validation: max file size 5MB, allowed formats (jpg/png/webp)
- [ ] Prometheus metrics: request count, latency histogram, errors
- [ ] Unit tests (fixtures for model, test images)
- [ ] Dockerfile (multi-stage, optimize for ARM64)
- [ ] docker-compose.yml (expose :8001)
- [ ] Load test: 100 concurrent /search/image requests
- [ ] Add to Ocelot routes

**Acceptance Criteria**:
- ✅ Service starts in <2s, loads model in <1.5s
- ✅ Text embedding: <100ms latency (p95)
- ✅ Image embedding: <300ms latency (p95)
- ✅ Memory stable: ~450MB after first request
- ✅ Docker image: <1GB (arm64)
- ✅ 100 concurrent image uploads: no OOM, <5s total

**Estimated**: 4-5 days

---

### Story 4.2: pgvector Index for Species Images

**Objective**: Create HNSW index on species media embeddings for image search.

**Scope**:
- Add `clip_embedding` column to `SpeciesMedia` entity (nullable vector, 512 dims)
- EF Core migration
- PostgreSQL HNSW index
- Backfill embeddings for existing images (using CLIP service)

**Deliverables**:
1. Entity update: `FishDex/FishDex.EFCore/Entities/Media/SpeciesMedia.cs`
   ```csharp
   public class SpeciesMedia {
       // ... existing
       [Column(TypeName = "vector(512)")]
       public Vector? ClipEmbedding { get; set; } // for image search
   }
   ```

2. EF migration (auto-generated)

3. PostgreSQL index:
   ```sql
   CREATE INDEX ON species_media USING hnsw (clip_embedding vector_cosine_ops);
   ```

4. Backfill script: `FishDex/FishDex.EFCore/Scripts/backfill_image_embeddings.py`
   - Query all SpeciesMedia rows
   - Download image from R2 (presigned URL)
   - Embed via CLIP service (VM3 :8001)
   - Upsert embedding
   - Log progress

**Tasks**:
- [ ] Create entity migration (add ClipEmbedding column)
- [ ] Run migration locally + Docker
- [ ] Write backfill script
- [ ] Test backfill: 10 images locally → check quality
- [ ] Run backfill on all images (~35k species × avg 2 images = ~70k total)
  - Estimate: 70k images × 200ms/image = ~4 hours (run batch job)
- [ ] Create HNSW index
- [ ] Test search: query "yellow fish" → returns relevant images

**Acceptance Criteria**:
- ✅ All species media have ClipEmbedding
- ✅ HNSW index present + functional
- ✅ Semantic search test: "tropical fish" → returns relevant images
- ✅ Index query latency <100ms

**Estimated**: 2-3 days (mostly waiting for backfill to complete)

---

### Story 4.3: Image Search API (FishDex Service)

**Objective**: FishDex API exposes image search endpoint (text query + image upload).

**Architecture**:
```
FE → API Gateway /fishdex/v1/search/image → FishDex :8081
  ↓
FishDex orchestrates:
  1. Call VM3 :8001 to embed query (text or image)
  2. Query PostgreSQL pgvector (top-10 species media)
  3. Generate presigned URLs for images
  4. Return results
```

**Scope**:
- Service: `FishDex/FishDex.Domain/Services/ImageSearchService.cs`
- Controller: `FishDex/FishDex.API/Controllers/SearchController.cs`
- Support: text query, image file upload, combined filters (genus, ecosystem)

**Deliverables**:
1. Service:
   ```csharp
   public class ImageSearchService {
       private readonly IClipApiClient _clipClient;
       private readonly AppDbContext _dbContext;
       private readonly IStorageService _storage;
       
       public async Task<List<ImageSearchResult>> SearchAsync(
           string? textQuery, 
           IFormFile? imageFile,
           int limit = 10
       ) {
           // 1. Get CLIP embedding
           Vector embedding = textQuery != null
               ? await _clipClient.EmbedTextAsync(textQuery)
               : await _clipClient.EmbedImageAsync(imageFile.OpenReadStream());
           
           // 2. Search pgvector
           var results = await _dbContext.SpeciesMedia
               .OrderBy(m => EF.Functions.VectorDistance(m.ClipEmbedding, embedding))
               .Take(limit)
               .Select(m => new ImageSearchResult {
                   SpeciesId = m.Species.Id,
                   SpeciesName = m.Species.CommonName,
                   ImageUrl = _storage.GetPresignedUrl(m.ObjectKey),
                   SimilarityScore = 1 - EF.Functions.VectorDistance(...),
                   Source = "FishBase"
               })
               .ToListAsync();
           
           return results;
       }
   }
   ```

2. Controller:
   ```csharp
   [HttpPost("search/image")]
   public async Task<IActionResult> SearchImageAsync(
       [FromQuery] string? query,
       [FromForm] IFormFile? image,
       [FromQuery] int limit = 10
   ) {
       if (query == null && image == null) return BadRequest("Provide query or image");
       var results = await _imageSearchService.SearchAsync(query, image, limit);
       return Ok(results);
   }
   ```

3. Response DTO:
   ```csharp
   public class ImageSearchResult {
       public Guid SpeciesId { get; set; }
       public string SpeciesName { get; set; }
       public string ImageUrl { get; set; }
       public double SimilarityScore { get; set; } // 0-1
       public string Source { get; set; }
   }
   ```

**Tasks**:
- [ ] Create IClipApiClient interface + implementation (HTTP wrapper)
- [ ] Create ImageSearchService
- [ ] Create SearchController endpoint
- [ ] Request validation (max file size 5MB, query length)
- [ ] Response mapping (include species metadata)
- [ ] Error handling (CLIP service down, pgvector error)
- [ ] Logging + metrics (latency, search volume)
- [ ] Unit tests (mock CLIP client, mock pgvector)
- [ ] Integration test (Docker backend)

**Acceptance Criteria**:
- ✅ Text search: "yellow cichlid" → returns relevant species images
- ✅ Image search: upload JPG → returns similar species
- ✅ Combined filter: text query + ecosystem filter
- ✅ Latency: <650ms (p95) for 10 results
- ✅ Similarity scores make sense (top-1 > top-10)

**Estimated**: 3-4 days

---

### Story 4.4: Frontend Image Search UI

**Objective**: Add image search component to FishDex detail page.

**Scope**:
- Search widget: text input + image upload
- Results grid: scrollable, lazy-load images
- Click result → open species detail
- Mobile-first (iPhone 12+)

**Deliverables**:
1. Component: `FrontEnd/src/components/ImageSearchWidget.tsx`
   ```tsx
   export const ImageSearchWidget: React.FC = () => {
       const [query, setQuery] = useState("");
       const [uploadedImage, setUploadedImage] = useState<File | null>(null);
       const [results, setResults] = useState<ImageSearchResult[]>([]);
       const [loading, setLoading] = useState(false);
       
       const search = async () => {
           setLoading(true);
           const formData = new FormData();
           if (query) formData.append("query", query);
           if (uploadedImage) formData.append("image", uploadedImage);
           
           const resp = await fetch(`${VITE_GATEWAY_URL}/fishdex/v1/search/image`, {
               method: "POST",
               body: formData
           });
           setResults(await resp.json());
           setLoading(false);
       };
       
       return (
           <div className="image-search">
               <input placeholder="e.g. yellow fish" value={query} onChange={e => setQuery(e.target.value)} />
               <label>Upload image: <input type="file" onChange={e => setUploadedImage(e.target.files?.[0])} /></label>
               <button onClick={search} disabled={loading}>{loading ? "Searching..." : "Search"}</button>
               <div className="results-grid">
                   {results.map(r => (
                       <div key={r.speciesId} onClick={() => navigate(`/species/${r.speciesId}`)}>
                           <img src={r.imageUrl} alt={r.speciesName} />
                           <p>{r.speciesName} ({r.similarityScore.toFixed(2)})</p>
                       </div>
                   ))}
               </div>
           </div>
       );
   };
   ```

2. Styles: Grid layout, image aspect ratio, responsive

**Tasks**:
- [ ] Create ImageSearchWidget component
- [ ] Add text input + file upload
- [ ] Integrate with /fishdex/v1/search/image endpoint
- [ ] Results grid (CSS Grid)
- [ ] Click result → navigate to species detail
- [ ] Loading state + error handling
- [ ] Mobile responsive (390px+)
- [ ] Image lazy loading (IntersectionObserver)
- [ ] Accessibility
- [ ] Unit tests

**Acceptance Criteria**:
- ✅ Text search visible on FishDex page
- ✅ Image upload works
- ✅ Results display in 1-2s (p95)
- ✅ Click result navigates to species detail
- ✅ Mobile responsive (390px)

**Estimated**: 2-3 days

---

## Summary: Total Story Breakdown

| Story | Title | Days | Blocker? | Phase |
|-------|-------|------|----------|-------|
| 2.0 | Embedding Ingestion | 3-4 | ✅ YES | v2.0 start |
| 2.1 | Embedding Service (VM3) | 4-5 | ✅ YES | v2.0 week 1 |
| 2.2 | RAG Pipeline (AquaHome) | 4-5 | 🔗 YES (after 2.1) | v2.0 week 2 |
| 2.3 | Groq LLM Integration | 2-3 | 🔗 YES (parallel 2.2) | v2.0 week 2 |
| 2.4 | Caching & Optimization | 1-2 | ❌ NO | v2.0 week 3 |
| 2.5 | FE Chat Widget | 3-4 | 🔗 YES (after 2.2) | v2.0 week 3-4 |
| 4.1 | CLIP Service (VM3) | 4-5 | ✅ YES | v2.0 week 1-2 (parallel 2.1) |
| 4.2 | pgvector Image Index | 2-3 | 🔗 YES (after 4.1 backfill) | v2.0 week 2 |
| 4.3 | Image Search API | 3-4 | 🔗 YES (after 4.2) | v2.0 week 2-3 |
| 4.4 | FE Image Search UI | 2-3 | 🔗 YES (after 4.3) | v2.0 week 3-4 |
| **TOTAL** | | **29-41 days** | | |

---

## Parallel Work Strategy

```
Week 1:
  └─ Story 2.0 (embedding ingestion) - 3-4 days
     + Story 2.1 (embedding service) - 4-5 days (parallel)
     + Story 4.1 (CLIP service) - 4-5 days (parallel)

Week 2:
  └─ Story 2.2 (RAG pipeline) - 4-5 days
     + Story 2.3 (Groq LLM) - 2-3 days (parallel)
     + Story 4.2 (image index) - 2-3 days (after 4.1 backfill)
     + Story 4.3 (image search API) - 3-4 days

Week 3:
  └─ Story 2.4 (caching) - 1-2 days
     + Story 2.5 (chat widget) - 3-4 days
     + Story 4.4 (image UI) - 2-3 days (parallel)

Total: ~5-6 weeks for v2.0 AI features (MVP)
```

---

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|-----------|
| **VM3 OOM during backfill** | High (image index incomplete) | Batch backfill in 1k chunks, monitor RSS |
| **CLIP inference slow (200ms)** | Medium (UX degradation) | Optimize batch size, consider quantization (fp16) |
| **Groq rate limit spike** | Medium (chat offline) | Queue + retry, fallback to Gemini ready |
| **Embedding ingestion takes >10h** | Low (schedule off-hours) | Run nightly, log progress |
| **pgvector index building slow** | Low (schedule off-hours) | Build index at 2 AM, alert if takes >1h |

---

## Definition of Done

Each story completed when:
1. ✅ All tasks checked off
2. ✅ Unit tests pass (>80% coverage)
3. ✅ Integration tests pass (Docker containers)
4. ✅ Acceptance criteria met
5. ✅ Code reviewed + merged to `feat/v2.0-ai` branch
6. ✅ Logged to Grafana for monitoring
7. ✅ Documented in story comment (Notion)

# R&D: FishLover v2.0 AI Stack (Option A Research)

📅 **Date**: 2026-06-19  
🎯 **Goal**: Implement RAG Q&A + Image Search on free tier (Groq + VM3)  
✅ **Status**: Option A selected + detailed breakdown complete

---

## 📂 Files in This Directory

### 1. `ai_research_summary.html` ← **START HERE**
**Purpose**: Verified free tier limits + risk assessment  
**Contents**:
- ✅ Groq API: 30 RPM / 14.4k req/day (confirmed safe)
- ✅ Google Gemini: 10 RPM / 1.5k req/day (fallback)
- ❌ Cloudflare Workers AI: 10k neurons/day (too low, not recommended)
- 💾 Model sizes: CLIP (338MB) + MiniLM (80MB) → fits in VM3 (6GB)
- ⚠️ Risk assessment: Memory headroom, rate limits, OOM scenarios

**Open**: `D:\Workspace\Practice\FishDex\R&D\ai_research_summary.html`

---

### 2. `option_a_architecture.md`
**Purpose**: System design + data flows for Option A  
**Contents**:
- 🔷 System architecture diagram (services, ports, VMs)
- 📊 RAG pipeline flow: Query → Embedding → pgvector → Groq → Response
- 📊 Image search flow: Text/Image → CLIP → pgvector → Results
- 🗺️ Service deployment map (ports, memory, scaling)
- ⚠️ Risk mitigation (VM3 OOM, Groq rate limit, fallback)
- ⏱️ Latency budgets (RAG ~700-1350ms, Image search ~400-650ms)

**Key Diagrams**:
```
RAG: User Query (30ms embedding) → 
     pgvector search (100ms) → 
     Groq LLM (500-1000ms) → 
     Response (p95: ~1.3s)

Image Search: Upload/Query (200ms CLIP) → 
              pgvector search (100ms) → 
              Presigned URLs (200ms) → 
              Results (p95: ~650ms)
```

---

### 3. `option_a_story_breakdown.md`
**Purpose**: Detailed story cards + task breakdown for Epic 2 & 4  
**Contents**:
- ⭐ 10 stories: 2.0 (embedding ingestion) → 2.1-2.5 (RAG) + 4.1-4.4 (image search)
- 🔗 Dependency chain: Which stories block which
- 📋 Each story includes:
  - Objective, Architecture, Scope
  - Deliverables (code structure, endpoints, configs)
  - Tasks (checklist)
  - Acceptance Criteria
  - Estimated days (3-5 days per story, 29-41 days total)
- 📅 Parallel work strategy (5-6 weeks for MVP)
- 🚨 Risks + mitigations

**Quick Summary**:

| Story | Title | Days | Blocker |
|-------|-------|------|---------|
| 2.0 | Embedding Ingestion | 3-4 | ✅ YES |
| 2.1 | Embedding Service (VM3) | 4-5 | ✅ YES |
| 2.2 | RAG Pipeline | 4-5 | 🔗 YES |
| 2.3 | Groq LLM Integration | 2-3 | 🔗 YES |
| 2.4 | Caching | 1-2 | ❌ NO |
| 2.5 | FE Chat Widget | 3-4 | 🔗 YES |
| 4.1 | CLIP Service (VM3) | 4-5 | ✅ YES |
| 4.2 | pgvector Image Index | 2-3 | 🔗 YES |
| 4.3 | Image Search API | 3-4 | 🔗 YES |
| 4.4 | FE Image Search UI | 2-3 | 🔗 YES |
| **TOTAL** | | **29-41 days** | |

---

## 🎯 Why Option A?

| Factor | Option A | Option B | Option C |
|--------|----------|----------|----------|
| **Cost** | $0 (Groq free) | $0 (self-hosted) | $0 (Cloudflare) |
| **Charge Risk** | ✅ None (rate-limited) | ✅ None | ✅ None |
| **LLM Quality** | ✅ Excellent (Llama 8B) | ❌ Poor (Gemma 2B) | ✅ Good (Llama 8B) |
| **VM3 Memory** | ✅ Safe (3.5/6 GB used) | ⚠️ Borderline (4.6/6 GB) | ✅ Safe |
| **Latency** | ✅ Good (~1.3s RAG) | ❌ Slow (~4-5s) | ✅ Good (~1.5s) |
| **Vendor Dependency** | ⚠️ Groq | ✅ None | ⚠️ Cloudflare |
| **Rate Limit** | ⚠️ 30 RPM | ✅ Unlimited | ❌ 10k neurons/day too low |
| **Scaling for Production** | ✅ Easy (upgrade Groq paid) | ❌ Difficult (expensive HW) | ⚠️ Low quota → bad |
| **Complexity** | Medium | Low | Low |

**Verdict**: Option A is the sweet spot — excellent quality, safe, scales to production.

---

## 🚀 Next Steps

1. **Update Notion v2.0 Release**:
   - Link these R&D docs
   - Create Epic 2 + Epic 4 stories in "By Release" view
   - Mark Story 2.0, 2.1, 4.1 as blockers

2. **Prepare Dev Environment**:
   - Create `feat/v2.0-ai` branch
   - Scaffold `Pipeline/vm3/embedding_service/` + `image_search_service/` directories
   - Set up Groq API account + get free API key

3. **Start Sprint**:
   - Story 2.0 (embedding ingestion) — critical path
   - Story 2.1 + 4.1 (services on VM3) — parallel
   - Weekly checkpoint: verify VM3 memory + latency metrics

---

## ⚠️ Critical Constraints

- **VM3 Memory**: 6GB total. CLIP (450MB) + MiniLM (200MB) + OS (800MB) + inference (1-2GB) = 2.5-3.5GB headroom OK, but no room for LLM on same VM.
- **Groq Rate Limit**: 30 RPM sufficient for small scale. Monitor usage in Grafana.
- **pgvector Backfill**: ~4 hours for 70k images. Run off-hours.
- **No Offline Mode**: If Groq or VM3 down → RAG/image search offline. Have fallback plan.

---

## 📞 Questions?

- **Groq quota too low?** → Upgrade to paid tier ($0.10 per 1M tokens)
- **VM3 OOM in production?** → Scale to VM3+ (12GB available on Oracle free tier)
- **Latency unacceptable?** → Optimize CLIP with quantization (fp16) or smaller model (CLIP ViT-B/16)
- **FE users complain about image search?** → Implement hybrid (text + re-ranking with ML)

---

## 🔍 Document Map

```
R&D/
├─ README.md (this file) ← Overview + links
├─ ai_research_summary.html ← Verified free tier limits (open in browser)
├─ option_a_architecture.md ← System design + flows
└─ option_a_story_breakdown.md ← Detailed stories + tasks

Production Links:
├─ CLAUDE.md (BackEndProject/) — Development rules
├─ Notion: "By Release" view → v2.0 AI epics
└─ Grafana: (TBD) Monitor VM3 memory + Groq latency
```

---

**Created**: 2026-06-19  
**Research Author**: Claude  
**Review Status**: ⏳ Pending your review  

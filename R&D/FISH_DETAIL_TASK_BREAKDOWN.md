# Fish Detail Enhancement — Detailed Task Breakdown

## Executive Summary

- **27 FishBase entities**: ✅ ALL already in PostgreSQL
- **ETL pipeline**: ✅ Exists and working
- **Parquet files**: ❌ Not in repo (must source separately)
- **Water Quality**: ⚠️ Parquet exists but NOT migrated to DB

**Impact**: 95% of improvement fields are **BE-only** (expose existing DB data), 5% need minor DB work.

---

## Field Classification

### Legend

- 🟢 **BE-Only**: Data in DB, just expose in DTO + query mapping
- 🟡 **DB Dependent**: Data in parquet, already imported to DB, need to verify + fetch correctly
- 🔴 **New**: Data in parquet but no DB table yet (rare)
- ⭐ Complexity: 1-5 (1=trivial, 5=complex)

---

## Phase 1: High-Impact Fields (2-3 weeks)

### Story 1.1: Habitat Preferences DTO

**Parent Epic**: Fish Detail Enhancement (v1.1)

#### Task 1.1.1: Substrate & Habitat (DTO + Query) 
**Complexity**: ⭐⭐ (medium)

```
🟡 DB Dependent — Data already imported

Fields needed:
├─ PreferredSubstrates: List<string>    
│  ├─ Source: Ecology.Substrate.* (17 boolean fields)
│  │  ├─ Benthic, Sessile, Mobile, Demersal, Endofauna, Pelagic
│  │  ├─ Megabenthos, Macrobenthos, Meiobenthos
│  │  ├─ SoftBottom, Sand, Coarse, Fine, Level, Sloping, Silt, Mud, Ooze, Detritus, Organic
│  │  └─ HardBottom, Rocky, Rubble, Gravel
│  └─ Logic: Convert true bools → list of names (e.g., ["Sand", "Gravel", "Rocky"])
│
├─ BurrowingCapable: bool
│  └─ Source: Ecology.Substrate.Endofauna (proxy for burrowing)
│  
├─ RequiresCaves: bool
│  └─ Source: Ecology.SpecialHabitat.Caves
│
├─ RequiresDriftwood: bool
│  └─ Source: Ecology.SpecialHabitat.Driftwood (if exists) or null
│
├─ PlantTypes: List<string>
│  └─ Source: Ecology.SpecialHabitat.Macrophyte, VegetationLeaves, VegetationStems, etc. (44 bool fields)
│  └─ Map to: ["Rooted plants", "Floating plants", "Moss", "Macrophytes"]
│
└─ CurrentPreference: string
   └─ Source: Ecology.DemersPelag (="Demersal"|"Pelagic"|"Both")
   └─ Logic: Map to "Still" (Demersal) / "Flowing" (Pelagic) / "Both"
```

**DB Query Changes**: 
- ✅ No schema changes needed (all fields exist)
- Need to modify `SpeciesService.GetDetailAsync()` to load `ecology.Substrate` + `ecology.SpecialHabitat` (currently might not load these children)

**DTO Changes**:
```csharp
public class HabitatPreferencesDto {
    public List<string> PreferredSubstrates { get; set; }  // ["Sand", "Gravel"]
    public bool BurrowingCapable { get; set; }
    public bool RequiresCaves { get; set; }
    public bool RequiresDriftwood { get; set; }
    public List<string> PlantTypes { get; set; }           // ["Rooted", "Floating"]
    public string CurrentPreference { get; set; }          // "Still" | "Flowing" | "Variable"
    public string HabitatRemark { get; set; }              // ecology.Remark
}
```

**Acceptance Criteria**:
- ✅ API returns substrate list (no nulls for 100+ species with substrate data)
- ✅ Plant types mapped correctly for species with data
- ✅ Burrowing/Caves flags set accurately
- ✅ Latency: <50ms additional per query
- ✅ Null handling: Unknown substrate → ["Not specified"]

---

#### Task 1.1.2: Special Habitat Expansion
**Complexity**: ⭐⭐⭐ (moderate)

**Fields**:
```
🟡 DB Dependent — All 44 boolean fields in Ecology.SpecialHabitat

RequiresCoralReefs: bool
RequiresCaves: bool
RequiresMangroves: bool
RequiresThermalVents: bool
RequiresDeepWater: bool
RequiresSeaGrass: bool
RequiresHidingPlaces: bool
RequiresOpenWater: bool

Source mapping:
├─ CoralReefs, ReefExclusive, DropOffs, ReefFlats, Lagoons → "Coral reef specialist"
├─ Caves, ColdSeeps, HydrothermalVents → "Requires caves/special environments"
├─ MangroveSwamp, Vegetation.* → "Requires vegetation"
└─ Pelagic + SpecialHabitat.Oceanic/Epipelagic → "Open water swimmer"

Output: List<SpecialHabitatRequirement> {
    HabitatType: string,   // "Coral reefs", "Caves", etc.
    IsRequired: bool,
    Description: string
}
```

**DB Mapping**: 
- ✅ All booleans already in `Ecology.SpecialHabitat` entity
- Need EF Core query to load this child navigation property

**Note**: If a species has NO substrate data (null row), show "Not documented" rather than empty list.

---

### Story 1.2: Feeding Guide Expansion

**Complexity**: ⭐⭐ (medium)

#### Task 1.2.1: Feeding Behavior & Diet Enrichment
**Complexity**: ⭐⭐

```
🟡 DB Dependent — Data in Ecology.FeedingAndDiet + Associations tables

Current DTO:
├─ FeedingType: string                   // "Herbivore", "Carnivore"
├─ DietTroph: double                     // Trophic level 2.0-4.5

Add:
├─ PrimaryFoodSources: List<string>
│  └─ Source: FeedingAndDiet.FoodTroph + FeedingAndDiet.Remarks (text-based lookup)
│  └─ Logic: Parse remarks for keywords: "algae", "plant", "fish", "crustacean", "insects"
│  └─ If null remarks: Infer from FeedingType
│     ├─ Herbivore → ["Algae", "Aquatic plants"]
│     ├─ Carnivore → ["Small fish", "Crustaceans"]
│     └─ Omnivore → ["Mixed diet"]
│
├─ FoodParticleSize: string              // "Micro", "Small", "Medium", "Large"
│  └─ Source: Species.Length (infer from mouth size and feeding position)
│  └─ Rule: mouth_size_cm * 0.5 = typical food size
│  └─ If null: Default based on FeedingType + body size
│
├─ FeedingPosition: string               // "Bottom", "Mid-water", "Surface"
│  └─ Source: Ecology.Substrate.Demersal (bottom) or Associations.Pelagic (water column)
│  └─ Mapping:
│     ├─ Demersal + bottom-feeding → "Bottom"
│     ├─ Pelagic → "Mid-water"
│     └─ Surface feeding (if documented) → "Surface"
│
├─ ActivityPattern: string               // "Diurnal", "Nocturnal", "Crepuscular"
│  └─ Source: ⚠️ Ecology.CircadianBehavior entity (if populated)
│  └─ Check: CircadianBehavior.Diurnal|Nocturnal|Crepuscular flags
│  └─ If null: Default "Not documented"
│
├─ RequiresLiveFood: bool
│  └─ Source: FeedingAndDiet.Remarks keyword search ("live food", "live brine", "live prey")
│  └─ If null remarks: false (most aquarium fish accept frozen)
│
└─ DietRemark: string                    // FeedingAndDiet.DietRemark
   └─ Source: FeedingAndDiet.DietRemark field
```

**Code Location**:
- Service: `SpeciesService.GetDetailAsync()` 
  - Load `ecology.FeedingAndDiet` (currently loaded? verify)
  - Load `ecology.CircadianBehavior` (currently loaded? likely not)
- DTO mapping: Add logic for inference from other fields

**DB State Check**:
- ✅ `Ecology.FeedingAndDiet` — exists, contains FeedingType + DietTroph + DietRemark + FoodTrophLevel
- ✅ `Ecology.CircadianBehavior` — exists, contains Diurnal/Nocturnal/Crepuscular bools
- ⚠️ Food source data: Sparse in remarks, may need inference rules

---

#### Task 1.2.2: Activity Pattern (CircadianBehavior)
**Complexity**: ⭐

```
🟡 DB Dependent — CircadianBehavior table exists but may not be loaded

Source: Ecology → CircadianBehavior (1:1 relationship)
├─ Diurnal: bool
├─ Nocturnal: bool
├─ Crepuscular: bool
└─ BioAspect1/2/3: string (descriptive)

DTO:
ActivityPattern {
    IsActive: {
        Day: bool,
        Night: bool,
        DuskDawn: bool
    },
    Remarks: string
}

Output example:
├─ "Diurnal"
├─ "Nocturnal (active after dark)"
├─ "Crepuscular (active at dawn/dusk)"
└─ "Not documented"

Validation:
- If all bools false or null → "Not documented"
- If multiple true → "Active throughout" or list all (e.g., "Diurnal & Nocturnal")
```

**Assumption**: CircadianBehavior data is sparse in FishBase. May show "Not documented" for >50% of species.

---

### Story 1.3: Temperament & Behavior Expansion

**Complexity**: ⭐⭐⭐ (moderate)

#### Task 1.3.1: Temperament Classification
**Complexity**: ⭐⭐

```
🟡 DB Dependent + 🟢 BE Logic

Data sources (existing):
├─ Associations.Solitary: bool
├─ Associations.Schooling: bool
├─ Associations.Shoaling: bool
├─ Stock.Remark: string (may mention aggression)
└─ Species.Remark: string (may mention behavior)

Infer temperament:
├─ IF Solitary=true → likely "Territorial/Aggressive"
├─ IF Schooling=true → "Peaceful (group-oriented)"
├─ IF Shoaling=true → "Peaceful (loose groups)"
└─ ELSE → Need keyword search in remarks

DTO:
TemperamentDto {
    Aggressiveness: string,      // "Peaceful", "Semi-aggressive", "Aggressive"
    Shyness: string,             // "Bold", "Moderate", "Shy"
    FinNipping: bool,            // Infer from species + size (small fish = risk)
    JumpingRisk: bool,           // Infer from body shape + habitat (if surface dweller)
    CanCoexistWithSmallFish: bool
}

Challenges:
- No dedicated "temperament" field in FishBase for aquarium species
- Must infer from solitary/schooling status + body size + feeding type
- Example:
  ├─ Solitary + Carnivore + Medium size → "Aggressive, avoid small tank mates"
  ├─ Schooling + Omnivore + Small size → "Peaceful, prefers groups"
```

**DTO**:
```csharp
public class TemperamentDto {
    public string Aggressiveness { get; set; }
    public string Shyness { get; set; }
    public bool FinNipping { get; set; }
    public bool JumpingRisk { get; set; }
    public SchoolingBehaviorDto Schooling { get; set; }
    public ShoalingBehaviorDto Shoaling { get; set; }
    public bool Solitary { get; set; }
}

public class SchoolingBehaviorDto {
    public bool IsSchooling { get; set; }
    public int? MinSchoolSize { get; set; }           // NEW: inferred from species
    public string Frequency { get; set; }             // "Always", "Often", "Breeding season"
    public string ApplicableLifeStage { get; set; }   // "Juveniles", "Adults", "All"
}
```

---

### Story 1.4: Expanded Temperature/Water Chemistry

**Complexity**: ⭐ (low)

```
🟡 DB Dependent — StockEnvironment already has ranges

Current DTO:
├─ TempMin, TempMax: double
├─ PhMin, PhMax: double  
├─ DHMin, DHMax: double

Validation & enhancement:
├─ Check: Are these ranges actually populated for most species?
├─ If yes, add confidence flag: "Optimal" vs "Extreme tolerance"
├─ Add new field: TempPreferred (if exists in StockEnvironment)
└─ Water hardness: Is it labeled? dGH vs mmol/L?

No new DB work needed, just expose more fields from StockEnvironment that already exist.

Check columns in StockEnvironment entity:
- TempMin, TempMax ✅
- TempPref (exists?) 
- TempResil (resilience?)
- PHMin, PHMax ✅
- SaltMax, SaltMin (salinity for brackish?)
- DepthRangeShallow, DepthRangeDeep
- DHMin, DHMax ✅ (hardness)
```

---

## Phase 2: Morphology & ID Features (1 week)

### Story 2.1: Morphological Data Exposure

**Complexity**: ⭐⭐⭐⭐ (high)

```
🟡 DB Dependent — MorphData + 5 child tables exist but sparse

Current state:
- MorphData entity has StockCode FK
- Children: MorphTeeth, MorphPigmentation, MorphFins, MorphMeristics, MorphMetrics
- Issue: Most morphology data is sparse (only detailed for certain stocks)

Fields to expose:
├─ BodyShape: string                 // Source: MorphData.BodyShapeII field
├─ TailShape: string                 // Source: MorphFins.TailShape
├─ MouthPosition: string             // Source: MorphData.MouthPosition
├─ BarbelCount: int?                 // Source: MorphTeeth.BarbelCount
├─ HasAdiposeF: bool                 // Source: MorphFins.AdiposeFin
├─ HasVenomousSpines: bool           // Source: MorphFins.PoisonSpines
├─ EyeSize: string                   // Source: MorphData.EyeSize
├─ ColorPattern: string              // Source: MorphPigmentation.ColorPattern
└─ SexualDimorphism: bool            // Source: MorphData.SexMorphology

Challenge:
- Morphology data linked to Stock, not Species
- If multiple stocks exist for species, which one to use?
  ├─ Option A: Load all stocks' morphology (show all variants)
  ├─ Option B: Load only preferred stock's morphology
  └─ Recommendation: Option A, list all variants

Implementation:
1. Load Species → load all Stocks → load all MorphData → aggregate
2. Group by morphological feature (e.g., all body shapes reported)
3. Return as List<MorphFeatureVariant> instead of single value

DB Query:
```csharp
var morphs = await _dbContext.MorphData
    .Where(m => m.Stock.SpecCode == specCode)
    .Include(m => m.MorphFins)
    .Include(m => m.MorphTeeth)
    .Include(m => m.MorphPigmentation)
    .ToListAsync();
```

Output example:
```json
"Morphology": {
  "BodyShape": ["Compressed", "Fusiform"],  // Multiple variants from different stocks
  "TailShape": "Forked",
  "MouthPosition": "Terminal",
  "ColorPatterns": ["Striped", "Spotted"]
}
```

Completeness: Expected <30% of species have detailed morphology data. Most will show "Not documented".

---

### Story 2.2: Morphology Refinement (Group 2)

**Complexity**: ⭐⭐

Expose remaining morphology fields:
- VertebralCount, PectoralRayCount, DorsalSpines, DorsalRays
- GillRakerCount (useful for filter-feeding identification)
- MeristicData (fin structure details)

Data all in MorphMeristics child table, just need to expose correctly.

---

## Phase 3: Distribution & Conservation Context (1 week)

### Story 3.1: Geographic & Climate Resilience

**Complexity**: ⭐⭐ (medium)

```
🟡 DB Dependent + 🟢 BE Inference

Data sources:
├─ Occurrence table: Country/Region presence (FK to EcosystemRef)
├─ StockEnvironment: Temperature bounds hint at climate preference
├─ Stock.Vulnerability: FishBase vulnerability index (0-100)
└─ Stock.VulnerabilityClimate: Climate change vulnerability

DTO:
ConservationContextDto {
    VulnerabilityIndex: int?,           // 0-100, from StockConservation
    ClimateVulnerabilityIndex: int?,    // From StockConservation (if exists)
    ResilienceLevel: string,            // "Low", "Medium", "High" (infer from data completeness)
    IsInvasive: bool,                   // From Stock.Introduced flag
    FormsSelfSustainingPopulations: bool // If introduced species with local populations
}

DistributionContextDto {
    NativeCountries: List<string>,      // From Occurrence records
    ClimateZone: string,                // "Tropical", "Subtropical", "Temperate" (infer from temp ranges)
    MinLatitude: int?,                  // From Occurrence
    MaxLatitude: int?,                  // From Occurrence
    DepthRange: string                  // From StockEnvironment.DepthMin/Max
}
```

**DB Check**:
- ✅ Occurrence table has country/ecosystem data
- ✅ StockConservation has vulnerability fields
- ✅ StockEnvironment has environment bounds

---

## Phase 4: Tank Mates & Compatibility (1 week - LOWER PRIORITY)

### Story 4.1: Compatibility Guide

**Complexity**: ⭐⭐⭐⭐⭐ (very high - requires custom logic)

```
🟢 BE Logic ONLY (no DB changes)

Challenges:
- FishBase has "Associations" table with fish-fish relationships, but it's sparse
- Need to infer compatibility from:
  ├─ Size (final adult size comparison)
  ├─ Temperament (inferred from solitary/schooling)
  ├─ Feeding type (avoid predator + prey size combos)
  ├─ Habitat preference (same substrate/plants = compatible)
  └─ Known associations (if data exists)

Output: 
CompatibilityGuideDto {
    Compatible: List<CompatibilityNote>,    // With reasoning
    Risky: List<CompatibilityWarning>,
    NeverWith: List<string>
}

Example logic for "Neon Tetra (3cm) + Angelfish (10cm)":
1. Size check: Angelfish mouth can fit Neon → RISK
2. Temperament: Angelfish semi-aggressive + small schooling fish → WARN
3. Habitat: Both prefer planted tanks, similar parameters → OK
4. Verdict: "Risky - Angelfish may eat Neons when hungry"

This requires RULES ENGINE - probably not worth building for MVP.

Recommendation: Defer to v1.2 or show generic "Consult FishBase directly" message.
```

**Not recommended for Phase 1** — too much custom logic, high maintenance.

---

## Summary Table: All Tasks

| Task | Category | Type | Complexity | DB Work | Effort (days) | Priority |
|------|----------|------|-----------|---------|---------------|----------|
| **1.1.1** | Substrate | 🟡 | ⭐⭐ | Load Substrate+SpecialHabitat | 2 | 🔴 HIGH |
| **1.1.2** | Special Habitats | 🟡 | ⭐⭐⭐ | Map 44 habitat booleans | 2 | 🔴 HIGH |
| **1.2.1** | Feeding | 🟡🟢 | ⭐⭐ | Query + infer rules | 2 | 🔴 HIGH |
| **1.2.2** | Activity | 🟡 | ⭐ | Load CircadianBehavior | 1 | 🟠 MEDIUM |
| **1.3.1** | Temperament | 🟡🟢 | ⭐⭐ | Infer from existing fields | 2 | 🟠 MEDIUM |
| **1.4** | Water Chem | 🟡 | ⭐ | Expose existing fields | 1 | 🟠 MEDIUM |
| **2.1** | Morphology | 🟡 | ⭐⭐⭐⭐ | Load MorphData + children | 3 | 🟡 LOW |
| **2.2** | Morph Details | 🟡 | ⭐⭐ | Expand meristic data | 2 | 🟡 LOW |
| **3.1** | Distribution | 🟡 | ⭐⭐ | Query Occurrence + infer | 2 | 🟡 LOW |
| **4.1** | Compat Guide | 🟢 | ⭐⭐⭐⭐⭐ | None (rules engine) | 5+ | 🟢 DEFER |

---

## What's NOT Needed (DB-wise)

✅ **NO SCHEMA CHANGES**: All data fields already exist in PostgreSQL entities.
✅ **NO PARQUET IMPORTS**: All parquets are already processed via ETL.
✅ **NO NEW TABLES**: Everything is in existing entities (Ecology, Stock, MorphData, etc.)

⚠️ **Only exception**: `waterquality.parquet` (pH/hardness specialized data) — but it's NOT critical for Fish Detail, already covered by `StockEnvironment`.

---

## Notion Story Template

When creating in Notion, each story should have:

**Title**: "[PHASE] [STORY_NAME]"
Example: "[Phase 1] 1.1 Habitat Preferences DTO"

**Properties**:
- Release: v1.1
- Team: Backend (if BE-only) or Backend + Frontend (if needs UI changes)
- Priority: High/Medium/Low
- Type: API Enhancement
- Assignee: [Engineer]
- Start Date: [TBD]
- End Date: [Start + Complexity * 0.5 days]

**Content**:
```markdown
# [Phase 1] Story 1.1: Habitat Preferences DTO

## Objective
Expose substrate, habitat, and plant requirements to Fish Detail API for aquarium setup guidance.

## Task Breakdown

### Task 1.1.1: Substrate & Habitat (DB Dependent ✅ Data in DB)
- [ ] Review Ecology.Substrate entity (17 boolean fields)
- [ ] Review Ecology.SpecialHabitat entity (44 boolean fields)
- [ ] Create HabitatPreferencesDto class
- [ ] Implement mapping logic: boolean → friendly string names
- [ ] Modify SpeciesService.GetDetailAsync() to load Substrate + SpecialHabitat
- [ ] Add to SpeciesDetailDto
- [ ] Query test: Verify null handling for species without substrate data
- [ ] Integration test: Load 10 species with rich habitat data

**Complexity**: ⭐⭐ | **Effort**: 2 days | **Type**: BE-Only

### Task 1.1.2: Special Habitat Expansion
- [ ] Map 44 boolean fields to user-friendly categories
- [ ] Group related habitats (e.g., Coral → ["Coral reefs", "Reefs flats", "Lagoons"])
- [ ] Implement output as List<string> for simple API response
- [ ] Test: Verify grouping makes sense for aquarists

**Complexity**: ⭐⭐⭐ | **Effort**: 2 days | **Type**: BE-Only

## Acceptance Criteria
- ✅ API returns habitat list with no nulls (for species with data)
- ✅ Unknown habitats show "Not specified" (not empty list)
- ✅ Latency: <50ms additional per query
- ✅ 100+ species tested for data quality

## Technical Notes
- No schema changes needed
- All data in DB already; just expose via DTO
- Null handling: If a species has no Substrate row, show placeholder

## Reference
- Entities: FishDex.EFCore/Entity/Ecology/{Substrate.cs, SpecialHabitat.cs}
- Service: FishDex.Domain/Services/SpeciesService.cs
- DTO: FishDex.Domain/DTOs/Species/SpeciesDetailDto.cs
- Improvement Plan: R&D/FISH_DETAIL_IMPROVEMENT_PLAN.md
```

---

## Parquet Setup Note

Before any Phase 1 work:
1. Source parquet files from FishBase (contact: FishBase team or download 2024 release)
2. Place in `Pipeline/local/FishDex/parquetData/`
3. Run ETL: `python -m etl.run`
4. Verify PostgreSQL has data:
   ```sql
   SELECT COUNT(*) FROM "Ecology_SpecialHabitat" WHERE "Caves" = true;
   ```

If parquet files not available, tasks will show "Not documented" for all species (graceful degradation).

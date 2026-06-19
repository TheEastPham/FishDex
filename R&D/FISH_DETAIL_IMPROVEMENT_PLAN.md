# Fish Detail Enhancement Plan — From 35 to 100+ Useful Fields

**Current State**: Returning only 35 of 240+ available FishBase fields → Many null values visible to users

**Goal**: Populate Fish Detail with rich, actionable data for aquarium hobbyists (aquaculturists)

---

## 📊 The Gap Analysis

| Category | DB Fields | Currently Exposed | Gap | Priority |
|----------|-----------|-------------------|-----|----------|
| **Substrate/Habitat** | 61+ | 1 simplified | 60+ | 🔴 CRITICAL |
| **Morphology** | 45+ | 0 | 45+ | 🟠 HIGH |
| **Feeding/Diet** | 16 | 2 | 14 | 🟠 HIGH |
| **Behavior/Associations** | 14 | 3 | 11 | 🟠 HIGH |
| **Environment bounds** | 20+ | 6 ranges | 14+ | 🟡 MEDIUM |
| **Circadian/Activity** | 7 | 0 | 7 | 🟡 MEDIUM |
| **Conservation risk** | 7 | 4 | 3 | 🟡 MEDIUM |
| **Geographic/Climate** | 20+ | 0 | 20+ | 🟡 MEDIUM |

---

## 🎯 What Aquarium Hobbyists Actually Need

### 1️⃣ Tank Setup (MOST IMPORTANT)

From current 6-range environment fields → expand to:

**Temperature**:
- ✅ Min/Max ranges (have)
- 🆕 **Preferred temperature** (optimal range, NOT min-max extremes)
- 🆕 **Temperature sensitivity** (can tolerate swings vs needs stability)

**Water Chemistry**:
- ✅ pH min/max (have)
- 🆕 **pH tolerance gradient** (gentle changes ok vs needs precision)
- ✅ Hardness (have)
- 🆕 **Hardness preference** (soft-water specialist vs hardy)

**Tank Substrate** (MISSING — 61 fields in DB):
- 🆕 **Preferred substrate**: sand, gravel, rocky, mixed
- 🆕 **Substrate grain size**: fine/medium/coarse
- 🆕 **Burrowing capability**: yes/no (affects tank design)
- 🆕 **Special substrate needs**: caves, driftwood, plants

**Tank Plants & Decorations** (MISSING):
- 🆕 **Plant dependency**: no plants ok vs requires dense vegetation
- 🆕 **Plant type preference**: rooted plants, floating plants, moss, macroalgae
- 🆕 **Special habitat needs**: 
  - Caves/hiding spots (frequency: occasional/essential)
  - Driftwood preference (yes/no)
  - Rocky structures (yes/no)
  - Open water (prefers swimming space)

---

### 2️⃣ Tank Mates (HIGH VALUE)

**Schooling/Shoaling Behavior** (14 fields in DB, only 3 booleans exposed):

```
Current: Schooling: true | Shoaling: true | Solitary: false

Should be:
Schooling: {
  Is Schooling: true
  Min School Size: 6-10 individuals
  Frequency: Always (vs occasional in breeding season)
  LifeStage: Adults required (vs juveniles only)
}

Shoaling: {
  Is Shoaling: true
  Preference: Prefers conspecifics (same species) OR mixed species ok
  Min Group: 3-5
  CanCoexistAlone: true/false
}
```

**Species Associations** (14 fields in DB):
- 🆕 **Compatible species**: Common natural associates
- 🆕 **Symbiotic relationships**: Clean fish? Anemone host? Coral associate?
- 🆕 **Parasitism/Predation**: Avoid these species
- 🆕 **Aggressive/Territorial notes**: Size ratio matters? Breeding season aggression?

---

### 3️⃣ Feeding & Diet (CRITICAL FOR CARE)

Current: FeedingType + DietTroph (trophic level) only

**Missing**:
- 🆕 **Primary food sources**: 
  - Herbivore → algae, aquatic plants, vegetable matter
  - Carnivore → small fish, crustaceans, insects
  - Omnivore → mixed
  - Detritivore → organic bottom matter
- 🆕 **Food particle size preference**: macro/micro/specific size range
- 🆕 **Feeding behavior**: 
  - Active bottom feeder vs water column picker vs surface feeder
  - Day/night activity (affects feeding schedule)
  - Scavenger capability (helps with tank cleanliness)
- 🆕 **Special diet notes**: 
  - Live food requirement (vs frozen ok vs pellets ok)
  - Plant-based vs meaty preference ratio
  - Algae wafers? Tablets? Flakes? Live cultures?
- 🆕 **Feeding frequency**: How often do they need food?

---

### 4️⃣ Behavioral/Activity Patterns (MISSING)

**Circadian Behavior** (7 fields, 0 exposed):
- 🆕 **Activity pattern**: 
  - Diurnal (active day)
  - Nocturnal (active night) — helps decide if nocturnal feeding needed
  - Crepuscular (active dawn/dusk)
  - Cathemeral (active mixed times)
- 🆕 **Hiding frequency**: Shy/reclusive vs bold/outgoing
- 🆕 **Nocturnal feeding requirement**: Yes/No

**Temperament**:
- 🆕 **Aggression level**: Peaceful / Semi-aggressive / Aggressive / Highly territorial
- 🆕 **Territoriality**: 
  - None (peaceful communal)
  - Male-male conflict (need territory separation)
  - Breeding territorial (seasonal aggression)
  - Always territorial (needs solitude or huge tank)
- 🆕 **Fin nipping**: Yes/No (important for community tanks)
- 🆕 **Jumping risk**: Can escape? (tank design consideration)
- 🆕 **Compatibility with smaller fish**: Prey predation risk?

---

### 5️⃣ Morphology & ID Features (45 FIELDS, ZERO EXPOSED)

**Visual ID**: Most important for species recognition on store shelf

- 🆕 **Body shape**: 
  - Fusiform (torpedo-like)
  - Compressed (side-to-side flattened)
  - Depressed (top-to-bottom flattened)
  - Spherical/Globular
  - Eel-like
- 🆕 **Fin characteristics**:
  - Dorsal fin: Single / Double / Sail-like / Adipose (fatty fin)
  - Tail shape: Rounded / Forked / Truncate / Lyre-tailed
  - Pectoral fins: Size/position
  - Barbels: Yes/No (whisker-like sensory organs)
- 🆕 **Mouth position**: 
  - Superior (top) — surface feeders
  - Terminal (centered) — water column feeders
  - Inferior (bottom) — bottom feeders
  - Barbel count for bottom feeders
- 🆕 **Eyes**: Normal / Reduced / Absent (cave fish)
- 🆕 **Unique features**: 
  - Spines/venomous (handling safety)
  - Teeth type (vegetarian vs predator)
  - Color pattern: Sexual dimorphism noted
- 🆕 **Size at maturity**: Male size / Female size (sexual dimorphism)

**Why it matters for hobbyists**:
- Buying from store — can distinguish male/female or juvenile/adult
- Tank planning — know final adult size (some species grow much larger than juveniles suggest)
- Gender selection — breeding or all-male tank strategies

---

### 6️⃣ Geographic & Climate Resilience (20+ FIELDS, ZERO EXPOSED)

**Native Distribution Bounds**:
- 🆕 **Geographic limits**: 
  - Latitude range (tropical vs subtropical understanding)
  - Altitude range (mountain stream vs lowland river?)
  - River basin (size understanding)
- 🆕 **Seasonal migration patterns**: Migratory vs year-round resident
- 🆕 **Water current preference**: Still water vs fast-flowing streams
- 🆕 **Habitat type**: River type (blackwater, clearwater, whitewater)

**Climate Resilience** (important for climate change context):
- 🆕 **Vulnerability index** (0-100): How risk climate change?
- 🆕 **Resilience level**: Can adapt to conditions vs specialist
- 🆕 **Introductions**: Does it establish non-native populations? (explains why it's invasive in some regions)

---

## 🏗️ Implementation Strategy

### Phase 1: High-Impact Fields (1-2 weeks)

Focus on top 3 categories that directly affect tank setup success:

**1. Substrate & Habitat Preferences** (New DTO fields):

```csharp
public class HabitatPreferencesDto {
    // Substrate
    public List<string> PreferredSubstrates { get; set; } // ["Sand", "Gravel", "Rocky"]
    public bool BurrowingCapable { get; set; }
    public bool RequiresCaves { get; set; }
    public bool RequiresDriftwood { get; set; }
    
    // Plants
    public bool RequiresLivePlants { get; set; }
    public List<string> PlantTypes { get; set; } // ["Rooted", "Floating", "Moss"]
    
    // Water movement
    public string CurrentPreference { get; set; } // "Still", "Gentle", "Strong", "Flowing"
    
    // Special needs
    public string HabitatRemark { get; set; } // Free text from FishBase "Remarks"
}
```

**Database mapping**:
- `Substrate.*` boolean fields → PreferredSubstrates list
- `SpecialHabitat.*` boolean fields → Requirements
- `Ecology.DemersPelag` + current fields → CurrentPreference
- `Environment.Remarks` → HabitatRemark

**2. Feeding & Diet** (Expand current fields):

```csharp
public class FeedingDto {
    public string FeedingType { get; set; } // "Herbivore", "Carnivore", "Omnivore"
    public double? TrophicLevel { get; set; } // ✅ Have
    
    // NEW:
    public List<string> PrimaryFoodSources { get; set; } 
    // ["Aquatic plants", "Algae", "Small fish", "Crustaceans"]
    
    public string FoodParticleSize { get; set; } // "Micro", "Small", "Medium", "Large"
    
    public string FeedingPosition { get; set; } 
    // "Bottom feeder", "Mid-water", "Surface", "All levels"
    
    public string ActivityPattern { get; set; } // "Diurnal", "Nocturnal", "Crepuscular"
    
    public bool RequiresLiveFood { get; set; }
    
    public string DietRemark { get; set; }
}
```

**3. Behavioral/Temperament** (New DTO):

```csharp
public class TemperamentDto {
    public string Aggressiveness { get; set; } 
    // "Peaceful", "Semi-aggressive", "Aggressive", "Highly territorial"
    
    public string Shyness { get; set; } 
    // "Bold/Outgoing", "Moderate", "Shy/Reclusive"
    
    public bool FinNipping { get; set; }
    
    public bool JumpingRisk { get; set; }
    
    // Schooling/Shoaling expanded:
    public SchoolingBehaviorDto Schooling { get; set; }
    public ShoalingBehaviorDto Shoaling { get; set; }
    
    public string BehaviorRemark { get; set; }
}

public class SchoolingBehaviorDto {
    public bool IsSchooling { get; set; }
    public int? MinSchoolSize { get; set; }
    public string Frequency { get; set; } // "Always", "Often", "Occasional"
    public string ApplicableLifeStage { get; set; } // "Juveniles", "Adults", "All stages"
}
```

---

### Phase 2: Visual & ID Features (1 week)

Hobbyists need morphology to identify species:

```csharp
public class MorphologyDto {
    public string BodyShape { get; set; } 
    // "Fusiform", "Compressed", "Depressed", "Spherical", "Eel-like"
    
    public string TailShape { get; set; } 
    // "Rounded", "Forked", "Truncate", "Lyre"
    
    public string MouthPosition { get; set; } 
    // "Superior", "Terminal", "Inferior"
    
    public int? BarbelCount { get; set; }
    
    public bool HasAdiposeF { get; set; }
    
    public bool HasVenomousSpines { get; set; }
    
    public string EyeSize { get; set; } // "Large", "Normal", "Reduced", "Absent"
    
    public string ColorPattern { get; set; } // Free text
    
    public bool SexualDimorphismPresent { get; set; }
    
    public string MorphologyRemark { get; set; }
}
```

---

### Phase 3: Distribution & Climate Resilience (1 week)

Context for hobbyists on climate impact:

```csharp
public class ConservationContextDto {
    // ✅ Have CITES/IUCN basic
    // NEW:
    public int? VulnerabilityIndex { get; set; } // 0-100, FishBase calculated
    public int? ClimateVulnerabilityIndex { get; set; }
    public string ResilienceLevel { get; set; } // "Low", "Medium", "High"
    
    public bool IsInvasive { get; set; }
    public bool FormsSelfSustainingPopulations { get; set; }
    
    public string ConservationRemark { get; set; }
}

public class DistributionContextDto {
    public string NativeHabitat { get; set; } // "Tropical blackwater streams", etc.
    public string ClimatZone { get; set; } // "Tropical", "Subtropical", "Temperate"
    public int? MinLatitude { get; set; }
    public int? MaxLatitude { get; set; }
    public string DistributionRemark { get; set; }
}
```

---

## 🎨 Frontend Display Suggestions (Mobile-first, 390px)

### Current Fish Detail Page Structure
```
[Species Name]
[Main Image]
[Basic Tabs: Overview | Habitat | Tank Mates | Feeding | Behavior]
[Conservation badges]
```

### Proposed Expanded Structure

**Tab 1: OVERVIEW** (Current + enhanced)
```
[Large image with gender/size indicators]

Quick Stats Grid (3 columns, mobile-friendly):
┌─────────┬─────────┬─────────┐
│ Size    │ Lifespan│ School? │
│ 5-8 cm  │ 3-5 yrs │ 6+ min  │
└─────────┴─────────┴─────────┘

Key Facts (expandable list):
• Temperament: Semi-aggressive
• Diet: Carnivore (small fish)
• Activity: Nocturnal
• Origin: Amazon Basin, Brazil
```

**Tab 2: TANK SETUP** (CRITICAL NEW TAB)
```
🌡️ Temperature
   Optimal: 24-27°C (NOT just min/max)
   Sensitivity: ⚠️ Prefers stable

💧 Water Chemistry
   pH: 6.0-7.0 (slightly acidic, prefers soft water)
   Hardness: 2-6 dGH
   ⚙️ Needs: Regular water changes

🏞️ Substrate & Decor
   ✓ Sandy substrate preferred
   ✓ Dense vegetation required
   ✓ Driftwood recommended
   ⚠️ Burrowing capable (tank escapes possible)

💨 Water Movement
   Prefers: Gentle current (blackwater stream fish)
   
⚡ Tank Size
   Min: 30 gallons solo, 50+ for group
```

**Tab 3: FEEDING** (CRITICAL - current is sparse)
```
🍽️ Diet Type
   Carnivore / Piscivore (small fish eater)

🎣 Primary Foods
   Natural diet: Small fish, aquatic insects
   Tank diet: Frozen bloodworms, small pellets
   ❌ Will not eat: Algae wafers

🔄 Feeding Behavior
   Activity: Nocturnal (feed in evening)
   Position: Mid-water feeder
   Frequency: Daily small amounts
   Particle size: 2-4mm pellets

✨ Special notes:
   Prefers live food if available
   Jumps at feeding time (secure lid!)
```

**Tab 4: TANK MATES** (NEW - address compatibility)
```
✅ Compatible:
   • Other small peaceful fish (non-predatory)
   • Catfish, tetras (given enough space)
   • Shrimp (adults only, may eat fry)

⚠️ Risky:
   • Fin-nippers (will attack flowing fins)
   • Large aggressive fish (will be chased)
   • Very small fry (may be eaten)

🆘 Never:
   • Cichlids (constant aggression)
   • Large predatory fish

📋 Schooling Info:
   Forms loose groups, min 6 individuals
   Best in pairs or small groups of same species
```

**Tab 5: BEHAVIOR** (NEW)
```
🎭 Temperament
   Personality: Semi-aggressive
   Attitude toward tank mates: Territorial (own space needed)
   New tank syndrome: May hide first week

🌙 Activity Pattern
   Active: Dusk to midnight
   Resting: Day (hides in plants/caves)
   ⚠️ NOTE: Nocturnal — avoid bright light overnight

🏃 Special Behaviors
   • Jumpers (secure lid essential)
   • Escapes (check water level daily)
   • Breeding: Males extremely territorial

🧠 Compatibility Note:
   Mix with equal-sized or larger tankmates only
```

**Tab 6: CONSERVATION** (Current badges expanded)
```
🔴 Conservation Status
   IUCN Red List: Endangered (as of 2020)
   CITES: Appendix II (trade regulated)

🌍 Wild Population
   Native Range: Amazon Basin, Peru
   Status: Habitat loss (52% decline since 1990s)
   Vulnerable to: Climate change, river damming

♻️ Captive Breeding
   Available captive-bred: Yes (90% of trade)
   ✅ Prefer captive-bred to support conservation

📚 Source: FishBase 2024, IUCN Red List 2020
```

---

## 📈 Impact on User Experience

**Current Problem**:
```
User sees:
- Species Detail
- Many "null" or "-" values
- Generic template
- Feel: Incomplete, unhelpful
```

**After Enhancement**:
```
User sees:
- Comprehensive care guide
- All critical tank setup data
- Feeding specifics
- Compatibility warnings
- Behavior patterns
- Feel: Expert-written care guide
- User action: Actually confident to buy & setup tank
```

---

## 🛠️ Backend Implementation (Story Card Template)

### Story: Fish Detail Data Enrichment — Phase 1 (High-Impact Fields)

**Deliverables**:
1. Extend `SpeciesDetailDto` with:
   - `HabitatPreferencesDto` (substrate, plants, special needs)
   - Expanded `FeedingDto` (food sources, position, particle size, activity)
   - `TemperamentDto` (aggression, shyness, schooling depth)

2. Backend queries to map FishBase data:
   - `Substrate.*` boolean fields → substrate list
   - `SpecialHabitat.*` fields → habitat requirements
   - `FeedingAndDiet.*` fields → feeding enrichment
   - `Associations.*` fields → schooling behavior depth
   - `CircadianBehavior.*` → activity pattern

3. Service layer changes:
   - `SpeciesService.GetDetailAsync()` to fetch & map new fields
   - Handle null values gracefully (use defaults/unknown labels)

4. Database changes:
   - Add nullable strings/enums to DTOs
   - No DB schema changes (all data exists)
   - **EF Core mapping** in `SpeciesDetailConfiguration`

5. Tests:
   - Load test species with rich data (e.g., SpecCode=1 = Abramis brama)
   - Load test species with sparse data (edge cases)
   - Compare outputs against ExxplainColumn.json definitions

**Acceptance Criteria**:
- ✅ 20+ additional fields exposed in API
- ✅ No null values for standard species (show sensible defaults)
- ✅ Latency: <200ms vs current (no N+1 queries)
- ✅ API response size: <50KB for complex species (gzipped)
- ✅ Mobile rendering: All fields readable on 390px screen

**Estimated**: 3-4 days (mostly mapping + testing)

---

## 📋 Quick Wins (1-2 days each)

1. **Expose more Substrate options** — users can optimize tank
2. **Add ActivityPattern** — helps schedule feeding (nocturnal species)
3. **Expand Schooling details** — tells min group size clearly
4. **Add mouth position** — helps identify species feeding strategy
5. **Show vulnerability index** — conservation awareness

---

## 🎯 Bottom Line

**Current**: 35 generic fields → Many null values → User thinks "incomplete data"

**After**: 100+ actionable fields → Complete care guide → User buys with confidence

**Time**: ~2 weeks for Phase 1-2 (high-impact fields + morphology)

**Value**: Transforms Fish Detail from "species encyclopedia" to "aquarium setup guide"

const DEFAULT_TTL = 5 * 60 * 1000; // 5 minutes for public/static data

interface Entry {
  data: unknown;
  ts: number;
  ttl: number;
}

const store = new Map<string, Entry>();

export function getCached<T>(key: string): T | null {
  const entry = store.get(key);
  if (!entry) return null;
  if (Date.now() - entry.ts > entry.ttl) {
    store.delete(key);
    return null;
  }
  return entry.data as T;
}

export function setCached(key: string, data: unknown, ttl = DEFAULT_TTL): void {
  store.set(key, { data, ts: Date.now(), ttl });
}

// Invalidates exact key OR all keys that start with keyPrefix + ":"
export function invalidateCache(keyOrPrefix: string): void {
  for (const key of store.keys()) {
    if (key === keyOrPrefix || key.startsWith(keyOrPrefix + ':')) {
      store.delete(key);
    }
  }
}

export function clearCache(): void {
  store.clear();
}

export const CacheKeys = {
  // FishDex — public data, 5-min TTL (default)
  speciesSummary:    (specCode: number, lang: string) => `species:summary:${specCode}:${lang}`,
  speciesDetail:     (specCode: number, lang: string) => `species:detail:${specCode}:${lang}`,
  speciesMedia:      (specCode: number)               => `species:media:${specCode}`,
  speciesOccurrences:  (specCode: number)               => `species:occurrences:${specCode}`,
  speciesCountries:    (specCode: number)               => `species:countries:${specCode}`,
  speciesDistribution: (specCode: number)               => `species:distribution:${specCode}`,
  relatedSpecies:    (specCode: number, lang: string) => `species:related:${specCode}:${lang}`,

  // AquaHome — user data, shorter TTL (60s), invalidated on mutation
  myAquariums:   () => 'aquariums:mine',
  myFavorites:   () => 'favorites:mine',
  favoriteCheck: (specCode: number) => `favorites:check:${specCode}`,
} as const;

export const USER_DATA_TTL = 60 * 1000; // 1 minute
export const FAVORITE_CHECK_TTL = 30 * 1000; // 30 seconds

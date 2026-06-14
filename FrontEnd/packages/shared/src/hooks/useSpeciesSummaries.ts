import { useEffect, useState } from 'react';
import { getSpeciesSummaries } from '../lib/api/fishDex';
import { getCached, setCached, CacheKeys } from '../lib/cache';
import type { SpeciesSummary } from '../types/species';

export function useSpeciesSummaries(specCodes: number[], language: string) {
  const [summaries, setSummaries] = useState<Record<number, SpeciesSummary>>({});
  const [loading, setLoading] = useState(false);

  const key = specCodes.slice().sort((a, b) => a - b).join(',');

  useEffect(() => {
    if (!specCodes.length) {
      setSummaries({});
      return;
    }

    const cached: Record<number, SpeciesSummary> = {};
    const missing: number[] = [];

    for (const code of specCodes) {
      const hit = getCached<SpeciesSummary>(CacheKeys.speciesSummary(code, language));
      if (hit) cached[code] = hit;
      else missing.push(code);
    }

    // Show cached items immediately
    setSummaries(cached);

    if (!missing.length) return;

    setLoading(true);
    getSpeciesSummaries(missing, language)
      .then((fresh) => {
        const freshMap: Record<number, SpeciesSummary> = {};
        for (const s of fresh) {
          setCached(CacheKeys.speciesSummary(s.specCode, language), s);
          freshMap[s.specCode] = s;
        }
        setSummaries((prev) => ({ ...prev, ...freshMap }));
      })
      .catch(() => {})
      .finally(() => setLoading(false));
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [key, language]);

  return { summaries, loading };
}

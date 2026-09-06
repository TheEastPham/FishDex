import { useState, useEffect } from 'react';
import { getSpeciesDetail, getSpeciesMedia, getSpeciesDistribution, getRelatedSpecies, AnonQuotaError } from '../lib/api/fishDex';
import { getCached, setCached, CacheKeys } from '../lib/cache';
import type { SpeciesDetail, SystemImageDto, SpeciesDistributionDto, SpeciesSearchResult } from '../types/species';

interface FishProfileState {
  detail: SpeciesDetail | null;
  media: SystemImageDto[];
  distribution: SpeciesDistributionDto | null;
  relatedSpecies: SpeciesSearchResult[];
  loading: boolean;
  error: Error | null;
  /** Khách đã hết lượt xem loài mới trong ngày — trang hiện soft wall thay vì "không tìm thấy". */
  quotaExceeded: boolean;
}

const EMPTY: FishProfileState = {
  detail: null,
  media: [],
  distribution: null,
  relatedSpecies: [],
  loading: false,
  error: null,
  quotaExceeded: false,
};

export function useFishProfile(specCode: number | null, lang: string): FishProfileState {
  const [state, setState] = useState<FishProfileState>(() => {
    if (!specCode) return EMPTY;
    const detail = getCached<SpeciesDetail>(CacheKeys.speciesDetail(specCode, lang));
    if (!detail) return { ...EMPTY, loading: true };
    return {
      detail,
      media:          getCached<SystemImageDto[]>(CacheKeys.speciesMedia(specCode))            ?? [],
      distribution:   getCached<SpeciesDistributionDto>(CacheKeys.speciesDistribution(specCode)) ?? null,
      relatedSpecies: getCached<SpeciesSearchResult[]>(CacheKeys.relatedSpecies(specCode, lang)) ?? [],
      loading: false,
      error: null,
      quotaExceeded: false,
    };
  });

  useEffect(() => {
    if (!specCode) { setState(EMPTY); return; }

    const cached = getCached<SpeciesDetail>(CacheKeys.speciesDetail(specCode, lang));
    if (cached) {
      setState({
        detail: cached,
        media:          getCached<SystemImageDto[]>(CacheKeys.speciesMedia(specCode))            ?? [],
        distribution:   getCached<SpeciesDistributionDto>(CacheKeys.speciesDistribution(specCode)) ?? null,
        relatedSpecies: getCached<SpeciesSearchResult[]>(CacheKeys.relatedSpecies(specCode, lang)) ?? [],
        loading: false,
        error: null,
        quotaExceeded: false,
      });
      return;
    }

    setState({ ...EMPTY, loading: true });

    Promise.all([
      getSpeciesDetail(specCode, lang),
      getSpeciesMedia(specCode),
      getSpeciesDistribution(specCode),
      getRelatedSpecies(specCode, 6, lang),
    ])
      .then(([detail, media, distribution, relatedSpecies]) => {
        setCached(CacheKeys.speciesDetail(specCode, lang), detail);
        setCached(CacheKeys.speciesMedia(specCode), media);
        setCached(CacheKeys.speciesDistribution(specCode), distribution);
        setCached(CacheKeys.relatedSpecies(specCode, lang), relatedSpecies);
        setState({ detail, media, distribution, relatedSpecies, loading: false, error: null, quotaExceeded: false });
      })
      .catch((e: unknown) => {
        setState({
          ...EMPTY,
          loading: false,
          error: e instanceof Error ? e : new Error(String(e)),
          quotaExceeded: e instanceof AnonQuotaError,
        });
      });
  }, [specCode, lang]);

  return state;
}

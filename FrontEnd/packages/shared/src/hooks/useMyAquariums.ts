import { useState, useEffect } from 'react';
import { getMyAquariums } from '../lib/api/aquaHome';
import { getCached, setCached, invalidateCache, CacheKeys, USER_DATA_TTL } from '../lib/cache';
import type { AquariumDto } from '../types/aquahome';

interface State {
  aquariums: AquariumDto[];
  loading: boolean;
}

export function useMyAquariums() {
  const [state, setState] = useState<State>(() => {
    const cached = getCached<AquariumDto[]>(CacheKeys.myAquariums());
    return { aquariums: cached ?? [], loading: !cached };
  });

  useEffect(() => {
    const cached = getCached<AquariumDto[]>(CacheKeys.myAquariums());
    if (cached) { setState({ aquariums: cached, loading: false }); return; }

    getMyAquariums()
      .then((data) => {
        setCached(CacheKeys.myAquariums(), data, USER_DATA_TTL);
        setState({ aquariums: data, loading: false });
      })
      .catch(() => setState((prev) => ({ ...prev, loading: false })));
  }, []);

  const invalidate = () => invalidateCache(CacheKeys.myAquariums());

  return { ...state, invalidate };
}

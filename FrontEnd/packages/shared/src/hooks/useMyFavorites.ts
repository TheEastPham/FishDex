import { useState, useEffect } from 'react';
import { getMyFavorites } from '../lib/api/aquaHome';
import { getCached, setCached, invalidateCache, CacheKeys, USER_DATA_TTL } from '../lib/cache';
import type { FavoriteDto } from '../types/aquahome';

interface State {
  favorites: FavoriteDto[];
  loading: boolean;
}

export function useMyFavorites() {
  const [state, setState] = useState<State>(() => {
    const cached = getCached<FavoriteDto[]>(CacheKeys.myFavorites());
    return { favorites: cached ?? [], loading: !cached };
  });

  useEffect(() => {
    const cached = getCached<FavoriteDto[]>(CacheKeys.myFavorites());
    if (cached) { setState({ favorites: cached, loading: false }); return; }

    getMyFavorites()
      .then((data) => {
        setCached(CacheKeys.myFavorites(), data, USER_DATA_TTL);
        setState({ favorites: data, loading: false });
      })
      .catch(() => setState((prev) => ({ ...prev, loading: false })));
  }, []);

  const invalidate = () => invalidateCache(CacheKeys.myFavorites());

  return { ...state, invalidate };
}

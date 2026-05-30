import axios from 'axios';
import type { PagedResult } from '../../types/common';
import type { SpeciesSearchResult, SearchSpeciesParams } from '../../types/species';
import { useAuthStore } from '../../store/authStore';

const fishDexClient = axios.create({
  baseURL: import.meta.env.VITE_FISHDEX_API_URL,
});

fishDexClient.interceptors.request.use((config) => {
  const token = useAuthStore.getState().accessToken;
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

export async function searchSpecies(
  params: SearchSpeciesParams,
): Promise<PagedResult<SpeciesSearchResult>> {
  const { data } = await fishDexClient.get<PagedResult<SpeciesSearchResult>>(
    '/api/species/search',
    { params },
  );
  return data;
}

export async function getFamilies(): Promise<import('../../types/species').Family[]> {
  const { data } = await fishDexClient.get<import('../../types/species').Family[]>(
    '/api/species/families'
  );
  return data;
}

import axios from 'axios';
import type { PagedResult } from '../../types/common';
import type { SpeciesSearchResult, SearchSpeciesParams, SpeciesDetail, SystemImageDto, OccurrenceDto, CountryDto } from '../../types/species';
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

export async function getSpeciesDetail(specCode: number, language?: string): Promise<SpeciesDetail> {
  const params = language ? { language } : {};
  const { data } = await fishDexClient.get<SpeciesDetail>(`/api/species/${specCode}/detail`, { params });
  return data;
}

export async function getSpeciesMedia(specCode: number): Promise<SystemImageDto[]> {
  const { data } = await fishDexClient.get<SystemImageDto[]>(`/api/species/${specCode}/media`);
  return data;
}

export async function getSpeciesOccurrences(specCode: number): Promise<OccurrenceDto[]> {
  const { data } = await fishDexClient.get<OccurrenceDto[]>(`/api/species/${specCode}/occurrences`, {
    params: { limit: 500 }
  });
  return data;
}

export async function getSpeciesCountries(specCode: number): Promise<CountryDto[]> {
  const { data } = await fishDexClient.get<CountryDto[]>(`/api/species/${specCode}/countries`);
  return data;
}

export async function getRelatedSpecies(specCode: number, limit: number = 6, language?: string): Promise<SpeciesSearchResult[]> {
  const params = language ? { limit, language } : { limit };
  const { data } = await fishDexClient.get<SpeciesSearchResult[]>(`/api/species/${specCode}/related`, { params });
  return data;
}

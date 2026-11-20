import axios from 'axios';
import type { PagedResult } from '../../types/common';
import type { SpeciesSearchResult, SpeciesSummary, SearchSpeciesParams, SpeciesDetail, SystemImageDto, OccurrenceDto, CountryDto, SpeciesDistributionDto } from '../../types/species';
import { useAuthStore } from '../../store/authStore';
import { useAnonQuotaStore } from '../../store/anonQuotaStore';
import { getVisitorId } from '../visitor';

/** 429 do hết hạn mức xem loài — khác hẳn 429 rate limit của gateway (chỉ trả text). */
export class AnonQuotaError extends Error {
  constructor(public readonly limit: number, public readonly resetsInSeconds: number) {
    super('anon_quota_exceeded');
    this.name = 'AnonQuotaError';
  }
}

const fishDexClient = axios.create({
  baseURL: import.meta.env.VITE_GATEWAY_URL,
});

fishDexClient.interceptors.request.use((config) => {
  const token = useAuthStore.getState().accessToken;
  if (token) config.headers.Authorization = `Bearer ${token}`;
  else config.headers['X-Visitor-Id'] = getVisitorId();
  return config;
});

// Hạn mức xem loài của khách đi kèm mọi response của FishDex, không có endpoint riêng để hỏi.
fishDexClient.interceptors.response.use(
  (response) => {
    readQuotaHeaders(response.headers as unknown as Record<string, string>);
    return response;
  },
  (error) => {
    const res = error?.response;
    if (res?.status === 429 && res?.data?.error === 'anon_quota_exceeded') {
      useAnonQuotaStore.getState().setExhausted(res.data.limit ?? 0, res.data.resetsInSeconds ?? 0);
      return Promise.reject(new AnonQuotaError(res.data.limit ?? 0, res.data.resetsInSeconds ?? 0));
    }
    return Promise.reject(error);
  },
);

function readQuotaHeaders(headers: Record<string, string>): void {
  const limit = Number(headers['x-anon-views-limit']);
  if (!Number.isFinite(limit) || limit <= 0) return;

  useAnonQuotaStore.getState().setFromHeaders(
    limit,
    Number(headers['x-anon-views-used']) || 0,
    Number(headers['x-anon-views-remaining']) || 0,
    Number(headers['x-anon-views-reset']) || 0,
  );
}

/**
 * Prefix cho species API theo trạng thái đăng nhập:
 * - Chưa login → route `/public/` ([AllowAnonymous] trên BE)
 * - Đã login  → route gốc ([Authorize])
 * Bản public của profile loài (detail/media/distribution/related) có trừ hạn mức xem theo loài
 * và trả payload mỏng hơn — chỉ ảnh đại diện, phân bố tới mức quốc gia.
 */
function speciesPrefix(): string {
  return useAuthStore.getState().isAuthenticated
    ? '/fishdex/v1/species'
    : '/fishdex/v1/public/species';
}

export async function searchSpecies(
  params: SearchSpeciesParams,
): Promise<PagedResult<SpeciesSearchResult>> {
  const { data } = await fishDexClient.get<PagedResult<SpeciesSearchResult>>(
    `${speciesPrefix()}/search`,
    { params },
  );
  return data;
}

export async function getFamilies(): Promise<import('../../types/species').Family[]> {
  const { data } = await fishDexClient.get<import('../../types/species').Family[]>(
    `${speciesPrefix()}/families`
  );
  return data;
}

export async function getSpeciesDetail(specCode: number, language?: string): Promise<SpeciesDetail> {
  const params = language ? { language } : {};
  const { data } = await fishDexClient.get<SpeciesDetail>(`${speciesPrefix()}/${specCode}/detail`, { params });
  return data;
}

export async function getSpeciesMedia(specCode: number): Promise<SystemImageDto[]> {
  const { data } = await fishDexClient.get<SystemImageDto[]>(`${speciesPrefix()}/${specCode}/media`);
  return data;
}

export async function getSpeciesOccurrences(specCode: number): Promise<OccurrenceDto[]> {
  const { data } = await fishDexClient.get<OccurrenceDto[]>(`/fishdex/v1/species/${specCode}/occurrences`, {
    params: { limit: 500 }
  });
  return data;
}

export async function getSpeciesCountries(specCode: number): Promise<CountryDto[]> {
  const { data } = await fishDexClient.get<CountryDto[]>(`/fishdex/v1/species/${specCode}/countries`);
  return data;
}

export async function getSpeciesSummaries(specCodes: number[], language?: string): Promise<SpeciesSummary[]> {
  const { data } = await fishDexClient.get<SpeciesSummary[]>(`${speciesPrefix()}/summaries`, {
    params: { codes: specCodes.join(','), ...(language ? { language } : {}) },
  });
  return data;
}

export async function getSpeciesDistribution(specCode: number): Promise<SpeciesDistributionDto> {
  const { data } = await fishDexClient.get<SpeciesDistributionDto>(`${speciesPrefix()}/${specCode}/distribution`);
  return data;
}

/** Batch distribution cho nhiều loài trong 1 request — key = specCode. Route public (no auth). */
export async function getSpeciesDistributionsBatch(specCodes: number[]): Promise<Record<number, SpeciesDistributionDto>> {
  if (specCodes.length === 0) return {};
  const { data } = await fishDexClient.get<Record<number, SpeciesDistributionDto>>(
    '/fishdex/v1/public/species/distributions',
    { params: { codes: specCodes.join(',') } },
  );
  return data;
}

export async function getRelatedSpecies(specCode: number, limit: number = 6, language?: string): Promise<SpeciesSearchResult[]> {
  const params = language ? { limit, language } : { limit };
  const { data } = await fishDexClient.get<SpeciesSearchResult[]>(`${speciesPrefix()}/${specCode}/related`, { params });
  return data;
}

import axios from 'axios';
import type { PagedResult } from '../../types/common';
import type {
  MarketCountryDto,
  MarketSpeciesDto,
  MarketStatsDto,
  GetMarketSpeciesParams,
  SpeciesLookupDto,
  AddTradedSpeciesRequest,
} from '../../types/market';
import { useAuthStore } from '../../store/authStore';

/**
 * Market API. Gateway map `/fishdex/v1/{everything}` → `/api/{everything}` của FishDex.
 *
 * Các endpoint đọc đều AllowAnonymous ở BE — trang market là cửa vào của khách chưa đăng nhập,
 * nên KHÔNG có biến thể `/public/` như species. Vẫn gắn token nếu có, để endpoint admin dùng chung client.
 */
const marketClient = axios.create({
  baseURL: import.meta.env.VITE_GATEWAY_URL,
});

marketClient.interceptors.request.use((config) => {
  const token = useAuthStore.getState().accessToken;
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

const BASE = '/fishdex/v1/market';

/** Các nước đã bật trang market — hiện chỉ Việt Nam, phần còn lại bật dần khi có người dùng. */
export async function getMarketCountries(): Promise<MarketCountryDto[]> {
  const { data } = await marketClient.get<MarketCountryDto[]>(`${BASE}/countries`);
  return data;
}

export async function getMarketSpecies(
  alpha2: string,
  params: GetMarketSpeciesParams = {},
): Promise<PagedResult<MarketSpeciesDto>> {
  const { data } = await marketClient.get<PagedResult<MarketSpeciesDto>>(
    `${BASE}/${alpha2.toLowerCase()}/species`,
    { params },
  );
  return data;
}

/** Ba con số của dải thống kê. Endpoint riêng vì chỉ COUNT — trả về tức thì, không đợi ảnh. */
export async function getMarketStats(alpha2: string): Promise<MarketStatsDto> {
  const { data } = await marketClient.get<MarketStatsDto>(`${BASE}/${alpha2.toLowerCase()}/stats`);
  return data;
}

/**
 * Các quốc gia đang bán một loài (alpha-2). Trang chi tiết loài dùng để hiện badge.
 * Mảng rỗng nghĩa là chưa nước nào có loài này trong danh sách.
 */
export async function getSellingCountries(specCode: number): Promise<string[]> {
  const { data } = await marketClient.get<string[]>(`${BASE}/species/${specCode}/countries`);
  return data;
}

/**
 * Tra tên khoa học trên index toàn bộ FishBase (~35.7k loài) để biết loài nằm ở nhánh nào.
 * Không tìm thấy nghĩa là loài lai — chuyển sang luồng submit community species.
 */
export async function lookupSpecies(query: string, limit = 20): Promise<SpeciesLookupDto[]> {
  const { data } = await marketClient.get<SpeciesLookupDto[]>(`${BASE}/lookup`, {
    params: { q: query, limit },
  });
  return data;
}

// ── Admin ─────────────────────────────────────────────────────────
// Chỉ SystemAdmin/ContentAdmin gọi được. Người dùng thường không có nút nào.

export async function addTradedSpecies(
  alpha2: string,
  request: AddTradedSpeciesRequest,
): Promise<void> {
  await marketClient.post(`${BASE}/${alpha2.toLowerCase()}/species`, request);
}

export async function removeTradedSpecies(alpha2: string, specCode: number): Promise<void> {
  await marketClient.delete(`${BASE}/${alpha2.toLowerCase()}/species/${specCode}`);
}

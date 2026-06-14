import { apiClient } from './client';
import type { AquariumDto, CreateAquariumRequest, UpdateAquariumRequest, FavoriteDto } from '../../types/aquahome';

// ── Aquariums ─────────────────────────────────────────────

export async function getMyAquariums(): Promise<AquariumDto[]> {
  const { data } = await apiClient.get<AquariumDto[]>('/aquahome/v1/aquariums');
  return data;
}

export async function getAquariumById(id: string): Promise<AquariumDto> {
  const { data } = await apiClient.get<AquariumDto>(`/aquahome/v1/aquariums/${id}`);
  return data;
}

export async function createAquarium(req: CreateAquariumRequest): Promise<AquariumDto> {
  const { data } = await apiClient.post<AquariumDto>('/aquahome/v1/aquariums', req);
  return data;
}

export async function updateAquarium(id: string, req: UpdateAquariumRequest): Promise<AquariumDto> {
  const { data } = await apiClient.put<AquariumDto>(`/aquahome/v1/aquariums/${id}`, req);
  return data;
}

export async function deleteAquarium(id: string): Promise<void> {
  await apiClient.delete(`/aquahome/v1/aquariums/${id}`);
}

export async function addFishToAquarium(aquariumId: string, specCode: number, quantity = 1): Promise<void> {
  await apiClient.post(`/aquahome/v1/aquariums/${aquariumId}/fish`, { specCode, quantity });
}

// ── Favorites ─────────────────────────────────────────────

export async function getMyFavorites(): Promise<FavoriteDto[]> {
  const { data } = await apiClient.get<FavoriteDto[]>('/aquahome/v1/favorites');
  return data;
}

export async function checkFavorite(specCode: number): Promise<boolean> {
  try {
    const { data } = await apiClient.get<boolean | { specCode: number; isFavorite: boolean }>(`/aquahome/v1/favorites/${specCode}`);
    if (typeof data === 'boolean') return data;
    return (data as { specCode: number; isFavorite: boolean }).isFavorite ?? false;
  } catch {
    return false;
  }
}

export async function addFavorite(specCode: number): Promise<void> {
  await apiClient.post(`/aquahome/v1/favorites/${specCode}`);
}

export async function removeFavorite(specCode: number): Promise<void> {
  await apiClient.delete(`/aquahome/v1/favorites/${specCode}`);
}

// ── TODO(BE): Cần API batch get species by list of specCodes ─────
// GET /fishdex/v1/species/batch?specCodes=1,2,3,...
// Hoặc POST /fishdex/v1/species/batch với body: { specCodes: number[] }
// Hiện tại FE sẽ gọi từng specCode một (không tối ưu) cho đến khi BE implement.

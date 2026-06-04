import { apiClient } from './client';
import type { AquariumDto, CreateAquariumRequest, UpdateAquariumRequest, FavoriteDto } from '../../types/aquahome';

// ── Aquariums ─────────────────────────────────────────────

export async function getMyAquariums(): Promise<AquariumDto[]> {
  const { data } = await apiClient.get<AquariumDto[]>('/api/aquariums');
  return data;
}

export async function getAquariumById(id: string): Promise<AquariumDto> {
  const { data } = await apiClient.get<AquariumDto>(`/api/aquariums/${id}`);
  return data;
}

export async function createAquarium(req: CreateAquariumRequest): Promise<AquariumDto> {
  const { data } = await apiClient.post<AquariumDto>('/api/aquariums', req);
  return data;
}

export async function updateAquarium(id: string, req: UpdateAquariumRequest): Promise<AquariumDto> {
  const { data } = await apiClient.put<AquariumDto>(`/api/aquariums/${id}`, req);
  return data;
}

export async function deleteAquarium(id: string): Promise<void> {
  await apiClient.delete(`/api/aquariums/${id}`);
}

// ── Favorites ─────────────────────────────────────────────

export async function getMyFavorites(): Promise<FavoriteDto[]> {
  const { data } = await apiClient.get<FavoriteDto[]>('/api/favorites');
  return data;
}

export async function checkFavorite(specCode: number): Promise<boolean> {
  try {
    const { data } = await apiClient.get<boolean | { specCode: number; isFavorite: boolean }>(`/api/favorites/${specCode}`);
    if (typeof data === 'boolean') return data;
    return (data as { specCode: number; isFavorite: boolean }).isFavorite ?? false;
  } catch {
    return false;
  }
}

export async function addFavorite(specCode: number): Promise<void> {
  await apiClient.post(`/api/favorites/${specCode}`);
}

export async function removeFavorite(specCode: number): Promise<void> {
  await apiClient.delete(`/api/favorites/${specCode}`);
}

// ── TODO(BE): Cần API batch get species by list of specCodes ─────
// GET /api/species/batch?specCodes=1,2,3,...
// Hoặc POST /api/species/batch với body: { specCodes: number[] }
// Hiện tại FE sẽ gọi từng specCode một (không tối ưu) cho đến khi BE implement.

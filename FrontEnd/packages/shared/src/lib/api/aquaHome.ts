import { apiClient } from './client';
import type { AquariumDto, AquariumFishDto, CreateAquariumRequest, UpdateAquariumRequest, FavoriteDto, RecentlyViewedDto, AquariumMediaDto, PresignedUploadDto, ReminderDto, CreateReminderRequest, CompleteReminderResponse } from '../../types/aquahome';

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

export async function getAquariumFish(aquariumId: string): Promise<AquariumFishDto[]> {
  const { data } = await apiClient.get<AquariumFishDto[]>(`/aquahome/v1/aquariums/${aquariumId}/fish`);
  return data;
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

// ── Recently Viewed ───────────────────────────────────────

export async function getRecentlyViewed(): Promise<RecentlyViewedDto[]> {
  const { data } = await apiClient.get<RecentlyViewedDto[]>('/aquahome/v1/recently-viewed');
  return data;
}

export async function recordView(specCode: number): Promise<void> {
  await apiClient.post(`/aquahome/v1/recently-viewed/${specCode}`);
}

// ── Aquarium Media ────────────────────────────────────────

export async function getAquariumMedia(aquariumId: string): Promise<AquariumMediaDto[]> {
  const { data } = await apiClient.get<AquariumMediaDto[]>(`/aquahome/v1/aquariums/${aquariumId}/media`);
  return data;
}

export async function requestMediaUpload(
  aquariumId: string,
  fileName: string,
  contentType: string,
): Promise<PresignedUploadDto> {
  const { data } = await apiClient.post<PresignedUploadDto>(
    `/aquahome/v1/aquariums/${aquariumId}/media/presign`,
    { fileName, contentType },
  );
  return data;
}

/** Upload file thẳng lên R2 dùng presigned PUT URL — không qua BE */
export async function uploadToR2(
  uploadUrl: string,
  file: Blob,
  contentType: string,
  onProgress?: (percent: number) => void,
): Promise<void> {
  await new Promise<void>((resolve, reject) => {
    const xhr = new XMLHttpRequest();
    xhr.open('PUT', uploadUrl);
    xhr.setRequestHeader('Content-Type', contentType);
    xhr.upload.onprogress = (e) => {
      if (e.lengthComputable) onProgress?.(Math.round((e.loaded / e.total) * 100));
    };
    xhr.onload = () => (xhr.status >= 200 && xhr.status < 300 ? resolve() : reject(new Error(`R2 upload failed: ${xhr.status}`)));
    xhr.onerror = () => reject(new Error('R2 upload network error'));
    xhr.send(file);
  });
}

export async function confirmMediaUpload(aquariumId: string, mediaId: string): Promise<AquariumMediaDto> {
  const { data } = await apiClient.post<AquariumMediaDto>(
    `/aquahome/v1/aquariums/${aquariumId}/media/${mediaId}/confirm`,
  );
  return data;
}

export async function deleteAquariumMedia(aquariumId: string, mediaId: string): Promise<void> {
  await apiClient.delete(`/aquahome/v1/aquariums/${aquariumId}/media/${mediaId}`);
}

// ── Reminders ─────────────────────────────────────────────

export async function getReminders(aquariumId: string): Promise<ReminderDto[]> {
  const { data } = await apiClient.get<ReminderDto[]>(`/aquahome/v1/aquariums/${aquariumId}/reminders`);
  return data;
}

export async function createReminder(aquariumId: string, req: CreateReminderRequest): Promise<ReminderDto> {
  const { data } = await apiClient.post<ReminderDto>(`/aquahome/v1/aquariums/${aquariumId}/reminders`, req);
  return data;
}

export async function completeReminder(aquariumId: string, reminderId: string): Promise<CompleteReminderResponse> {
  const { data } = await apiClient.put<CompleteReminderResponse>(
    `/aquahome/v1/aquariums/${aquariumId}/reminders/${reminderId}/complete`,
  );
  return data;
}

export async function deleteReminder(aquariumId: string, reminderId: string): Promise<void> {
  await apiClient.delete(`/aquahome/v1/aquariums/${aquariumId}/reminders/${reminderId}`);
}

// ── TODO(BE): Cần API batch get species by list of specCodes ─────
// GET /fishdex/v1/species/batch?specCodes=1,2,3,...
// Hoặc POST /fishdex/v1/species/batch với body: { specCodes: number[] }
// Hiện tại FE sẽ gọi từng specCode một (không tối ưu) cho đến khi BE implement.

import { apiClient } from './client';
import type {
  CommunitySpeciesDto, SubmitCommunitySpeciesRequest, CommunitySpeciesKind, CommunityImageUploadResultDto,
  CommunityCommonNameDto, SubmitCommonNameRequest,
} from '../../types/community';

const BASE = '/fishdex/v1/species';

// ── Community species (hybrid) ────────────────────────────
export async function submitCommunitySpecies(req: SubmitCommunitySpeciesRequest): Promise<CommunitySpeciesDto> {
  const { data } = await apiClient.post<CommunitySpeciesDto>(`${BASE}/community`, req);
  return data;
}

/** Sửa lại loài đang chờ duyệt (vd: gõ nhầm). Chỉ chủ sở hữu, chỉ khi chưa duyệt/từ chối. */
export async function updateCommunitySpecies(specCode: number, req: SubmitCommunitySpeciesRequest): Promise<CommunitySpeciesDto> {
  const { data } = await apiClient.patch<CommunitySpeciesDto>(`${BASE}/community/${specCode}`, req);
  return data;
}

export async function getMyCommunitySpecies(): Promise<CommunitySpeciesDto[]> {
  const { data } = await apiClient.get<CommunitySpeciesDto[]>(`${BASE}/community/mine`);
  return data;
}

export async function getPendingCommunitySpecies(): Promise<CommunitySpeciesDto[]> {
  const { data } = await apiClient.get<CommunitySpeciesDto[]>(`${BASE}/community/pending`);
  return data;
}

export async function verifyCommunitySpecies(specCode: number, kind?: CommunitySpeciesKind | null): Promise<void> {
  await apiClient.patch(`${BASE}/community/${specCode}/verify`, { kind: kind ?? null });
}

export async function requestCommunitySpeciesImageUpload(
  specCode: number, fileName: string, contentType: string,
): Promise<CommunityImageUploadResultDto> {
  const { data } = await apiClient.post<CommunityImageUploadResultDto>(
    `${BASE}/community/${specCode}/image/presign`, { fileName, contentType },
  );
  return data;
}

export async function rejectCommunitySpecies(specCode: number, reason: string): Promise<void> {
  await apiClient.patch(`${BASE}/community/${specCode}/reject`, { reason });
}

/** User tự xoá hẳn loài mình đã gửi (chỉ khi chưa được duyệt, kể cả đã bị từ chối). */
export async function deleteCommunitySpecies(specCode: number): Promise<void> {
  await apiClient.delete(`${BASE}/community/${specCode}`);
}

// ── Community local names ─────────────────────────────────
export async function submitCommonName(specCode: number, req: SubmitCommonNameRequest): Promise<CommunityCommonNameDto> {
  const { data } = await apiClient.post<CommunityCommonNameDto>(`${BASE}/${specCode}/common-names`, req);
  return data;
}

/** Sửa lại tên đang chờ duyệt (vd: gõ nhầm). Chỉ chủ sở hữu, chỉ khi chưa được duyệt/từ chối. */
export async function updateCommonName(autoCtr: number, comName: string): Promise<CommunityCommonNameDto> {
  const { data } = await apiClient.patch<CommunityCommonNameDto>(`${BASE}/common-names/${autoCtr}`, { comName });
  return data;
}

export async function getMyCommonNames(): Promise<CommunityCommonNameDto[]> {
  const { data } = await apiClient.get<CommunityCommonNameDto[]>(`${BASE}/common-names/mine`);
  return data;
}

export async function getPendingCommonNames(): Promise<CommunityCommonNameDto[]> {
  const { data } = await apiClient.get<CommunityCommonNameDto[]>(`${BASE}/common-names/pending`);
  return data;
}

export async function verifyCommonName(autoCtr: number): Promise<void> {
  await apiClient.patch(`${BASE}/common-names/${autoCtr}/verify`);
}

/** Duyệt hàng loạt — trả về số tên đã duyệt. */
export async function verifyCommonNamesBatch(autoCtrs: number[]): Promise<number> {
  const { data } = await apiClient.patch<{ verified: number }>(`${BASE}/common-names/verify-batch`, { autoCtrs });
  return data.verified;
}

export async function rejectCommonName(autoCtr: number, reason: string): Promise<void> {
  await apiClient.patch(`${BASE}/common-names/${autoCtr}/reject`, { reason });
}

/** User tự xoá hẳn tên mình đã gửi (chỉ khi chưa được duyệt, kể cả đã bị từ chối). */
export async function deleteCommonName(autoCtr: number): Promise<void> {
  await apiClient.delete(`${BASE}/common-names/${autoCtr}`);
}

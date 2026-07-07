import { apiClient } from './client';
import type { PagedResult } from '../../types/common';
import type {
  SnapshotPreviewDto, PublishSnapshotRequest, AquariumSnapshotDto, GetPublicSnapshotsParams, MySnapshotDto,
  ContestDto, CreateContestRequest, UpdateContestRequest,
  SubmitEntryRequest, SubmitEntryResultDto, ContestEntryDto, LeaderboardEntryDto,
} from '../../types/snapshot';

// ── Snapshot: publish flow (auth) ─────────────────────────

export async function previewSnapshot(aquariumId: string): Promise<SnapshotPreviewDto> {
  const { data } = await apiClient.post<SnapshotPreviewDto>(
    `/aquahome/v1/aquariums/${aquariumId}/snapshot/preview`,
  );
  return data;
}

export async function publishSnapshot(aquariumId: string, req: PublishSnapshotRequest = {}): Promise<AquariumSnapshotDto> {
  const { data } = await apiClient.post<AquariumSnapshotDto>(
    `/aquahome/v1/aquariums/${aquariumId}/snapshot/publish`,
    req,
  );
  return data;
}

export async function unpublishSnapshot(snapshotId: string): Promise<void> {
  await apiClient.patch(`/aquahome/v1/snapshots/${snapshotId}/unpublish`);
}

/** Snapshot active của user hiện tại — bản gọn cho contest entry form (auth) */
export async function getMySnapshots(): Promise<MySnapshotDto[]> {
  const { data } = await apiClient.get<MySnapshotDto[]>('/aquahome/v1/snapshots/mine');
  return data;
}

// ── Public gallery (no auth) ──────────────────────────────

export async function getPublicSnapshots(params: GetPublicSnapshotsParams = {}): Promise<PagedResult<AquariumSnapshotDto>> {
  const { data } = await apiClient.get<PagedResult<AquariumSnapshotDto>>(
    '/aquahome/v1/public/snapshots',
    { params },
  );
  return data;
}

export async function getPublicSnapshotBySlug(slug: string): Promise<AquariumSnapshotDto> {
  const { data } = await apiClient.get<AquariumSnapshotDto>(`/aquahome/v1/public/snapshots/${slug}`);
  return data;
}

// ── Like (auth) ───────────────────────────────────────────

export async function likeSnapshot(snapshotId: string): Promise<void> {
  await apiClient.post(`/aquahome/v1/public/snapshots/${snapshotId}/like`);
}

export async function unlikeSnapshot(snapshotId: string): Promise<void> {
  await apiClient.delete(`/aquahome/v1/public/snapshots/${snapshotId}/like`);
}

// ── Contests (public) ─────────────────────────────────────

export async function getActiveContests(): Promise<ContestDto[]> {
  const { data } = await apiClient.get<ContestDto[]>('/aquahome/v1/contests');
  return data;
}

export async function getContestLeaderboard(contestId: string): Promise<LeaderboardEntryDto[]> {
  const { data } = await apiClient.get<LeaderboardEntryDto[]>(`/aquahome/v1/contests/${contestId}/leaderboard`);
  return data;
}

// ── Contest entry (auth) ──────────────────────────────────

export async function submitContestEntry(contestId: string, req: SubmitEntryRequest): Promise<SubmitEntryResultDto> {
  const { data } = await apiClient.post<SubmitEntryResultDto>(`/aquahome/v1/contests/${contestId}/entries`, req);
  return data;
}

/** Gọi sau khi PUT video xong lên R2 — trigger auto-validate + upload YouTube Unlisted */
export async function confirmEntryUpload(contestId: string, entryId: string): Promise<void> {
  await apiClient.post(`/aquahome/v1/contests/${contestId}/entries/${entryId}/confirm-upload`);
}

// ── Contest admin (SystemAdmin) ───────────────────────────

export async function getAllContests(): Promise<ContestDto[]> {
  const { data } = await apiClient.get<ContestDto[]>('/aquahome/v1/admin/contests');
  return data;
}

export async function createContest(req: CreateContestRequest): Promise<ContestDto> {
  const { data } = await apiClient.post<ContestDto>('/aquahome/v1/admin/contests', req);
  return data;
}

export async function updateContest(id: string, req: UpdateContestRequest): Promise<ContestDto> {
  const { data } = await apiClient.put<ContestDto>(`/aquahome/v1/admin/contests/${id}`, req);
  return data;
}

export async function getPendingReviewEntries(): Promise<ContestEntryDto[]> {
  const { data } = await apiClient.get<ContestEntryDto[]>('/aquahome/v1/contests/entries/pending-review');
  return data;
}

export async function approveContestEntry(contestId: string, entryId: string): Promise<void> {
  await apiClient.patch(`/aquahome/v1/contests/${contestId}/entries/${entryId}/approve`);
}

export async function rejectContestEntry(contestId: string, entryId: string): Promise<void> {
  await apiClient.patch(`/aquahome/v1/contests/${contestId}/entries/${entryId}/reject`);
}

import type { WaterType, AquariumStyle } from './aquahome';

// ── Enums (phải khớp với BE AquaHome.Domain.Enums) ────────
export enum ContestAward {
  Participant = 1,
  Top3        = 2,
  Winner      = 3,
}

export enum ContestStatus {
  Draft  = 0,
  Active = 1,
  Ended  = 2,
}

export enum ContestEntryStatus {
  Pending       = 0,
  Validating    = 1,
  UploadedDraft = 2,
  Published     = 3,
  Rejected      = 4,
}

// ── Snapshot Types ────────────────────────────────────────
export interface DistributionPointDto {
  latitudeDec: number;
  longitudeDec: number;
  countryCode: string | null;
  locality: string | null;
}

export interface SnapshotFishDto {
  specCode: number;
  speciesName: string;
  commonName: string | null;
  imageUrl: string | null;
  quantity: number;
  distributionPoints: DistributionPointDto[];
}

/** Nội dung JSONB render-only của snapshot — BE denorm sẵn, public page không gọi thêm API */
export interface SnapshotDataDto {
  aquariumName: string;
  lengthCm: number | null;
  widthCm: number | null;
  heightCm: number | null;
  volumeLiters: number | null;
  description: string | null;
  fish: SnapshotFishDto[];
}

export interface SnapshotPreviewDto {
  waterType: WaterType | null;
  style: AquariumStyle | null;
  fishSpeciesCount: number;
  snapshotData: SnapshotDataDto;
}

export interface PublishSnapshotRequest {
  coverImageUrl?: string | null;
}

export interface AquariumSnapshotDto {
  id: string;
  slug: string;
  waterType: WaterType;
  style: AquariumStyle;
  likeCount: number;
  fishSpeciesCount: number;
  contestAward: ContestAward | null;
  coverImageUrl: string | null;
  youtubeVideoUrl: string | null;
  createdAt: string;
  snapshotData: SnapshotDataDto | null; // null trong gallery list, có data ở detail
  likedByMe: boolean;
}

/** Bản gọn từ GET /snapshots/mine — cho contest entry form chọn bể, không kèm fish list */
export interface MySnapshotDto {
  id: string;
  slug: string;
  aquariumName: string;
  waterType: WaterType;
  style: AquariumStyle;
  fishSpeciesCount: number;
  likeCount: number;
  createdAt: string;
}

export interface GetPublicSnapshotsParams {
  waterType?: WaterType;
  style?: AquariumStyle;
  contest?: 'any' | 'winners';
  sort?: 'likes' | 'newest';
  page?: number;
  pageSize?: number;
}

// ── Contest Types ─────────────────────────────────────────
export interface ContestDto {
  id: string;
  title: string;
  description: string | null;
  youTubePlaylistId: string | null;
  startAt: string;
  endAt: string;
  status: ContestStatus;
}

export interface CreateContestRequest {
  title: string;
  description?: string | null;
  youTubePlaylistId?: string | null;
  startAt: string;
  endAt: string;
}

export interface UpdateContestRequest {
  title?: string | null;
  description?: string | null;
  youTubePlaylistId?: string | null;
  startAt?: string | null;
  endAt?: string | null;
  status?: ContestStatus | null;
}

export interface SubmitEntryRequest {
  aquariumSnapshotId: string;
  fileName: string;
  contentType: string;
  fileSizeBytes: number;
  videoDurationSeconds: number;
}

export interface SubmitEntryResultDto {
  entryId: string;
  uploadUrl: string;
  objectKey: string;
}

export interface ContestEntryDto {
  id: string;
  contestId: string;
  aquariumSnapshotId: string;
  youTubeVideoId: string | null;
  youTubeViewCount: number;
  rank: number | null;
  status: ContestEntryStatus;
  submittedAt: string;
}

export interface LeaderboardEntryDto {
  entryId: string;
  aquariumSnapshotId: string;
  youTubeVideoId: string | null;
  youTubeViewCount: number;
  rank: number | null;
}

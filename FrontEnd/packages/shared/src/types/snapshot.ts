import type { WaterType, AquariumStyle } from './aquahome';

// ── Enums (phải khớp với BE AquaHome.Domain.Enums) ────────
/** Chỉ dùng để chọn màu/icon huy chương — không ảnh hưởng logic xếp hạng */
export enum PrizeTierLevel {
  Gold          = 1, // Giải Nhất
  Silver        = 2, // Giải Nhì
  Bronze        = 3, // Giải Ba
  Encouragement = 4, // Giải Khuyến khích
  Custom        = 5, // Hạng giải admin tự đặt thêm
}

export enum SponsorTier {
  Platinum = 1,
  Gold     = 2,
  Silver   = 3,
  Bronze   = 4,
  Partner  = 5,
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
  ownerNickname: string | null;
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
  /** Id của AquariumMedia đã upload — server ký lại presigned URL mỗi lần trang public được xem */
  coverMediaId?: string | null;
  /** Có giá trị = ghi đè (giữ nguyên URL/slug/lượt thích) snapshot đang active này thay vì tạo mới */
  targetSnapshotId?: string | null;
}

export interface AquariumSnapshotDto {
  id: string;
  slug: string;
  waterType: WaterType;
  style: AquariumStyle;
  likeCount: number;
  fishSpeciesCount: number;
  awardTierName: string | null;
  awardTierLevel: PrizeTierLevel | null;
  coverImageUrl: string | null;
  youtubeVideoUrl: string | null;
  createdAt: string;
  updatedAt: string;
  snapshotData: SnapshotDataDto | null; // null trong gallery list, có data ở detail
  likedByMe: boolean;
}

/** Bản gọn từ GET /snapshots/mine — trang quản lý "bể đã public của tôi" + contest entry form chọn bể */
export interface MySnapshotDto {
  id: string;
  aquariumId: string;
  slug: string;
  aquariumName: string;
  waterType: WaterType;
  style: AquariumStyle;
  fishSpeciesCount: number;
  likeCount: number;
  coverImageUrl: string | null;
  createdAt: string;
  updatedAt: string;
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
  prizeTiers: ContestPrizeTierDto[];
  sponsors: ContestSponsorDto[];
}

// ── Prize tiers ────────────────────────────────────────────
export interface ContestPrizeTierDto {
  id: string;
  name: string;
  tierLevel: PrizeTierLevel;
  slotCount: number;
  displayOrder: number;
  description: string | null;
  imageUrl: string | null;
}

export interface CreatePrizeTierRequest {
  name: string;
  tierLevel: PrizeTierLevel;
  slotCount: number;
  description?: string | null;
}

export interface UpdatePrizeTierRequest {
  name?: string | null;
  tierLevel?: PrizeTierLevel | null;
  slotCount?: number | null;
  displayOrder?: number | null;
  description?: string | null;
}

export interface PrizeTierImageUploadResultDto {
  uploadUrl: string;
  objectKey: string;
}

// ── Sponsors ───────────────────────────────────────────────
export interface ContestSponsorDto {
  id: string;
  name: string;
  /** Website hoặc Facebook Page — link chung, không phân biệt loại */
  websiteUrl: string | null;
  address: string | null;
  logoUrl: string | null;
  sponsorTier: SponsorTier;
  displayOrder: number;
}

export interface CreateSponsorRequest {
  name: string;
  websiteUrl?: string | null;
  address?: string | null;
  sponsorTier: SponsorTier;
}

export interface UpdateSponsorRequest {
  name?: string | null;
  websiteUrl?: string | null;
  address?: string | null;
  sponsorTier?: SponsorTier | null;
  displayOrder?: number | null;
  isActive?: boolean | null;
}

export interface SponsorLogoUploadResultDto {
  uploadUrl: string;
  objectKey: string;
}

// ── Finalize ───────────────────────────────────────────────
export interface EntryAwardAssignment {
  entryId: string;
  prizeTierId: string | null;
}

export interface FinalizeContestRequest {
  assignments: EntryAwardAssignment[];
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
  /** Tên video trên YouTube — bỏ trống thì BE lấy tên bể đã public. Tối đa 100 ký tự. */
  title?: string | null;
  /** Mô tả người dự thi tự viết, tối đa 100 ký tự. */
  description?: string | null;
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
  prizeTierId: string | null;
  prizeTierName: string | null;
  status: ContestEntryStatus;
  submittedAt: string;
  title: string | null;
  description: string | null;
  /** Lý do admin từ chối — hiện lại cho người dự thi biết vì sao trượt. */
  rejectionReason: string | null;
  /** Denorm từ snapshot: admin duyệt / user theo dõi biết đây là bể nào, của ai. */
  aquariumName: string | null;
  ownerNickname: string | null;
  snapshotSlug: string | null;
}

export interface LeaderboardEntryDto {
  entryId: string;
  aquariumSnapshotId: string;
  youTubeVideoId: string | null;
  youTubeViewCount: number;
  prizeTierId: string | null;
  prizeTierName: string | null;
  prizeTierLevel: PrizeTierLevel | null;
}

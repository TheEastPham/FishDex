import type { WaterType } from './aquahome';

/** Khớp BE SnapshotCareLevel. */
export enum CommunityCareLevel {
  Beginner = 0,
  Intermediate = 1,
  Expert = 2,
}

/**
 * Khớp BE CommunitySpeciesKind. Chỉ là gợi ý/phân loại hiển thị — không tự động chạy ETL.
 * Natural = loài có thật (cần admin tự đối chiếu SpecCode FishBase + chạy lại ETL thủ công).
 * Hybrid = loài lai tạo, dùng thẳng data user submit.
 */
export enum CommunitySpeciesKind {
  Natural = 0,
  Hybrid = 1,
}

// ── Community species (loài mới, không có trong FishBase) ──
export interface CommunitySpeciesDto {
  specCode: number;
  speciesName: string;
  commonName: string | null;
  familyName: string | null;
  genusName: string | null;
  waterType: WaterType;
  isVerified: boolean;
  rejectionReason: string | null;
  contributedBy: string | null;
  imageUrl: string | null;
  populatedAt: string;
  suggestedKind: CommunitySpeciesKind | null;
  kind: CommunitySpeciesKind | null;
  tempMin: number | null;
  tempMax: number | null;
  phMin: number | null;
  phMax: number | null;
  dhMin: number | null;
  dhMax: number | null;
  length: number | null;
  longevityCaptive: number | null;
  feedingType: string | null;
  feedingPosition: string | null;
  activityPattern: string | null;
  requiresLiveFood: boolean | null;
  aggressiveness: string | null;
  finNippingRisk: boolean | null;
  jumpingRisk: boolean | null;
  careLevel: CommunityCareLevel | null;
  minTankLiters: number | null;
}

export interface SubmitCommunitySpeciesRequest {
  speciesName: string;
  waterType: WaterType;
  commonName?: string | null;
  familyName?: string | null;
  suggestedKind?: CommunitySpeciesKind | null;
  tempMin?: number | null;
  tempMax?: number | null;
  phMin?: number | null;
  phMax?: number | null;
  dhMin?: number | null;
  dhMax?: number | null;
  length?: number | null;
  longevityCaptive?: number | null;
  feedingType?: string | null;
  aggressiveness?: string | null;
  careLevel?: CommunityCareLevel | null;
  minTankLiters?: number | null;
}

export interface CommunityImageUploadResultDto {
  uploadUrl: string;
  objectKey: string;
}

// ── Community local names (tên địa phương cho loài FishBase) ──
export interface CommunityCommonNameDto {
  autoCtr: number;
  specCode: number;
  comName: string;
  language: string | null;
  countryCode: string | null;
  isVerified: boolean;
  rejectionReason: string | null;
  contributedBy: string | null;
}

export interface SubmitCommonNameRequest {
  comName: string;
  language?: string;
  /**
   * C_Code của FishBase (vd "704" = Việt Nam), khớp `CommonName.CountryCode`.
   * BE nhận field này từ lâu nhưng FE trước đây chưa bao giờ gửi, nên mọi tên cộng đồng cũ
   * đều không gắn quốc gia. Gửi từ nay để tên mới có ngữ cảnh nước.
   */
  countryCode?: string;
}

import type { WaterType } from './aquahome';

/** Khớp BE SnapshotCareLevel. */
export enum CommunityCareLevel {
  Beginner = 0,
  Intermediate = 1,
  Expert = 2,
}

// ── Community species (loài lai tạo, không có trong FishBase) ──
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
}

export interface SubmitCommunitySpeciesRequest {
  speciesName: string;
  waterType: WaterType;
  commonName?: string | null;
  familyName?: string | null;
  genusName?: string | null;
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

// ── Community local names (tên địa phương cho loài FishBase) ──
export interface CommunityCommonNameDto {
  autoCtr: number;
  specCode: number;
  comName: string;
  language: string | null;
  isVerified: boolean;
  rejectionReason: string | null;
  contributedBy: string | null;
}

export interface SubmitCommonNameRequest {
  comName: string;
  language?: string;
  transliteration?: string | null;
  countryCode?: string | null;
}

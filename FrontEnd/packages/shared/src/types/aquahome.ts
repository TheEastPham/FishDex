// ── Enums (phải khớp với BE AquaHome.Domain.Enums) ────────
export enum WaterType {
  Unknown    = 0,
  Freshwater = 1,
  Saltwater  = 2,
  Brackish   = 3,
}

export enum AquariumStyle {
  Unknown    = 0,
  Nature     = 1,
  Dutch      = 2,
  Iwagumi    = 3,
  Biotope    = 4,
  Reef       = 5,
  Blackwater = 6,
  Community  = 7,
  Predator   = 8,
  Paludarium = 9,
}

// ── AquaHome Types ────────────────────────────────────────
export interface AquariumDto {
  id: string;
  name: string;
  lengthCm: number | null;
  widthCm: number | null;
  heightCm: number | null;
  volumeLiters: number | null;
  waterType: WaterType | null;
  style: AquariumStyle | null;
  description: string | null;
  createdAt: string;
  fishCount: number;      // số loài đang nuôi (species count)
  totalQuantity: number;  // tổng số cá thể (sum of quantity)
}

export interface CreateAquariumRequest {
  name: string;
  lengthCm?: number | null;
  widthCm?: number | null;
  heightCm?: number | null;
  waterType?: WaterType | null;
  style?: AquariumStyle | null;
  description?: string | null;
}

export interface UpdateAquariumRequest {
  name?: string | null;
  lengthCm?: number | null;
  widthCm?: number | null;
  heightCm?: number | null;
  waterType?: WaterType | null;
  style?: AquariumStyle | null;
  description?: string | null;
}

export interface AquariumFishDto {
  specCode: number;
  quantity: number;
  addedAt: string;
}

export interface FavoriteDto {
  specCode: number;
}

export interface RecentlyViewedDto {
  specCode: number;
  viewedAt: string;
}

export interface AquariumMediaDto {
  id: string;
  aquariumId: string;
  fileName: string;
  contentType: string;
  createdAt: string;
  url: string | null;
}

export interface PresignedUploadDto {
  mediaId: string;
  uploadUrl: string;
  objectKey: string;
}

// ── Reminders ─────────────────────────────────────────────
export enum AquaTaskType {
  WaterChange    = 0,
  FilterCleaning = 1,
}

export interface ReminderDto {
  id: string;
  aquariumId: string;
  aquaTaskType: AquaTaskType;
  dueAt: string;
  intervalDays: number | null;
  isCompleted: boolean;
  completedAt: string | null;
}

export interface CreateReminderRequest {
  aquaTaskType: AquaTaskType;
  dueAt: string;
  intervalDays: number | null;
}

export interface CompleteReminderResponse {
  completedId: string;
  suggestedNextDueAt: string | null;
}

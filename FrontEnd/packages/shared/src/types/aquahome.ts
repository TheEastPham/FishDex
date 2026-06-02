// ── AquaHome Types ────────────────────────────────────────
export interface AquariumDto {
  id: string;
  name: string;
  lengthCm: number | null;
  widthCm: number | null;
  heightCm: number | null;
  volumeLiters: number | null;
  type: string | null;
  description: string | null;
  createdAt: string;
  fishCount: number;
}

export interface CreateAquariumRequest {
  name: string;
  lengthCm?: number | null;
  widthCm?: number | null;
  heightCm?: number | null;
  type?: string | null;
  description?: string | null;
}

export interface UpdateAquariumRequest {
  name?: string | null;
  lengthCm?: number | null;
  widthCm?: number | null;
  heightCm?: number | null;
  type?: string | null;
  description?: string | null;
}

export interface FavoriteDto {
  specCode: number;
}

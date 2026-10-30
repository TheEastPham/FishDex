export interface SpeciesSummary {
  specCode: number;
  speciesName: string;
  commonName: string | null;
  /** Presigned URL cũ — chết sau 60 phút, KHÔNG an toàn khi cache. Chỉ dùng làm fallback. */
  imageUrl: string | null;
  /** Object key bất biến — render qua useMediaUrl(). Thiếu khi BE còn bản cũ. */
  imageKey?: string | null;
}

export interface SpeciesSearchResult {
  specCode: number;
  speciesName: string;
  preferredCommonName: string | null;
  genusName: string | null;
  familyName: string | null;
  /** Presigned cũ — chỉ dùng làm fallback, render qua useMediaUrl(imageKey, imageUrl). */
  imageUrl?: string | null;
  imageKey?: string | null;
}

export interface Family {
  id: string;
  famCode: number;
  name: string;
  commonName: string | null;
}

export interface SearchSpeciesParams {
  query?: string;
  famId?: string;
  language?: string;
  page?: number;
  pageSize?: number;
}

export interface SpeciesDetail {
  specCode: number;
  speciesName: string;
  preferredCommonName: string | null;
  genusName: string | null;
  familyName: string | null;

  kingdom: string;
  phylum: string;
  className: string | null;
  orderName: string | null;

  waterType: string;
  length: number | null;
  weight: number | null;
  dangerous: string | null;
  demersPelag: string | null;
  lifeCycle: string | null;
  remark: string | null;

  longevityWild: number | null;
  longevityCaptive: number | null;

  /** *ImageUrl: presigned cũ (fallback). *ImageKey: object key — render qua useMediaUrl(). */
  preferredImageUrl: string | null;
  maleImageUrl: string | null;
  femaleImageUrl: string | null;
  preferredImageKey?: string | null;
  maleImageKey?: string | null;
  femaleImageKey?: string | null;

  ecology: {
    feedingType: string | null;
    dietTroph: number | null;
    habitatZones: string[];
    schooling: boolean | null;
    shoaling: boolean | null;
    solitary: boolean | null;
  } | null;

  habitat: {
    preferredSubstrates: string[];
    burrowingCapable: boolean;
    requiresCaves: boolean;
    requiresDriftwood: boolean;
    requiresVegetation: boolean;
    requiresCoralReefs: boolean;
    specialHabitats: string[];
  } | null;

  conservation: {
    iucnCode: string | null;
    iucnAssessment: string | null;
    iucnDateAssessed: string | null;
    citesCode: string | null;
  } | null;

  environment: {
    tempMin: number | null;
    tempMax: number | null;
    tempPreferred: number | null;
    phMin: number | null;
    phMax: number | null;
    dHMin: number | null;
    dHMax: number | null;
    resilience: 'VeryLow' | 'Low' | 'Medium' | 'High' | null;
    resilienceRemark: string | null;
  } | null;
}

export interface SystemImageDto {
  id: string;
  specCode: number;
  name: string;
  pictureType: string;
  picPreferred: boolean | null;
  gender: 'Unknown' | 'Male' | 'Female' | 'Juvenile';
  /** Presigned cũ — fallback. Render qua useMediaUrl(objectKey, url). */
  url: string | null;
  objectKey?: string | null;
}

export interface OccurrenceDto {
  id: number;
  specCode: number;
  countryCode: string | null;
  locality: string | null;
  latitudeDec: number | null;
  longitudeDec: number | null;
  province: string | null;
}

export interface CountryDto {
  code: string;
  name: string;
}

export interface OccurrencePointDto {
  lat: number;
  lon: number;
  locality: string | null;
  province: string | null;
}

export interface CountryDistributionDto {
  code: string;
  name: string;
  alpha2: string | null;
  count: number;
  occurrences: OccurrencePointDto[];
}

export interface SpeciesDistributionDto {
  totalOccurrences: number;
  countries: CountryDistributionDto[];
}

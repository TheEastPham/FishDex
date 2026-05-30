export interface SpeciesSearchResult {
  specCode: number;
  speciesName: string;
  preferredCommonName: string | null;
  genusName: string | null;
  familyName: string | null;
  imageUrl?: string | null;
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
  waterType: string;
  length: number | null;
  weight: number | null;
  dangerous: string | null;
  demersPelag: string | null;
  lifeCycle: string | null;
  remark: string | null;

  preferredImageUrl: string | null;
  maleImageUrl: string | null;
  femaleImageUrl: string | null;

  ecology: {
    feedingType: string | null;
    dietTroph: number | null;
    habitatZones: string[];
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
    phMin: number | null;
    phMax: number | null;
  } | null;
}

export interface SystemImageDto {
  id: string;
  specCode: number;
  name: string;
  pictureType: string;
  picPreferred: boolean | null;
  gender: 'Unknown' | 'Male' | 'Female' | 'Juvenile';
  url: string | null;
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

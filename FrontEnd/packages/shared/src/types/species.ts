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

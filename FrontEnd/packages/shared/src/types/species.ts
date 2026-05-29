export interface SpeciesSearchResult {
  specCode: number;
  speciesName: string;
  preferredCommonName: string | null;
  genusName: string | null;
  familyName: string | null;
}

export interface SearchSpeciesParams {
  query?: string;
  famId?: string;
  language?: string;
  page?: number;
  pageSize?: number;
}

// ── Types ────────────────────────────────────────────────
export type { TokenResponse, UserInfo }           from './types/auth';
export type { PagedResult }                        from './types/common';
export type { SpeciesSearchResult, SearchSpeciesParams, Family, SpeciesDetail, SystemImageDto, OccurrenceDto, CountryDto } from './types/species';

// ── Store ─────────────────────────────────────────────────
export { useAuthStore }                            from './store/authStore';

// ── Auth ──────────────────────────────────────────────────
export { generateCodeVerifier, generateCodeChallenge, generateState } from './lib/auth/pkce';
export {
  buildAuthorizeUrl,
  exchangeCode,
  refreshAccessToken,
  revokeToken,
  buildLogoutUrl,
}                                                  from './lib/auth/oidc';

// ── API clients ───────────────────────────────────────────
// TODO(mobile): client.ts uses window.location — wrap in platform-specific impl
export { apiClient }                               from './lib/api/client';
// TODO(mobile): fishDex.ts uses import.meta.env — pass baseUrl via config object
export { searchSpecies, getFamilies, getSpeciesDetail, getSpeciesMedia, getSpeciesOccurrences, getSpeciesCountries, getRelatedSpecies } from './lib/api/fishDex';

// ── Hooks ─────────────────────────────────────────────────
export { useDebounce }                             from './hooks/useDebounce';
// TODO(mobile): useLogout uses useNavigate (react-router-dom) + window.location
export { useLogout }                               from './hooks/useLogout';
export { useAuthRestore }                          from './hooks/useAuthRestore';

// ── Utils ─────────────────────────────────────────────────
export { cn, getCountryCode }                       from './lib/utils';

// ── i18n ──────────────────────────────────────────────────
export { i18n, setLanguage, useTranslation }       from './i18n';
export { LanguageSwitcher }                        from './components/LanguageSwitcher';

// ── Types ────────────────────────────────────────────────
export type { TokenResponse, UserInfo }           from './types/auth';
export type { PagedResult }                        from './types/common';
export type { SpeciesSearchResult, SpeciesSummary, SearchSpeciesParams, Family, SpeciesDetail, SystemImageDto, OccurrenceDto, CountryDto, OccurrencePointDto, CountryDistributionDto, SpeciesDistributionDto } from './types/species';
export type { AquariumDto, AquariumFishDto, CreateAquariumRequest, UpdateAquariumRequest, FavoriteDto } from './types/aquahome';
export { WaterType, AquariumStyle } from './types/aquahome';


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
export { searchSpecies, getFamilies, getSpeciesDetail, getSpeciesSummaries, getSpeciesMedia, getSpeciesOccurrences, getSpeciesCountries, getSpeciesDistribution, getRelatedSpecies } from './lib/api/fishDex';
export { getMyAquariums, getAquariumById, createAquarium, updateAquarium, deleteAquarium, addFishToAquarium, getAquariumFish, getMyFavorites, checkFavorite, addFavorite, removeFavorite, getRecentlyViewed, recordView } from './lib/api/aquaHome';
export type { RecentlyViewedDto } from './types/aquahome';
export { requestOtp, registerUser, forgotPassword, resetPassword, getMyProfile, updateMyProfile, changePassword } from './lib/api/auth';
export type { RequestOtpResponse, RegisterRequest, RegisterResponse, ForgotPasswordResponse, ResetPasswordPayload, ResetPasswordResponse, UserProfileDto, UpdateProfilePayload, ChangePasswordPayload } from './lib/api/auth';


// ── Cache ─────────────────────────────────────────────────
export { getCached, setCached, invalidateCache, clearCache, CacheKeys, USER_DATA_TTL, FAVORITE_CHECK_TTL, SPECIES_DATA_TTL } from './lib/cache';

// ── Hooks ─────────────────────────────────────────────────
export { useDebounce }                             from './hooks/useDebounce';
// TODO(mobile): useLogout uses useNavigate (react-router-dom) + window.location
export { useLogout }                               from './hooks/useLogout';
export { useAuthRestore }                          from './hooks/useAuthRestore';
export { useFishProfile }                          from './hooks/useFishProfile';
export { useMyFavorites }                          from './hooks/useMyFavorites';
export { useMyAquariums }                          from './hooks/useMyAquariums';
export { useSpeciesSummaries }                     from './hooks/useSpeciesSummaries';

// ── Utils ─────────────────────────────────────────────────
export { cn, getCountryCode }                       from './lib/utils';

// ── i18n ──────────────────────────────────────────────────
export { i18n, setLanguage, useTranslation }       from './i18n';
export { LanguageSwitcher }                        from './components/LanguageSwitcher';

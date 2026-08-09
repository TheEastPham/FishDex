// ── Types ────────────────────────────────────────────────
export type { TokenResponse, UserInfo }           from './types/auth';
export type { PagedResult }                        from './types/common';
export type { SpeciesSearchResult, SpeciesSummary, SearchSpeciesParams, Family, SpeciesDetail, SystemImageDto, OccurrenceDto, CountryDto, OccurrencePointDto, CountryDistributionDto, SpeciesDistributionDto } from './types/species';
export type { AquariumDto, AquariumFishDto, CreateAquariumRequest, UpdateAquariumRequest, FavoriteDto, AquariumMediaDto, PresignedUploadDto, ReminderDto, CreateReminderRequest, CompleteReminderResponse, UserReminderDto } from './types/aquahome';
export { WaterType, AquariumStyle, AquaTaskType } from './types/aquahome';
export type { DistributionPointDto, SnapshotFishDto, SnapshotDataDto, SnapshotPreviewDto, PublishSnapshotRequest, AquariumSnapshotDto, GetPublicSnapshotsParams, MySnapshotDto, ContestDto, CreateContestRequest, UpdateContestRequest, SubmitEntryRequest, SubmitEntryResultDto, ContestEntryDto, LeaderboardEntryDto, ContestPrizeTierDto, CreatePrizeTierRequest, UpdatePrizeTierRequest, PrizeTierImageUploadResultDto, ContestSponsorDto, CreateSponsorRequest, UpdateSponsorRequest, SponsorLogoUploadResultDto, EntryAwardAssignment, FinalizeContestRequest } from './types/snapshot';
export { PrizeTierLevel, SponsorTier, ContestStatus, ContestEntryStatus } from './types/snapshot';
export type { CommunitySpeciesDto, SubmitCommunitySpeciesRequest, CommunityCommonNameDto, SubmitCommonNameRequest, CommunityImageUploadResultDto } from './types/community';
export { CommunityCareLevel, CommunitySpeciesKind } from './types/community';


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
export { searchSpecies, getFamilies, getSpeciesDetail, getSpeciesSummaries, getSpeciesMedia, getSpeciesOccurrences, getSpeciesCountries, getSpeciesDistribution, getSpeciesDistributionsBatch, getRelatedSpecies } from './lib/api/fishDex';
export { getMyAquariums, getAquariumById, createAquarium, updateAquarium, deleteAquarium, addFishToAquarium, getAquariumFish, getMyFavorites, checkFavorite, addFavorite, removeFavorite, getRecentlyViewed, recordView, getAquariumMedia, requestMediaUpload, uploadToR2, confirmMediaUpload, deleteAquariumMedia, getReminders, createReminder, completeReminder, deleteReminder, getAllReminders } from './lib/api/aquaHome';
export type { RecentlyViewedDto } from './types/aquahome';
export { requestOtp, registerUser, forgotPassword, resetPassword, getMyProfile, updateMyProfile, changePassword } from './lib/api/auth';
export type { RequestOtpResponse, RegisterRequest, RegisterResponse, ForgotPasswordResponse, ResetPasswordPayload, ResetPasswordResponse, UserProfileDto, UpdateProfilePayload, ChangePasswordPayload } from './lib/api/auth';
export { previewSnapshot, publishSnapshot, unpublishSnapshot, getMySnapshots, getPublicSnapshots, getPublicSnapshotBySlug, likeSnapshot, unlikeSnapshot, getActiveContests, getContestLeaderboard, submitContestEntry, confirmEntryUpload, getMyContestEntries, getAllContests, createContest, updateContest, getPendingReviewEntries, approveContestEntry, rejectContestEntry, finalizeContest, createPrizeTier, updatePrizeTier, deletePrizeTier, requestPrizeTierImageUpload, createSponsor, updateSponsor, deleteSponsor, requestSponsorLogoUpload } from './lib/api/snapshots';
export { getVapidPublicKey, saveSubscription, removeSubscription } from './lib/api/push';
export type { MarketCountryDto, MarketSpeciesDto, MarketStatsDto, GetMarketSpeciesParams, SpeciesLookupDto, AddTradedSpeciesRequest } from './types/market';
export { TradeStatus, LegalStatus, SizeBand, NameStatusFilter, SpeciesLookupOutcome } from './types/market';
export { getMarketCountries, getMarketSpecies, getMarketStats, getSellingCountries, lookupSpecies, addTradedSpecies, removeTradedSpecies } from './lib/api/market';
export type { MarketCountry } from './lib/countries';
export { MARKET_COUNTRIES, DEFAULT_MARKET_COUNTRY, getCountryFlag, getStoredCountry, storeCountry, findCountry } from './lib/countries';
export { submitCommunitySpecies, updateCommunitySpecies, deleteCommunitySpecies, getMyCommunitySpecies, getPendingCommunitySpecies, verifyCommunitySpecies, rejectCommunitySpecies, requestCommunitySpeciesImageUpload, submitCommonName, updateCommonName, deleteCommonName, getMyCommonNames, getPendingCommonNames, verifyCommonName, verifyCommonNamesBatch, rejectCommonName } from './lib/api/community';
export type { SaveSubscriptionPayload } from './lib/api/push';


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
export { usePushNotification }                     from './hooks/usePushNotification';

// ── Utils ─────────────────────────────────────────────────
export { cn, getCountryCode }                       from './lib/utils';

// ── i18n ──────────────────────────────────────────────────
export { i18n, setLanguage, useTranslation }       from './i18n';
export { LanguageSwitcher }                        from './components/LanguageSwitcher';

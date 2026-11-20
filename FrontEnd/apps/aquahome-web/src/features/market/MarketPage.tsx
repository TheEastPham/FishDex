import { useState, useEffect, useCallback } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import type { MarketCountryDto, MarketSpeciesDto, MarketStatsDto, PagedResult } from '@fishlover/shared';
import {
  useTranslation, NameStatusFilter, useAuthStore, useMyAquariums,
  getMarketCountries, getMarketSpecies, getMarketStats,
  getStoredCountry, storeCountry, DEFAULT_MARKET_COUNTRY,
  getMyFavorites, getCached, setCached, CacheKeys, USER_DATA_TTL,
} from '@fishlover/shared';
import CountrySelect from './components/CountrySelect';
import MarketStats from './components/MarketStats';
import MarketFilters, { type MarketFilterValue } from './components/MarketFilters';
import MarketSpeciesCard from './components/MarketSpeciesCard';
import AddLocalNameModal from '../community/AddLocalNameModal';

const PAGE_SIZE = 24;

/**
 * Trang duyệt cá theo quốc gia.
 *
 * Khác trang tra cứu khoa học ở chỗ **vào là thấy nội dung ngay, không cần gõ gì** — đó là lý do
 * tồn tại của trang này. Và cố ý **không có ô tìm kiếm**: trang `/fish` giữ độc quyền vai trò
 * tìm kiếm, ở đây lọc và lướt là đủ với vài trăm loài.
 */
export default function MarketPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { cc } = useParams<{ cc?: string }>();
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const { aquariums } = useMyAquariums();

  const [countries, setCountries] = useState<MarketCountryDto[]>([]);
  const [country, setCountry] = useState<string>(() => cc?.toUpperCase() ?? getStoredCountry());
  const [filters, setFilters] = useState<MarketFilterValue>({ nameStatus: NameStatusFilter.All });
  const [page, setPage] = useState(1);

  const [result, setResult] = useState<PagedResult<MarketSpeciesDto> | null>(null);
  const [stats, setStats] = useState<MarketStatsDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [namingSpecCode, setNamingSpecCode] = useState<number | null>(null);

  const countryName = t(`countries.${country}`);

  // Prefetch favorites một lần để mỗi thẻ tự giải quyết từ cache, không gọi N lần API
  useEffect(() => {
    if (!isAuthenticated) return;
    if (getCached(CacheKeys.myFavorites()) !== null) return;
    getMyFavorites()
      .then((favs) => setCached(CacheKeys.myFavorites(), favs, USER_DATA_TTL))
      .catch(() => {});
  }, [isAuthenticated]);

  useEffect(() => {
    getMarketCountries()
      .then((list) => {
        setCountries(list);
        // Nước trên URL hoặc trong localStorage có thể đã bị tắt — rơi về nước đầu danh sách.
        if (list.length > 0 && !list.some((c) => c.alpha2 === country)) {
          setCountry(list[0]?.alpha2 ?? DEFAULT_MARKET_COUNTRY);
        }
      })
      .catch(() => setError(t('market.error')));
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  // Đổi nước hoặc đổi bộ lọc thì luôn về trang 1, tránh rơi vào trang trống
  useEffect(() => { setPage(1); }, [country, filters]);

  useEffect(() => {
    storeCountry(country);
    navigate(`/market/${country.toLowerCase()}`, { replace: true });
  }, [country, navigate]);

  // Nước chưa bật thì không gọi endpoint dữ liệu — BE sẽ trả 404, mà đó không phải lỗi,
  // chỉ là chưa tới lượt nước đó.
  const isCountryEnabled = countries.length === 0
    || countries.some((c) => c.alpha2 === country && c.isEnabled);

  const load = useCallback(async () => {
    if (!isCountryEnabled) {
      setResult(null);
      setStats(null);
      setLoading(false);
      return;
    }
    setLoading(true);
    setError(null);
    try {
      // Thống kê tách endpoint riêng vì chỉ COUNT — không đợi ảnh của danh sách
      const [list, s] = await Promise.all([
        getMarketSpecies(country, {
          page,
          pageSize: PAGE_SIZE,
          sizeBand: filters.sizeBand,
          nameStatus: filters.nameStatus,
        }),
        getMarketStats(country),
      ]);
      setResult(list);
      setStats(s);
    } catch {
      setError(t('market.error'));
      setResult(null);
    } finally {
      setLoading(false);
    }
  }, [country, page, filters, t, isCountryEnabled]);

  useEffect(() => { void load(); }, [load]);

  const totalPages = result?.totalPages ?? 0;
  const isEmpty = !loading && result !== null && result.items.length === 0;
  const hasFilter = filters.sizeBand !== undefined || filters.nameStatus !== NameStatusFilter.All;

  return (
    <div className="flex flex-col gap-4 pb-10 pt-6">
      <header>
        {/* Tên nước nằm trong tiêu đề để không mất ngữ cảnh khi nó chỉ là một dropdown */}
        <h1 className="text-xl sm:text-2xl font-semibold text-white tracking-tight">
          {t('market.title', { country: countryName })}
        </h1>
        <p className="text-sm text-slate-400 mt-1">{t('market.subtitle', { country: countryName })}</p>
      </header>

      <div className="flex items-start gap-2 sm:flex-col sm:gap-4">
        <CountrySelect
          countries={countries}
          value={country}
          onChange={setCountry}
          className="flex-1 sm:flex-none sm:w-56"
        />
        <MarketFilters value={filters} onChange={setFilters} resultCount={result?.totalCount} />
      </div>

      {isCountryEnabled && (
        <MarketStats
          stats={stats}
          loading={loading && stats === null}
          onAwaitingClick={() => setFilters((f) => ({ ...f, nameStatus: NameStatusFilter.Missing }))}
        />
      )}

      {!isCountryEnabled && (
        <div className="rounded-2xl border border-dashed border-slate-700/60 bg-[#202226] py-14 px-6 text-center">
          <p className="text-base text-slate-300">{t('market.countryNotEnabled', { country: countryName })}</p>
          <p className="text-sm text-slate-500 mt-2 max-w-md mx-auto">{t('market.countryNotEnabledHint')}</p>
        </div>
      )}

      {error && (
        <div className="rounded-xl border border-red-500/30 bg-red-500/10 px-4 py-3 text-sm text-red-400">
          {error}
        </div>
      )}

      {isCountryEnabled && loading && (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-3">
          {Array.from({ length: 8 }).map((_, i) => (
            <div key={i} className="h-[360px] rounded-xl bg-[#202226] animate-pulse" />
          ))}
        </div>
      )}

      {isCountryEnabled && !loading && result && result.items.length > 0 && (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-3">
          {result.items.map((s, i) => (
            <MarketSpeciesCard
              key={s.specCode}
              species={s}
              index={i}
              aquariums={isAuthenticated ? aquariums : []}
              countryAlpha2={country}
              onAddName={setNamingSpecCode}
            />
          ))}
        </div>
      )}

      {isCountryEnabled && isEmpty && (
        <div className="rounded-2xl border border-dashed border-slate-700/60 bg-[#202226] py-14 px-6 text-center">
          <p className="text-sm text-slate-400">
            {hasFilter ? t('market.emptyFiltered') : t('market.empty', { country: countryName })}
          </p>
        </div>
      )}

      {isCountryEnabled && !loading && totalPages > 1 && (
        <div className="flex items-center justify-center gap-3 pt-4">
          <button
            onClick={() => setPage((p) => Math.max(1, p - 1))}
            disabled={page === 1}
            className="min-h-[44px] px-4 rounded-xl border border-slate-700/60 bg-[#202226] text-sm text-slate-300 hover:bg-slate-800 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
          >
            {t('pagination.prev')}
          </button>
          <span className="text-sm text-slate-400 tabular-nums">
            {t('pagination.page')} <span className="text-sky-300 font-semibold">{page}</span> {t('pagination.of')} {totalPages}
          </span>
          <button
            onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
            disabled={page === totalPages}
            className="min-h-[44px] px-4 rounded-xl border border-slate-700/60 bg-[#202226] text-sm text-slate-300 hover:bg-slate-800 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
          >
            {t('pagination.next')}
          </button>
        </div>
      )}

      {namingSpecCode !== null && (
        <AddLocalNameModal
          specCode={namingSpecCode}
          countryAlpha2={country}
          onClose={() => setNamingSpecCode(null)}
          onSaved={() => { setNamingSpecCode(null); void load(); }}
        />
      )}
    </div>
  );
}

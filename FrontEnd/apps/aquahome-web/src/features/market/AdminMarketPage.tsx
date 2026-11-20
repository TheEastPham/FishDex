import { useState, useEffect, useCallback } from 'react';
import type { MarketCountryDto, MarketSpeciesDto, SpeciesLookupDto } from '@fishlover/shared';
import {
  useTranslation, useDebounce, SpeciesLookupOutcome,
  getMarketCountries, getMarketSpecies, lookupSpecies,
  addTradedSpecies, removeTradedSpecies,
  getStoredCountry, storeCountry,
} from '@fishlover/shared';
import { Search, Plus, Trash2, Copy, Check, Loader2, AlertCircle } from 'lucide-react';
import CountrySelect from './components/CountrySelect';

const PAGE_SIZE = 100;

/**
 * Quản lý danh sách market — chỉ SystemAdmin/ContentAdmin (route đã bọc RoleGuard).
 *
 * Hai việc: tra loài để thêm vào danh sách, và gỡ loài khỏi danh sách.
 *
 * <b>Về "hàng đợi UC2":</b> kế hoạch ban đầu định làm hàng đợi các loài người dùng yêu cầu nạp,
 * nhưng chưa có bảng lưu yêu cầu nên chưa có nguồn dữ liệu. Bản này làm theo hướng thực tế hơn:
 * admin tra tên trên index toàn bộ FishBase, loài nào chưa nạp thì copy `SpecCode` để dán vào
 * `new_spec_codes.txt` rồi chạy ETL theo lô.
 */
export default function AdminMarketPage() {
  const { t } = useTranslation();

  const [countries, setCountries] = useState<MarketCountryDto[]>([]);
  const [country, setCountry] = useState(getStoredCountry());
  const [list, setList] = useState<MarketSpeciesDto[]>([]);
  const [loading, setLoading] = useState(true);

  const [query, setQuery] = useState('');
  const debounced = useDebounce(query, 400);
  const [hits, setHits] = useState<SpeciesLookupDto[]>([]);
  const [busyCode, setBusyCode] = useState<number | null>(null);
  const [copied, setCopied] = useState(false);

  useEffect(() => {
    getMarketCountries().then(setCountries).catch(() => {});
  }, []);

  const loadList = useCallback(async () => {
    setLoading(true);
    try {
      const res = await getMarketSpecies(country, { page: 1, pageSize: PAGE_SIZE });
      setList(res.items);
    } catch {
      setList([]);
    } finally {
      setLoading(false);
    }
  }, [country]);

  useEffect(() => { storeCountry(country); void loadList(); }, [country, loadList]);

  useEffect(() => {
    if (!debounced.trim()) { setHits([]); return; }
    let cancelled = false;
    lookupSpecies(debounced.trim(), 10)
      .then((r) => { if (!cancelled) setHits(r); })
      .catch(() => { if (!cancelled) setHits([]); });
    return () => { cancelled = true; };
  }, [debounced]);

  const inList = new Set(list.map((s) => s.specCode));

  const add = async (specCode: number) => {
    setBusyCode(specCode);
    try {
      await addTradedSpecies(country, { specCode });
      await loadList();
    } catch {
      // giữ nguyên trạng thái — danh sách sẽ phản ánh sự thật ở lần tải sau
    } finally {
      setBusyCode(null);
    }
  };

  const remove = async (specCode: number) => {
    setBusyCode(specCode);
    try {
      await removeTradedSpecies(country, specCode);
      await loadList();
    } finally {
      setBusyCode(null);
    }
  };

  // Gom SpecCode của các loài CHƯA nạp để dán một lần vào new_spec_codes.txt
  const pendingCodes = hits
    .filter((h) => h.outcome === SpeciesLookupOutcome.NeedsMigration)
    .map((h) => h.specCode);

  const copyCodes = async () => {
    await navigator.clipboard.writeText(pendingCodes.join(','));
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  return (
    <div className="flex flex-col gap-5 pb-10 pt-6">
      <header>
        <h1 className="text-xl sm:text-2xl font-semibold text-white">{t('nav.marketAdmin')}</h1>
        <p className="text-sm text-slate-400 mt-1">
          {t('market.subtitle', { country: t(`countries.${country}`) })}
        </p>
      </header>

      <CountrySelect countries={countries} value={country} onChange={setCountry} className="w-full sm:w-64" />

      {/* Tra loài để thêm */}
      <section className="rounded-xl bg-[#202226] border border-slate-800 p-4">
        <div className="relative">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-500" />
          <input
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder={t('fish.placeholder')}
            className="w-full rounded-xl bg-[#141518] border border-slate-700/60 pl-10 pr-4 py-3 text-base text-slate-200 placeholder:text-slate-500 focus:outline-none focus:border-primary/50 min-h-[44px]"
          />
        </div>

        {pendingCodes.length > 0 && (
          <div className="mt-3 flex items-center justify-between gap-3 rounded-lg border border-amber-500/30 bg-amber-500/10 px-3 py-2.5">
            <p className="text-xs text-amber-300 leading-snug">
              {pendingCodes.length} loài chưa có trong FishDex — dán mã vào <code className="font-mono">new_spec_codes.txt</code> rồi chạy lại ETL.
            </p>
            <button
              onClick={copyCodes}
              className="shrink-0 inline-flex items-center gap-1.5 rounded-lg border border-amber-500/40 bg-amber-500/20 px-3 min-h-[36px] text-xs font-medium text-amber-200 hover:bg-amber-500/30 transition-colors"
            >
              {copied ? <Check className="w-3.5 h-3.5" /> : <Copy className="w-3.5 h-3.5" />}
              {copied ? 'Đã copy' : 'Copy mã'}
            </button>
          </div>
        )}

        <div className="mt-3 flex flex-col gap-1.5">
          {hits.map((h) => {
            const already = inList.has(h.specCode);
            const needsMigration = h.outcome === SpeciesLookupOutcome.NeedsMigration;
            return (
              <div key={h.specCode} className="flex items-center gap-3 rounded-lg bg-[#141518] px-3 py-2.5">
                <div className="min-w-0 flex-1">
                  <p className="text-sm text-slate-200 italic truncate">{h.scientificName}</p>
                  <p className="text-[11px] text-slate-500 font-mono">#{h.specCode}</p>
                </div>

                {needsMigration ? (
                  <span className="inline-flex items-center gap-1 text-[11px] text-amber-400 shrink-0">
                    <AlertCircle className="w-3.5 h-3.5" />
                    chưa nạp
                  </span>
                ) : already ? (
                  <span className="text-[11px] text-emerald-400 shrink-0">đã có</span>
                ) : (
                  <button
                    onClick={() => add(h.specCode)}
                    disabled={busyCode === h.specCode}
                    className="shrink-0 inline-flex items-center gap-1.5 rounded-lg bg-primary/15 border border-primary/35 px-3 min-h-[36px] text-xs font-medium text-sky-300 hover:bg-primary/25 transition-colors disabled:opacity-50"
                  >
                    {busyCode === h.specCode ? <Loader2 className="w-3.5 h-3.5 animate-spin" /> : <Plus className="w-3.5 h-3.5" />}
                    Thêm
                  </button>
                )}
              </div>
            );
          })}
        </div>
      </section>

      {/* Danh sách hiện tại */}
      <section>
        <p className="text-sm text-slate-400 mb-2">
          <span className="text-white font-semibold tabular-nums">{list.length}</span> loài trong danh sách
        </p>

        {loading ? (
          <div className="space-y-1.5">
            {Array.from({ length: 6 }).map((_, i) => (
              <div key={i} className="h-14 rounded-lg bg-[#202226] animate-pulse" />
            ))}
          </div>
        ) : (
          <div className="flex flex-col gap-1.5">
            {list.map((s) => (
              <div key={s.specCode} className="flex items-center gap-3 rounded-lg bg-[#202226] border border-slate-800 px-3 py-2.5">
                <div className="min-w-0 flex-1">
                  <p className="text-sm text-white truncate">{s.localName ?? s.scientificName}</p>
                  {s.localName && <p className="text-[11px] text-slate-500 italic truncate">{s.scientificName}</p>}
                </div>
                <button
                  onClick={() => remove(s.specCode)}
                  disabled={busyCode === s.specCode}
                  title={t('market.removeFromCountry', { country: t(`countries.${country}`) })}
                  className="shrink-0 grid place-items-center min-w-[44px] min-h-[44px] rounded-lg text-slate-500 hover:text-red-400 hover:bg-red-500/10 transition-colors disabled:opacity-50"
                >
                  {busyCode === s.specCode ? <Loader2 className="w-4 h-4 animate-spin" /> : <Trash2 className="w-4 h-4" />}
                </button>
              </div>
            ))}
          </div>
        )}
      </section>
    </div>
  );
}

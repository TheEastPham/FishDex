import { useState, useEffect } from 'react';
import { Search, Filter, Sparkles } from 'lucide-react';
import { useDebounce, searchSpecies, getFamilies, useTranslation } from '@fishlover/shared';
import type { PagedResult, SpeciesSearchResult, Family } from '@fishlover/shared';
import SpeciesCard from './components/SpeciesCard';
import SpeciesCardSkeleton from './components/SpeciesCardSkeleton';

const PAGE_SIZE = 12;

export default function FishSearchPage() {
  const { t } = useTranslation();
  const [query, setQuery]     = useState('');
  const [page, setPage]       = useState(1);
  const [loading, setLoading] = useState(false);
  const [result, setResult]   = useState<PagedResult<SpeciesSearchResult> | null>(null);
  const [error, setError]     = useState<string | null>(null);
  const [families, setFamilies] = useState<Family[]>([]);
  const [selectedFamily, setSelectedFamily] = useState<string>('');

  const debouncedQuery = useDebounce(query, 400);

  useEffect(() => {
    let cancelled = false;
    getFamilies().then(data => {
      if (!cancelled) setFamilies(data);
    }).catch(console.error);
    return () => { cancelled = true; };
  }, []);

  useEffect(() => {
    setPage(1);
  }, [debouncedQuery, selectedFamily]);

  useEffect(() => {
    if (!debouncedQuery.trim() && !selectedFamily) {
      setResult(null);
      setError(null);
      return;
    }

    let cancelled = false;
    setLoading(true);
    setError(null);

    searchSpecies({ 
      query: debouncedQuery.trim() || undefined, 
      famId: selectedFamily || undefined,
      language: 'en', 
      page, 
      pageSize: PAGE_SIZE 
    })
      .then((data) => { if (!cancelled) setResult(data); })
      .catch(() => { if (!cancelled) setError(t('fish.error')); })
      .finally(() => { if (!cancelled) setLoading(false); });

    return () => { cancelled = true; };
  }, [debouncedQuery, selectedFamily, page, t]);

  const totalPages = result?.totalPages ?? 0;

  return (
    <div className="flex flex-col gap-8 pb-10">

      {/* Hero Header */}
      <div className="relative overflow-hidden rounded-[2rem] bg-gradient-to-r from-primary to-blue-600 px-6 py-8 sm:py-12 shadow-sm">
        {/* Decorative background elements */}
        <div className="absolute top-0 right-0 h-full w-1/2 bg-gradient-to-l from-white/10 to-transparent pointer-events-none" />
        <div className="absolute -top-24 -right-24 h-64 w-64 rounded-full bg-white/10 blur-3xl pointer-events-none" />
        <div className="absolute -bottom-24 -left-24 h-64 w-64 rounded-full bg-blue-400/20 blur-3xl pointer-events-none" />
        
        <div className="relative z-10 flex flex-col items-center text-center max-w-2xl mx-auto">
          <div className="inline-flex items-center gap-1.5 rounded-full bg-white/10 px-3 py-1 text-xs font-semibold text-white/90 backdrop-blur-md mb-4 border border-white/10">
            <Sparkles className="h-3.5 w-3.5 text-blue-200" />
            <span>Discover Aquatic Life</span>
          </div>
          <h1 className="text-3xl sm:text-4xl font-extrabold text-white tracking-tight mb-3">
            {t('fish.title')}
          </h1>
          <p className="text-blue-100 text-sm sm:text-base max-w-lg">
            {t('fish.subtitle')}
          </p>
        </div>
      </div>

      {/* Search controls - Floating Bar */}
      <div className="relative -mt-10 z-20 mx-auto w-full max-w-4xl flex flex-col sm:flex-row gap-2 rounded-2xl bg-white p-2 shadow-lg shadow-slate-200/50 border border-slate-100">
        
        <div className="relative w-full sm:w-1/3 flex items-center bg-slate-50/50 rounded-xl border border-transparent hover:bg-slate-100/50 transition-colors">
          <Filter className="absolute left-4 h-4 w-4 text-primary pointer-events-none" />
          <select
            value={selectedFamily}
            onChange={(e) => setSelectedFamily(e.target.value)}
            className="w-full rounded-xl bg-transparent pl-11 pr-8 py-3 text-sm font-medium text-slate-700
                       focus:outline-none focus:ring-2 focus:ring-primary/20 
                       transition-all appearance-none cursor-pointer"
          >
            <option value="">{t('fish.allFamilies') || 'All Families'}</option>
            {families.map(f => (
              <option key={f.id} value={f.id}>
                {f.name} {f.commonName ? `(${f.commonName})` : ''}
              </option>
            ))}
          </select>
          {/* Custom dropdown arrow */}
          <div className="absolute right-4 pointer-events-none text-slate-400">
             <svg width="10" height="6" viewBox="0 0 10 6" fill="none" xmlns="http://www.w3.org/2000/svg">
                <path d="M1 1L5 5L9 1" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/>
             </svg>
          </div>
        </div>

        <div className="relative w-full sm:w-2/3 group">
          <Search className="absolute left-4 top-1/2 -translate-y-1/2 h-5 w-5 text-slate-400 pointer-events-none transition-colors group-focus-within:text-primary" />
          <input
            type="text"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder={t('fish.placeholder')}
            className="w-full rounded-xl bg-slate-50/50 pl-12 pr-4 py-3 text-sm text-slate-700 font-medium
                       placeholder:text-slate-400 placeholder:font-normal
                       focus:outline-none focus:bg-white focus:ring-2 focus:ring-primary/20 hover:bg-slate-100/50
                       transition-all"
            autoFocus
          />
        </div>
      </div>

      {/* Results count */}
      {result && !loading && (
        <div className="px-2">
          <p className="text-sm text-slate-500 font-medium">
            <span className="text-slate-900 bg-slate-100 px-2 py-0.5 rounded-md mr-1">{result.totalCount.toLocaleString()}</span>
            {t('fish.results')}{' '}
            {debouncedQuery && (
              <>
                <span>for</span> <span className="text-primary">"{debouncedQuery}"</span>
              </>
            )}
          </p>
        </div>
      )}

      {/* Error */}
      {error && (
        <div className="rounded-2xl border border-red-200 bg-red-50 px-5 py-4 text-sm text-red-600 shadow-sm flex items-center gap-3">
          <div className="h-2 w-2 rounded-full bg-red-500 animate-pulse" />
          {error}
        </div>
      )}

      {/* Loading skeletons */}
      {loading && (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
          {Array.from({ length: 8 }).map((_, i) => (
            <SpeciesCardSkeleton key={i} />
          ))}
        </div>
      )}

      {/* Results grid */}
      {!loading && result && result.items.length > 0 && (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
          {result.items.map((species, index) => (
            <SpeciesCard
              key={species.specCode}
              species={species}
              aquariumReady={false}
              index={index}
            />
          ))}
        </div>
      )}

      {/* Empty result */}
      {!loading && result && result.items.length === 0 && (
        <div className="flex flex-col items-center justify-center py-20 text-center bg-white rounded-3xl border border-dashed border-slate-200">
          <div className="mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-slate-50 ring-8 ring-slate-50/50">
            <Search className="h-6 w-6 text-slate-400" />
          </div>
          <h3 className="text-lg font-semibold text-slate-900 mb-1">No results found</h3>
          <p className="text-slate-500 text-sm max-w-sm">
            {t('fish.emptyResult')}{' '}
            <span className="font-medium text-slate-700">"{debouncedQuery}"</span>. Try adjusting your search or filters.
          </p>
        </div>
      )}

      {/* Empty state */}
      {!loading && !result && !error && (
        <div className="flex flex-col items-center justify-center py-20 text-center bg-white rounded-3xl border border-dashed border-slate-200">
          <div className="mb-4 flex h-16 w-16 items-center justify-center rounded-2xl bg-primary/10 rotate-3 transition-transform hover:rotate-6">
            <Search className="h-6 w-6 text-primary" />
          </div>
          <h3 className="text-lg font-semibold text-slate-900 mb-1">Search the FishDex</h3>
          <p className="text-sm text-slate-500 max-w-sm">{t('fish.emptyState')}</p>
        </div>
      )}

      {/* Pagination */}
      {!loading && totalPages > 1 && (
        <div className="flex items-center justify-center gap-2 pt-8 pb-4">
          <button
            onClick={() => setPage((p) => Math.max(1, p - 1))}
            disabled={page === 1}
            className="rounded-xl border border-slate-200 bg-white px-4 py-2.5 text-sm font-medium text-slate-700
                       hover:bg-slate-50 hover:text-slate-900 disabled:opacity-50 disabled:bg-slate-50 disabled:cursor-not-allowed transition-all shadow-sm"
          >
            {t('pagination.prev')}
          </button>
          
          <div className="flex items-center gap-1 px-4 text-sm font-medium text-slate-500">
            {t('pagination.page')}{' '}
            <span className="inline-flex items-center justify-center min-w-[2rem] h-8 rounded-lg bg-primary/10 text-primary font-bold">
              {page}
            </span>
            {' '}{t('pagination.of')}{' '}
            <span className="font-semibold text-slate-700">{totalPages}</span>
          </div>
          
          <button
            onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
            disabled={page === totalPages}
            className="rounded-xl border border-slate-200 bg-white px-4 py-2.5 text-sm font-medium text-slate-700
                       hover:bg-slate-50 hover:text-slate-900 disabled:opacity-50 disabled:bg-slate-50 disabled:cursor-not-allowed transition-all shadow-sm"
          >
            {t('pagination.next')}
          </button>
        </div>
      )}

    </div>
  );
}

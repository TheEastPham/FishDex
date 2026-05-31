import { useState, useEffect } from 'react';
import { Search, Sparkles } from 'lucide-react';
import { useDebounce, searchSpecies, getFamilies, useTranslation } from '@fishlover/shared';
import type { PagedResult, SpeciesSearchResult, Family } from '@fishlover/shared';
import SpeciesCard from './components/SpeciesCard';
import SpeciesCardSkeleton from './components/SpeciesCardSkeleton';
import FamilySelect from './components/FamilySelect';

const PAGE_SIZE = 12;

export default function FishSearchPage() {
  const { t, i18n } = useTranslation();
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
      language: i18n.language, 
      page, 
      pageSize: PAGE_SIZE 
    })
      .then((data) => { if (!cancelled) setResult(data); })
      .catch(() => { if (!cancelled) setError(t('fish.error')); })
      .finally(() => { if (!cancelled) setLoading(false); });

    return () => { cancelled = true; };
  }, [debouncedQuery, selectedFamily, page, t, i18n.language]);

  const handleFamilyClick = (familyName: string) => {
    const family = families.find(f => f.name.toLowerCase() === familyName.toLowerCase());
    if (family) {
      setSelectedFamily(family.id);
      setQuery(''); // Optional: clear search if they just want the family
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }
  };

  const totalPages = result?.totalPages ?? 0;

  return (
    <div className="flex flex-col gap-8 pb-10">

      {/* Hero Header */}
      <div className="relative overflow-hidden bg-transparent pt-8 pb-16">
        {/* Subtle glowing accents instead of a solid blue block */}
        <div className="absolute top-0 left-1/2 -translate-x-1/2 h-64 w-2/3 bg-primary/10 blur-[100px] pointer-events-none rounded-full" />
        
        <div className="relative z-10 flex flex-col items-center text-center max-w-2xl mx-auto px-4">
          <div className="inline-flex items-center gap-1.5 rounded-full bg-primary/10 border border-primary/20 px-3 py-1 text-xs font-semibold text-primary mb-6 shadow-inner">
            <Sparkles className="h-3.5 w-3.5" />
            <span>Discover Aquatic Life</span>
          </div>
          <h1 className="text-3xl sm:text-4xl font-bold text-white tracking-tight mb-4 leading-tight">
            {t('fish.title')}
          </h1>
          <p className="text-slate-400 text-sm sm:text-base max-w-xl leading-relaxed">
            {t('fish.subtitle')}
          </p>
        </div>
      </div>

      {/* Search controls - Floating Bar */}
      <div className="relative -mt-10 z-20 mx-auto w-full max-w-4xl flex flex-col sm:flex-row gap-2 rounded-2xl bg-[#202226] p-2 shadow-2xl shadow-black/50 border border-slate-700/50">
        
        <FamilySelect 
          families={families} 
          value={selectedFamily} 
          onChange={setSelectedFamily} 
        />

        <div className="relative w-full sm:w-2/3 group">
          <Search className="absolute left-4 top-1/2 -translate-y-1/2 h-5 w-5 text-slate-400 pointer-events-none transition-colors group-focus-within:text-primary" />
          <input
            type="text"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder={t('fish.placeholder')}
            className="w-full rounded-xl bg-[#141518] pl-12 pr-4 py-3 text-sm text-slate-200 font-medium
                       placeholder:text-slate-500 placeholder:font-normal border border-transparent
                       focus:outline-none focus:border-primary/50 focus:ring-4 focus:ring-primary/20 hover:bg-[#1a1c20]
                       transition-all"
            autoFocus
          />
        </div>
      </div>

      {/* Results count */}
      {result && !loading && (
        <div className="px-2">
          <p className="text-sm text-slate-400 font-medium">
            <span className="text-white bg-slate-800 px-2 py-0.5 rounded-md mr-1">{result.totalCount.toLocaleString()}</span>
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
        <div className="rounded-2xl border border-red-500/30 bg-red-500/10 px-5 py-4 text-sm text-red-400 shadow-sm flex items-center gap-3">
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
              index={index}
              onFamilyClick={handleFamilyClick}
            />
          ))}
        </div>
      )}

      {/* Empty result */}
      {!loading && result && result.items.length === 0 && (
        <div className="flex flex-col items-center justify-center py-20 text-center bg-[#202226] rounded-3xl border border-dashed border-slate-700/50">
          <div className="mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-[#141518] ring-8 ring-[#141518]/50">
            <Search className="h-6 w-6 text-slate-500" />
          </div>
          <h3 className="text-lg font-semibold text-slate-200 mb-1">No results found</h3>
          <p className="text-slate-400 text-sm max-w-sm">
            {t('fish.emptyResult')}{' '}
            <span className="font-medium text-slate-300">"{debouncedQuery}"</span>. Try adjusting your search or filters.
          </p>
        </div>
      )}

      {/* Empty state */}
      {!loading && !result && !error && (
        <div className="flex flex-col items-center justify-center py-20 text-center bg-[#202226] rounded-3xl border border-dashed border-slate-700/50 shadow-lg">
          <div className="mb-4 flex h-16 w-16 items-center justify-center rounded-2xl bg-primary/20 rotate-3 transition-transform hover:rotate-6 border border-primary/30 shadow-inner">
            <Search className="h-6 w-6 text-primary" />
          </div>
          <h3 className="text-lg font-semibold text-white mb-1">Search the FishDex</h3>
          <p className="text-sm text-slate-400 max-w-sm">{t('fish.emptyState')}</p>
        </div>
      )}

      {/* Pagination */}
      {!loading && totalPages > 1 && (
        <div className="flex items-center justify-center gap-2 pt-8 pb-4">
          <button
            onClick={() => setPage((p) => Math.max(1, p - 1))}
            disabled={page === 1}
            className="rounded-xl border border-slate-700/50 bg-[#202226] px-4 py-2.5 text-sm font-medium text-slate-300
                       hover:bg-slate-700/50 hover:text-white disabled:opacity-50 disabled:bg-[#202226] disabled:cursor-not-allowed transition-all shadow-sm"
          >
            {t('pagination.prev')}
          </button>
          
          <div className="flex items-center gap-1 px-4 text-sm font-medium text-slate-500">
            {t('pagination.page')}{' '}
            <span className="inline-flex items-center justify-center min-w-[2rem] h-8 rounded-lg bg-primary/20 text-primary font-bold border border-primary/30">
              {page}
            </span>
            {' '}{t('pagination.of')}{' '}
            <span className="font-semibold text-slate-300">{totalPages}</span>
          </div>
          
          <button
            onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
            disabled={page === totalPages}
            className="rounded-xl border border-slate-700/50 bg-[#202226] px-4 py-2.5 text-sm font-medium text-slate-300
                       hover:bg-slate-700/50 hover:text-white disabled:opacity-50 disabled:bg-[#202226] disabled:cursor-not-allowed transition-all shadow-sm"
          >
            {t('pagination.next')}
          </button>
        </div>
      )}

    </div>
  );
}

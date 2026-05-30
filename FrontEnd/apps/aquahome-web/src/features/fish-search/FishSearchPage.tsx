import { useState, useEffect } from 'react';
import { Search } from 'lucide-react';
import { useDebounce, searchSpecies, getFamilies, useTranslation } from '@fishlover/shared';
import type { PagedResult, SpeciesSearchResult, Family } from '@fishlover/shared';
import SpeciesCard from './components/SpeciesCard';
import SpeciesCardSkeleton from './components/SpeciesCardSkeleton';

const PAGE_SIZE = 18;

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
    <div className="space-y-6">

      {/* Header */}
      <div>
        <h1 className="text-xl font-semibold text-foreground">{t('fish.title')}</h1>
        <p className="text-sm text-muted-foreground mt-0.5">{t('fish.subtitle')}</p>
      </div>

      {/* Search controls */}
      <div className="flex flex-col sm:flex-row gap-3 max-w-2xl">
        <select
          value={selectedFamily}
          onChange={(e) => setSelectedFamily(e.target.value)}
          className="w-full sm:w-1/3 rounded-xl border border-input bg-background px-4 py-2.5 text-sm
                     focus:outline-none focus:ring-2 focus:ring-ring focus:border-transparent
                     transition-shadow text-foreground appearance-none"
        >
          <option value="">{t('fish.allFamilies') || 'All Families'}</option>
          {families.map(f => (
            <option key={f.id} value={f.id}>
              {f.name} {f.commonName ? `(${f.commonName})` : ''}
            </option>
          ))}
        </select>
        <div className="relative w-full sm:w-2/3">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground pointer-events-none" />
          <input
            type="text"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder={t('fish.placeholder')}
            className="w-full rounded-xl border border-input bg-background pl-9 pr-4 py-2.5 text-sm
                       placeholder:text-muted-foreground
                       focus:outline-none focus:ring-2 focus:ring-ring focus:border-transparent
                       transition-shadow"
            autoFocus
          />
        </div>
      </div>

      {/* Results count */}
      {result && !loading && (
        <p className="text-sm text-muted-foreground">
          <span className="font-medium text-foreground">{result.totalCount.toLocaleString()}</span>
          {' '}{t('fish.results')}{' '}
          <span className="font-medium text-foreground">"{debouncedQuery}"</span>
        </p>
      )}

      {/* Error */}
      {error && (
        <div className="rounded-xl border border-destructive/30 bg-destructive/5 px-4 py-3 text-sm text-destructive">
          {error}
        </div>
      )}

      {/* Loading skeletons */}
      {loading && (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
          {Array.from({ length: 6 }).map((_, i) => (
            <SpeciesCardSkeleton key={i} />
          ))}
        </div>
      )}

      {/* Results grid */}
      {!loading && result && result.items.length > 0 && (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
          {result.items.map((species) => (
            <SpeciesCard
              key={species.specCode}
              species={species}
              aquariumReady={false}
            />
          ))}
        </div>
      )}

      {/* Empty result */}
      {!loading && result && result.items.length === 0 && (
        <div className="flex flex-col items-center py-16 text-center">
          <p className="text-muted-foreground text-sm">
            {t('fish.emptyResult')}{' '}
            <span className="font-medium">"{debouncedQuery}"</span>
          </p>
        </div>
      )}

      {/* Empty state */}
      {!loading && !result && !error && (
        <div className="flex flex-col items-center py-16 text-center">
          <div className="mb-3 flex h-12 w-12 items-center justify-center rounded-full bg-muted">
            <Search className="h-5 w-5 text-muted-foreground" />
          </div>
          <p className="text-sm text-muted-foreground">{t('fish.emptyState')}</p>
        </div>
      )}

      {/* Pagination */}
      {!loading && totalPages > 1 && (
        <div className="flex items-center justify-center gap-3 pt-2">
          <button
            onClick={() => setPage((p) => Math.max(1, p - 1))}
            disabled={page === 1}
            className="rounded-lg border border-border px-4 py-2 text-sm font-medium
                       hover:bg-muted disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
          >
            {t('pagination.prev')}
          </button>
          <span className="text-sm text-muted-foreground">
            {t('pagination.page')}{' '}
            <span className="font-medium text-foreground">{page}</span>
            {' '}{t('pagination.of')}{' '}{totalPages}
          </span>
          <button
            onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
            disabled={page === totalPages}
            className="rounded-lg border border-border px-4 py-2 text-sm font-medium
                       hover:bg-muted disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
          >
            {t('pagination.next')}
          </button>
        </div>
      )}

    </div>
  );
}

import { useState, useEffect } from 'react';
import { Search } from 'lucide-react';
import { useDebounce } from '@/hooks/useDebounce';
import { searchSpecies } from '@/lib/api/fishDex';
import type { PagedResult } from '@/types/common';
import type { SpeciesSearchResult } from '@/types/species';
import SpeciesCard from './components/SpeciesCard';
import SpeciesCardSkeleton from './components/SpeciesCardSkeleton';

const PAGE_SIZE = 18;

export default function FishSearchPage() {
  const [query, setQuery]               = useState('');
  const [page, setPage]                 = useState(1);
  const [loading, setLoading]           = useState(false);
  const [result, setResult]             = useState<PagedResult<SpeciesSearchResult> | null>(null);
  const [error, setError]               = useState<string | null>(null);

  const debouncedQuery = useDebounce(query, 400);

  useEffect(() => {
    setPage(1);
  }, [debouncedQuery]);

  useEffect(() => {
    // Không search khi query rỗng — hiển thị empty state
    if (!debouncedQuery.trim()) {
      setResult(null);
      setError(null);
      return;
    }

    let cancelled = false;
    setLoading(true);
    setError(null);

    searchSpecies({ query: debouncedQuery.trim(), language: 'en', page, pageSize: PAGE_SIZE })
      .then((data) => { if (!cancelled) setResult(data); })
      .catch(() => { if (!cancelled) setError('Không thể tải kết quả. Kiểm tra FishDex Service.'); })
      .finally(() => { if (!cancelled) setLoading(false); });

    return () => { cancelled = true; };
  }, [debouncedQuery, page]);

  const totalPages = result?.totalPages ?? 0;

  return (
    <div className="space-y-6">

      {/* Header */}
      <div>
        <h1 className="text-xl font-semibold text-foreground">Fish Search</h1>
        <p className="text-sm text-muted-foreground mt-0.5">
          Tìm kiếm trong ~8,883 loài cá aquarium từ FishBase
        </p>
      </div>

      {/* Search input */}
      <div className="relative max-w-lg">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground pointer-events-none" />
        <input
          type="text"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          placeholder="Tên khoa học hoặc tên thường gọi..."
          className="w-full rounded-xl border border-input bg-background pl-9 pr-4 py-2.5 text-sm
                     placeholder:text-muted-foreground
                     focus:outline-none focus:ring-2 focus:ring-ring focus:border-transparent
                     transition-shadow"
          autoFocus
        />
      </div>

      {/* Results count */}
      {result && !loading && (
        <p className="text-sm text-muted-foreground">
          <span className="font-medium text-foreground">{result.totalCount.toLocaleString()}</span>
          {' '}kết quả cho{' '}
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
            Không tìm thấy loài nào cho <span className="font-medium">"{debouncedQuery}"</span>
          </p>
        </div>
      )}

      {/* Empty state — chưa nhập */}
      {!loading && !result && !error && (
        <div className="flex flex-col items-center py-16 text-center">
          <div className="mb-3 flex h-12 w-12 items-center justify-center rounded-full bg-muted">
            <Search className="h-5 w-5 text-muted-foreground" />
          </div>
          <p className="text-sm text-muted-foreground">Nhập tên loài để bắt đầu tìm kiếm</p>
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
            ← Trước
          </button>
          <span className="text-sm text-muted-foreground">
            Trang <span className="font-medium text-foreground">{page}</span> / {totalPages}
          </span>
          <button
            onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
            disabled={page === totalPages}
            className="rounded-lg border border-border px-4 py-2 text-sm font-medium
                       hover:bg-muted disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
          >
            Tiếp →
          </button>
        </div>
      )}

    </div>
  );
}

import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import {
  getArticles, useTranslation, useDebounce, cn,
  ArticleType, ReadingLevel,
} from '@fishlover/shared';
import type { ArticleListItemDto } from '@fishlover/shared';
import {
  Search, Loader2, FileText, Eye, Clock, Star, ChevronLeft, ChevronRight, Sparkles, Lock,
} from 'lucide-react';
import {
  ARTICLE_TYPES, READING_LEVELS, TYPE_KEYS, LEVEL_KEYS, TYPE_BADGE, LEVEL_BADGE, formatArticleDate,
} from './labels';

const PAGE_SIZE = 12;

function ArticleCard({ article, onClick, t, locale }: {
  article: ArticleListItemDto;
  onClick: () => void;
  t: ReturnType<typeof useTranslation>['t'];
  locale: string;
}) {
  return (
    <button
      onClick={onClick}
      className="group text-left rounded-2xl overflow-hidden border border-slate-800/60 bg-[#1E293B] hover:border-sky-500/40 transition-colors"
    >
      <div className="relative h-36 bg-gradient-to-br from-slate-800 to-slate-900">
        {article.thumbnailUrl
          ? <img src={article.thumbnailUrl} alt="" loading="lazy" className="h-full w-full object-cover transition-transform duration-300 group-hover:scale-105" />
          : <div className="flex h-full w-full items-center justify-center"><FileText className="h-10 w-10 text-white/15" /></div>
        }
        {article.isFeatured && (
          <span className="absolute left-2 top-2 flex items-center gap-1 rounded-full bg-amber-500/90 px-2 py-0.5 text-[10px] font-bold text-slate-900">
            <Star className="h-3 w-3 fill-slate-900" /> {t('articles.featured')}
          </span>
        )}
        {/* Khách vẫn thấy bài, chỉ báo trước là phải đăng nhập mới đọc được nội dung */}
        {article.requiresAuth && (
          <span className="absolute right-2 top-2 flex items-center gap-1 rounded-full bg-slate-900/80 px-2 py-0.5 text-[10px] font-semibold text-sky-300 backdrop-blur">
            <Lock className="h-3 w-3" /> {t('articles.lockedBadge')}
          </span>
        )}
      </div>

      <div className="p-3">
        <div className="mb-2 flex flex-wrap items-center gap-1.5">
          <span className={cn('rounded-full border px-1.5 py-0.5 text-[10px] font-semibold', TYPE_BADGE[article.type])}>
            {t(TYPE_KEYS[article.type])}
          </span>
          <span className={cn('rounded-full border px-1.5 py-0.5 text-[10px] font-semibold', LEVEL_BADGE[article.readingLevel])}>
            {t(LEVEL_KEYS[article.readingLevel])}
          </span>
        </div>

        <p className="line-clamp-2 text-sm font-bold leading-snug text-white">{article.title}</p>
        {article.summary && (
          <p className="mt-1.5 line-clamp-2 text-xs leading-relaxed text-slate-400">{article.summary}</p>
        )}

        <div className="mt-2.5 flex items-center gap-3 text-xs text-slate-500">
          <span className="flex items-center gap-1">
            <Clock className="h-3.5 w-3.5" />
            {t('articles.readMinutes', { count: article.readingMinutes })}
          </span>
          <span className="flex items-center gap-1">
            <Eye className="h-3.5 w-3.5" />
            {article.viewCount}
          </span>
          <span className="ml-auto">{formatArticleDate(article.publishedAt, locale)}</span>
        </div>
      </div>
    </button>
  );
}

export default function ArticlesListPage() {
  const navigate = useNavigate();
  const { t, i18n } = useTranslation();
  const lang = i18n.language?.slice(0, 2) ?? 'vi';

  const [articles, setArticles] = useState<ArticleListItemDto[]>([]);
  const [totalPages, setTotalPages] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(false);

  const [type, setType] = useState<ArticleType | undefined>();
  const [level, setLevel] = useState<ReadingLevel | undefined>();
  const [query, setQuery] = useState('');
  const debounced = useDebounce(query, 400);
  const [page, setPage] = useState(1);

  useEffect(() => {
    setLoading(true);
    setError(false);
    getArticles({ lang, type, level, q: debounced.trim() || undefined, page, pageSize: PAGE_SIZE })
      .then((res) => {
        setArticles(res.items);
        setTotalPages(res.totalPages);
      })
      .catch(() => setError(true))
      .finally(() => setLoading(false));
  }, [lang, type, level, debounced, page]);

  const resetPage = () => setPage(1);
  const selectCls = 'bg-[#1E293B] border border-slate-700 rounded-lg px-2.5 py-2 text-base sm:text-sm text-white min-h-[44px] sm:min-h-0';

  return (
    <div className="px-4 pb-10 pt-5 sm:px-6">
      <div className="mb-5">
        <h1 className="text-xl font-black text-white sm:text-2xl">{t('articles.title')}</h1>
        <p className="mt-1 text-sm text-slate-500">{t('articles.subtitle')}</p>
      </div>

      {/* Release notes vẫn là trang riêng, ghim ở đây để không mất lối vào */}
      <Link
        to="/articles/release"
        className="mb-5 flex items-center gap-3 rounded-2xl border border-sky-500/20 bg-sky-500/5 p-3.5 transition-colors hover:bg-sky-500/10"
      >
        <Sparkles className="h-5 w-5 shrink-0 text-sky-400" />
        <div className="min-w-0 flex-1">
          <p className="text-sm font-semibold text-white">{t('articles.releaseNotes')}</p>
          <p className="mt-0.5 line-clamp-1 text-xs text-slate-400">{t('articles.releaseNotesDesc')}</p>
        </div>
        <ChevronRight className="h-4 w-4 shrink-0 text-slate-500" />
      </Link>

      {/* Bộ lọc */}
      <div className="mb-6 flex flex-wrap items-center gap-2">
        <div className="relative min-w-[200px] flex-1">
          <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-500" />
          <input
            value={query}
            onChange={(e) => { setQuery(e.target.value); resetPage(); }}
            placeholder={t('articles.searchPlaceholder')}
            className="min-h-[44px] w-full rounded-lg border border-slate-700 bg-[#1E293B] py-2 pl-10 pr-4 text-base text-slate-200 placeholder:text-slate-500 focus:border-sky-500/50 focus:outline-none sm:text-sm"
          />
        </div>

        <select
          value={type ?? ''}
          onChange={(e) => { setType(e.target.value ? Number(e.target.value) as ArticleType : undefined); resetPage(); }}
          className={selectCls}
        >
          <option value="">{t('articles.allTypes')}</option>
          {ARTICLE_TYPES.map((v) => <option key={v} value={v}>{t(TYPE_KEYS[v])}</option>)}
        </select>

        <select
          value={level ?? ''}
          onChange={(e) => { setLevel(e.target.value ? Number(e.target.value) as ReadingLevel : undefined); resetPage(); }}
          className={selectCls}
        >
          <option value="">{t('articles.allLevels')}</option>
          {READING_LEVELS.map((v) => <option key={v} value={v}>{t(LEVEL_KEYS[v])}</option>)}
        </select>
      </div>

      {loading && (
        <div className="flex items-center justify-center py-20 text-slate-600">
          <Loader2 className="h-6 w-6 animate-spin" />
        </div>
      )}

      {!loading && error && (
        <div className="rounded-xl border border-red-500/30 bg-red-500/10 p-4 text-sm text-red-400">
          {t('articles.loadError')}
        </div>
      )}

      {!loading && !error && articles.length === 0 && (
        <div className="flex flex-col items-center justify-center py-20 text-center">
          <FileText className="mb-3 h-12 w-12 text-slate-700" />
          <p className="text-sm text-slate-500">{t('articles.empty')}</p>
        </div>
      )}

      {!loading && !error && articles.length > 0 && (
        <>
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
            {articles.map((a) => (
              <ArticleCard
                key={a.id}
                article={a}
                onClick={() => navigate(`/articles/${a.slug}`)}
                t={t}
                locale={lang}
              />
            ))}
          </div>

          {totalPages > 1 && (
            <div className="mt-8 flex items-center justify-center gap-3">
              <button
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                disabled={page <= 1}
                className="flex min-h-[44px] min-w-[44px] items-center justify-center rounded-lg border border-slate-700 bg-[#1E293B] p-2.5 text-slate-400 transition-colors hover:border-sky-500/40 disabled:opacity-40"
              >
                <ChevronLeft className="h-4 w-4" />
              </button>
              <span className="text-sm text-slate-400">{page} / {totalPages}</span>
              <button
                onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                disabled={page >= totalPages}
                className="flex min-h-[44px] min-w-[44px] items-center justify-center rounded-lg border border-slate-700 bg-[#1E293B] p-2.5 text-slate-400 transition-colors hover:border-sky-500/40 disabled:opacity-40"
              >
                <ChevronRight className="h-4 w-4" />
              </button>
            </div>
          )}
        </>
      )}
    </div>
  );
}

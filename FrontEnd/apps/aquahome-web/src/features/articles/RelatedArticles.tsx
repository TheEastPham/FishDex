import { Link } from 'react-router-dom';
import { Clock, Lock } from 'lucide-react';
import { cn, useTranslation } from '@fishlover/shared';
import type { ArticleListItemDto } from '@fishlover/shared';
import { TYPE_KEYS, TYPE_BADGE } from './labels';

/**
 * Bài liên quan cuối trang đọc. Trước đây bài kết thúc bằng hàng tag rồi hết — đọc xong là ngõ cụt,
 * không có lối đi tiếp nào ngoài nút Back.
 *
 * BE xếp hạng theo số tag trùng rồi tới cùng loại bài, nên thứ tự nhận được đã là thứ tự nên hiện.
 */
interface Props {
  articles: ArticleListItemDto[];
}

export default function RelatedArticles({ articles }: Props) {
  const { t } = useTranslation();
  if (articles.length === 0) return null;

  return (
    <section className="mt-16 border-t border-slate-800 pt-8">
      <h2 className="mb-4 text-sm font-semibold uppercase tracking-wider text-slate-400">
        {t('articles.relatedTitle')}
      </h2>

      <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
        {articles.map((a) => (
          <Link
            key={a.id}
            to={`/articles/${a.slug}`}
            className="group flex gap-3 overflow-hidden rounded-xl border border-slate-800 bg-[#1E293B] p-2.5 transition-colors hover:border-sky-500/40 sm:flex-col sm:p-0"
          >
            <div className="relative h-16 w-24 shrink-0 overflow-hidden rounded-lg bg-slate-800 sm:h-28 sm:w-full sm:rounded-none">
              {a.thumbnailUrl && (
                <img
                  src={a.thumbnailUrl}
                  alt=""
                  loading="lazy"
                  className="h-full w-full object-cover transition-transform duration-300 group-hover:scale-105"
                />
              )}
              {a.requiresAuth && (
                <span className="absolute right-1.5 top-1.5 rounded-full bg-slate-900/80 p-1 text-sky-300 backdrop-blur">
                  <Lock className="h-3 w-3" />
                </span>
              )}
            </div>

            <div className="min-w-0 flex-1 sm:p-3">
              <span className={cn(
                'inline-block rounded-full border px-1.5 py-0.5 text-[10px] font-semibold',
                TYPE_BADGE[a.type],
              )}>
                {t(TYPE_KEYS[a.type])}
              </span>
              <p className="mt-1.5 line-clamp-2 text-sm font-semibold leading-snug text-white">{a.title}</p>
              <span className="mt-1.5 flex items-center gap-1 text-xs text-slate-500">
                <Clock className="h-3 w-3" />
                {t('articles.readMinutes', { count: a.readingMinutes })}
              </span>
            </div>
          </Link>
        ))}
      </div>
    </section>
  );
}

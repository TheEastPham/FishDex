import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import {
  getArticleBySlug, fetchArticleContent, recordArticleView, useTranslation, cn,
} from '@fishlover/shared';
import type { ArticleContent, ArticleDetailDto } from '@fishlover/shared';
import { ArrowLeft, Clock, Eye, Loader2, FileText, Languages, AlertTriangle, Lock, LogIn } from 'lucide-react';
import ArticleContentRenderer from './ArticleContentRenderer';
import { TYPE_KEYS, LEVEL_KEYS, TYPE_BADGE, LEVEL_BADGE, LANGUAGE_KEYS, formatArticleDate } from './labels';

export default function ArticleDetailPage() {
  const { slug = '' } = useParams();
  const { t, i18n } = useTranslation();
  const lang = i18n.language?.slice(0, 2) ?? 'vi';

  const [article, setArticle] = useState<ArticleDetailDto | null>(null);
  const [content, setContent] = useState<ArticleContent | null>(null);
  const [loading, setLoading] = useState(true);
  const [notFound, setNotFound] = useState(false);
  const [contentError, setContentError] = useState(false);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setNotFound(false);
    setContentError(false);
    setContent(null);

    (async () => {
      let detail: ArticleDetailDto;
      try {
        detail = await getArticleBySlug(slug, lang);
      } catch {
        if (!cancelled) { setNotFound(true); setLoading(false); }
        return;
      }
      if (cancelled) return;
      setArticle(detail);

      // Nội dung tải riêng từ R2: hỏng bước này thì phần đầu bài vẫn đọc được,
      // và người dùng biết chính xác là lỗi nội dung chứ không phải bài không tồn tại.
      // Bài bị khóa thì BE cố tình không trả contentUrl — đó không phải lỗi, đừng báo lỗi.
      if (detail.contentUrl) {
        try {
          const body = await fetchArticleContent(detail.contentUrl);
          if (!cancelled) setContent(body);
        } catch {
          if (!cancelled) setContentError(true);
        }
      } else if (!cancelled && !detail.requiresAuth) {
        setContentError(true);
      }

      if (!cancelled) setLoading(false);
    })();

    return () => { cancelled = true; };
  }, [slug, lang]);

  // Đếm view tách hẳn khỏi luồng đọc — hỏng thì thôi, không ảnh hưởng gì tới người dùng.
  useEffect(() => {
    if (!slug) return;
    recordArticleView(slug).catch(() => {});
  }, [slug]);

  if (loading) {
    return (
      <div className="flex items-center justify-center py-24 text-slate-600">
        <Loader2 className="h-6 w-6 animate-spin" />
      </div>
    );
  }

  if (notFound || !article) {
    return (
      <div className="flex flex-col items-center justify-center py-24 text-center">
        <FileText className="mb-3 h-12 w-12 text-slate-700" />
        <p className="mb-4 text-sm text-slate-500">{t('articles.notFound')}</p>
        <Link to="/articles" className="text-sm text-sky-400 hover:text-sky-300">
          {t('articles.backToList')}
        </Link>
      </div>
    );
  }

  // BE đã lùi sang bản khác khi ngôn ngữ đang xem chưa được dịch — nói rõ cho người đọc biết.
  const isFallback = article.language !== article.requestedLanguage;

  return (
    <div className="px-4 pb-14 pt-5 sm:px-6">
      <div className="mx-auto max-w-3xl">
        <Link
          to="/articles"
          className="inline-flex items-center gap-1.5 text-sm text-slate-400 transition-colors hover:text-white"
        >
          <ArrowLeft className="h-4 w-4" />
          {t('articles.backToList')}
        </Link>

        <header className="mt-5">
          <div className="mb-3 flex flex-wrap items-center gap-1.5">
            <span className={cn('rounded-full border px-2 py-0.5 text-[11px] font-semibold', TYPE_BADGE[article.type])}>
              {t(TYPE_KEYS[article.type])}
            </span>
            <span className={cn('rounded-full border px-2 py-0.5 text-[11px] font-semibold', LEVEL_BADGE[article.readingLevel])}>
              {t(LEVEL_KEYS[article.readingLevel])}
            </span>
          </div>

          <h1 className="text-2xl font-black leading-snug text-white sm:text-3xl">{article.title}</h1>

          {article.summary && (
            <p className="mt-3 text-[15px] leading-relaxed text-slate-400">{article.summary}</p>
          )}

          <div className="mt-4 flex flex-wrap items-center gap-x-4 gap-y-1.5 text-xs text-slate-500">
            {article.authorName && <span>{article.authorName}</span>}
            <span>{formatArticleDate(article.publishedAt, lang)}</span>
            <span className="flex items-center gap-1">
              <Clock className="h-3.5 w-3.5" />
              {t('articles.readMinutes', { count: article.readingMinutes })}
            </span>
            <span className="flex items-center gap-1">
              <Eye className="h-3.5 w-3.5" />
              {article.viewCount}
            </span>
          </div>

          {isFallback && (
            <div className="mt-4 flex items-start gap-2 rounded-xl border border-slate-700/60 bg-slate-800/40 p-3 text-xs text-slate-400">
              <Languages className="mt-0.5 h-3.5 w-3.5 shrink-0 text-slate-500" />
              <span>{t('articles.fallbackNotice', { shown: t(LANGUAGE_KEYS[article.language] ?? article.language) })}</span>
            </div>
          )}
        </header>

        {article.thumbnailUrl && (
          <img
            src={article.thumbnailUrl}
            alt=""
            className="mt-6 max-h-[45vh] w-full rounded-2xl border border-slate-800 object-cover"
          />
        )}

        <div className="mt-8">
          {content && <ArticleContentRenderer content={content} assets={article.assets} />}

          {article.requiresAuth && (
            <div className="rounded-2xl border border-sky-500/20 bg-sky-500/5 p-6 text-center">
              <Lock className="mx-auto mb-3 h-8 w-8 text-sky-400" />
              <p className="text-base font-semibold text-white">{t('articles.memberOnly')}</p>
              <p className="mx-auto mt-2 max-w-md text-sm leading-relaxed text-slate-400">
                {t('articles.memberOnlyDesc', { level: t(LEVEL_KEYS[article.readingLevel]) })}
              </p>
              <Link
                to="/login"
                className="mt-5 inline-flex min-h-[44px] items-center gap-2 rounded-xl bg-sky-500 px-5 py-2.5 text-sm font-semibold text-white transition-colors hover:bg-sky-400"
              >
                <LogIn className="h-4 w-4" />
                {t('articles.signInToRead')}
              </Link>
            </div>
          )}

          {contentError && (
            <div className="flex items-start gap-2.5 rounded-xl border border-amber-500/30 bg-amber-500/5 p-4 text-sm text-amber-300/90">
              <AlertTriangle className="mt-0.5 h-4 w-4 shrink-0" />
              <span>{t('articles.contentError')}</span>
            </div>
          )}
        </div>

        {article.tags.length > 0 && (
          <div className="mt-10 flex flex-wrap gap-1.5 border-t border-slate-800 pt-6">
            {article.tags.map((tag) => (
              <span key={tag} className="rounded-full border border-slate-700/60 bg-slate-800/60 px-2.5 py-1 text-[11px] text-slate-400">
                #{tag}
              </span>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

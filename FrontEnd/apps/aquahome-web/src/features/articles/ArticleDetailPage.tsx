import { useCallback, useEffect, useMemo, useRef, useState, type CSSProperties } from 'react';
import { Link, useParams } from 'react-router-dom';
import {
  getArticleBySlug, fetchArticleContent, recordArticleView, getRelatedArticles, useTranslation, cn,
} from '@fishlover/shared';
import type { ArticleContent, ArticleDetailDto, ArticleListItemDto } from '@fishlover/shared';
import {
  ArrowLeft, Clock, Eye, Loader2, FileText, Languages, AlertTriangle, Lock, LogIn, List,
} from 'lucide-react';
import ArticleContentRenderer, { collectImages } from './ArticleContentRenderer';
import ArticleCarousel from './ArticleCarousel';
import RelatedArticles from './RelatedArticles';
import ImageLightbox from './ImageLightbox';
import { TYPE_KEYS, LEVEL_KEYS, TYPE_BADGE, LEVEL_BADGE, LANGUAGE_KEYS, formatArticleDate } from './labels';
import { templateSpec, headingAnchor, PROSE_WIDTH, FONT_SCALES } from './templates';

const FONT_SCALE_KEY = 'fishlover_article_font';

/**
 * AppShell cho nội dung cuộn bên trong `<main class="overflow-auto">`, KHÔNG phải cuộn cửa sổ —
 * `window.scrollY` luôn bằng 0 và listener gắn vào window không bao giờ chạy. Vậy nên phải leo
 * ngược cây DOM tìm đúng phần tử đang cuộn.
 */
function findScroller(from: HTMLElement | null): HTMLElement | null {
  let node = from?.parentElement ?? null;
  while (node) {
    // Chỉ xét overflow, KHÔNG kiểm tra scrollHeight > clientHeight: lúc effect chạy thì ảnh
    // chưa tải xong nên trang còn ngắn, kiểm tra chiều cao ở đây sẽ bỏ sót đúng phần tử cuộn
    // và listener rơi nhầm về window (nơi scrollY luôn bằng 0).
    const overflowY = getComputedStyle(node).overflowY;
    if (overflowY === 'auto' || overflowY === 'scroll') return node;
    node = node.parentElement;
  }
  return null;
}

export default function ArticleDetailPage() {
  const { slug = '' } = useParams();
  const { t, i18n } = useTranslation();
  const lang = i18n.language?.slice(0, 2) ?? 'vi';

  const [article, setArticle] = useState<ArticleDetailDto | null>(null);
  const [content, setContent] = useState<ArticleContent | null>(null);
  const [loading, setLoading] = useState(true);
  const [notFound, setNotFound] = useState(false);
  const [contentError, setContentError] = useState(false);

  const [progress, setProgress] = useState(0);
  const [activeAnchor, setActiveAnchor] = useState<string | null>(null);
  const [lightboxAt, setLightboxAt] = useState<number | null>(null);
  const [autoPlay, setAutoPlay] = useState(false);
  const [fontScale, setFontScale] = useState(1);
  const [related, setRelated] = useState<ArticleListItemDto[]>([]);
  const heroRef = useRef<HTMLImageElement>(null);
  const rootRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    try {
      const saved = Number(localStorage.getItem(FONT_SCALE_KEY));
      if (FONT_SCALES.includes(saved as typeof FONT_SCALES[number])) setFontScale(saved);
    } catch { /* localStorage bị chặn — dùng cỡ mặc định */ }
  }, []);

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

      // Nội dung tải riêng từ R2: hỏng bước này thì phần đầu bài vẫn đọc được, và người dùng biết
      // chính xác là lỗi nội dung chứ không phải bài không tồn tại.
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

  // Bài liên quan là phần phụ: tách hẳn khỏi luồng tải bài, hỏng thì chỉ thiếu khối cuối trang.
  useEffect(() => {
    if (!slug) return;
    let cancelled = false;
    setRelated([]);
    getRelatedArticles(slug, lang, 3)
      .then((r) => { if (!cancelled) setRelated(r); })
      .catch(() => {});
    return () => { cancelled = true; };
  }, [slug, lang]);

  // Đếm view tách hẳn khỏi luồng đọc — hỏng thì thôi, không ảnh hưởng gì tới người dùng.
  useEffect(() => {
    if (!slug) return;
    recordArticleView(slug).catch(() => {});
  }, [slug]);

  const spec = templateSpec(article?.templateKey);
  const images = useMemo(
    () => (content && article ? collectImages(content, article.assets) : []),
    [content, article],
  );

  /** Mục lục dựng từ H2 của thân bài — dưới 2 mục thì không đáng có mục lục. */
  const toc = useMemo(() => {
    if (!spec.toc || !content?.body) return [];
    return content.body
      .map((b, i) => ({ block: b, index: i }))
      .filter(({ block }) => block.type === 'heading' && block.level !== 3)
      .map(({ block, index }, n) => ({ text: block.text ?? '', anchor: headingAnchor(index), number: n + 1 }));
  }, [content, spec.toc]);

  // Thanh tiến độ + parallax ảnh bìa: một listener chung, cập nhật trong rAF để không giật khi cuộn.
  useEffect(() => {
    if (!spec.progress && !spec.parallax) return;

    const scroller = findScroller(rootRef.current);
    const target: HTMLElement | Window = scroller ?? window;
    const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    let frame = 0;

    const readScroll = () => {
      if (scroller) {
        return { top: scroller.scrollTop, max: scroller.scrollHeight - scroller.clientHeight };
      }
      return { top: window.scrollY, max: document.documentElement.scrollHeight - window.innerHeight };
    };

    const onScroll = () => {
      if (frame) return;
      frame = requestAnimationFrame(() => {
        frame = 0;
        const { top, max } = readScroll();

        if (spec.progress) setProgress(max > 0 ? Math.min(1, top / max) : 0);

        // Ảnh bìa trôi chậm hơn trang → cảm giác chiều sâu. Tôn trọng prefers-reduced-motion.
        if (spec.parallax && !reduced && heroRef.current) {
          heroRef.current.style.transform = `translate3d(0, ${top * 0.3}px, 0) scale(1.08)`;
        }
      });
    };

    target.addEventListener('scroll', onScroll, { passive: true });
    onScroll();
    return () => {
      target.removeEventListener('scroll', onScroll);
      if (frame) cancelAnimationFrame(frame);
    };
  }, [spec.progress, spec.parallax, content]);

  // Mục lục sáng theo mục đang đọc
  useEffect(() => {
    if (toc.length < 2) return;
    const headings = toc
      .map((item) => document.getElementById(item.anchor))
      .filter((el): el is HTMLElement => el !== null);
    if (headings.length === 0) return;

    const observer = new IntersectionObserver(
      (entries) => {
        const visible = entries.filter((e) => e.isIntersecting);
        if (visible.length > 0) setActiveAnchor(visible[0].target.id);
      },
      // Vùng nhận diện là dải ngang gần đỉnh màn: mục nào chạm dải đó là mục đang đọc
      { rootMargin: '-80px 0px -70% 0px', threshold: 0 },
    );

    headings.forEach((h) => observer.observe(h));
    return () => observer.disconnect();
  }, [toc]);

  const changeFont = useCallback((delta: number) => {
    setFontScale((current) => {
      const i = FONT_SCALES.indexOf(current as typeof FONT_SCALES[number]);
      const next = FONT_SCALES[Math.min(FONT_SCALES.length - 1, Math.max(0, (i < 0 ? 1 : i) + delta))];
      try { localStorage.setItem(FONT_SCALE_KEY, String(next)); } catch { /* bỏ qua */ }
      return next;
    });
  }, []);

  const openImage = (i: number) => { setAutoPlay(false); setLightboxAt(i); };

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
  const galleryMode = spec.gallery && images.length > 0;
  const overlayHero = !galleryMode && spec.hero === 'overlay' && Boolean(article.thumbnailUrl);

  const badges = (
    <div className="flex flex-wrap items-center gap-1.5">
      <span className={cn('rounded-full border px-2 py-0.5 text-[11px] font-semibold', TYPE_BADGE[article.type])}>
        {t(TYPE_KEYS[article.type])}
      </span>
      <span className={cn('rounded-full border px-2 py-0.5 text-[11px] font-semibold', LEVEL_BADGE[article.readingLevel])}>
        {t(LEVEL_KEYS[article.readingLevel])}
      </span>
    </div>
  );

  const meta = (
    <div className="flex flex-wrap items-center gap-x-4 gap-y-1.5 text-[13px] text-slate-500">
      {article.authorName && <span className="text-slate-400">{article.authorName}</span>}
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
  );

  return (
    <div ref={rootRef} className="pb-20" style={{ '--prose-scale': fontScale } as CSSProperties}>
      {/* Thanh tiến độ đọc — biết còn bao nhiêu bài mà không cần nhìn thanh cuộn */}
      {spec.progress && (
        <div className="fixed inset-x-0 top-0 z-30 h-0.5 bg-transparent">
          <div
            className="h-full bg-sky-500 transition-[width] duration-150"
            style={{ width: `${progress * 100}%` }}
          />
        </div>
      )}

      {/* ── Hero ─────────────────────────────────────────────── */}
      {overlayHero ? (
        <header className="relative">
          <div className="relative h-[52vh] min-h-[300px] w-full overflow-hidden sm:h-[58vh]">
            <img
              ref={heroRef}
              src={article.thumbnailUrl!}
              alt=""
              className={cn('h-full w-full object-cover', spec.parallax && 'will-change-transform')}
            />
            {/* Ảnh phải tối dần về đáy, không thì chữ trắng chìm vào vùng sáng */}
            <div className="absolute inset-0 bg-gradient-to-t from-[#0F172A] via-[#0F172A]/70 to-[#0F172A]/10" />
          </div>

          <div className="absolute inset-x-0 bottom-0 px-4 pb-8 sm:px-6">
            <div className="mx-auto max-w-4xl">
              <Link
                to="/articles"
                className="mb-4 inline-flex items-center gap-1.5 text-sm text-slate-300 transition-colors hover:text-white"
              >
                <ArrowLeft className="h-4 w-4" />
                {t('articles.backToList')}
              </Link>
              {badges}
              <h1 className="mt-3 max-w-[26ch] text-[28px] font-black leading-[1.15] text-white drop-shadow sm:text-4xl">
                {article.title}
              </h1>
              <div className="mt-4">{meta}</div>
            </div>
          </div>
        </header>
      ) : (
        <header className="px-4 pt-6 sm:px-6">
          <div className={PROSE_WIDTH}>
            <Link
              to="/articles"
              className="inline-flex items-center gap-1.5 text-sm text-slate-400 transition-colors hover:text-white"
            >
              <ArrowLeft className="h-4 w-4" />
              {t('articles.backToList')}
            </Link>

            <div className="mt-6">{badges}</div>
            <h1 className="mt-3 text-[28px] font-black leading-[1.15] text-white sm:text-[38px]">
              {article.title}
            </h1>
            {article.summary && (
              <p className="mt-4 text-lg font-light leading-[1.6] text-slate-300 sm:text-xl">{article.summary}</p>
            )}

            <div className="mt-5 flex flex-wrap items-center gap-3 border-t border-slate-800 pt-4">
              {meta}

              {/* Chỉnh cỡ chữ: nhớ theo người đọc, áp cho mọi bài sau */}
              {spec.readerControls && (
                <div className="ml-auto flex items-center overflow-hidden rounded-lg border border-slate-700">
                  <button
                    onClick={() => changeFont(-1)}
                    disabled={fontScale === FONT_SCALES[0]}
                    className="flex h-9 w-9 items-center justify-center text-xs text-slate-400 hover:bg-white/5 hover:text-white disabled:opacity-30"
                    aria-label={t('articles.fontSmaller')}
                  >
                    A−
                  </button>
                  <span className="h-5 w-px bg-slate-700" />
                  <button
                    onClick={() => changeFont(1)}
                    disabled={fontScale === FONT_SCALES[FONT_SCALES.length - 1]}
                    className="flex h-9 w-9 items-center justify-center text-sm font-semibold text-slate-400 hover:bg-white/5 hover:text-white disabled:opacity-30"
                    aria-label={t('articles.fontBigger')}
                  >
                    A+
                  </button>
                </div>
              )}
            </div>
          </div>

          {spec.hero === 'inline' && article.thumbnailUrl && (
            <img
              src={article.thumbnailUrl}
              alt=""
              className="mx-auto mt-8 max-h-[52vh] w-full max-w-4xl rounded-2xl border border-slate-800 object-cover"
            />
          )}
        </header>
      )}

      {/* Dải ảnh + nút slideshow — chỉ kiểu "Ảnh là chính" */}

      {/* ── Thân bài ─────────────────────────────────────────── */}
      <div className="px-4 sm:px-6">
        <div className={cn(
          'mx-auto mt-10 max-w-4xl',
          // Mục lục chỉ xuất hiện từ lg trở lên: dưới đó không đủ chỗ, và trên mobile người đọc
          // cuộn nhanh hơn là bấm mục lục.
          toc.length >= 2 && 'lg:grid lg:max-w-6xl lg:grid-cols-[minmax(0,1fr)_200px] lg:gap-12',
        )}>
          <div className="min-w-0">
            {overlayHero && article.summary && (
              <p className={cn(PROSE_WIDTH, 'mb-8 text-lg font-light leading-[1.6] text-slate-300 sm:text-xl')}>
                {article.summary}
              </p>
            )}

            {isFallback && (
              <div className={cn(PROSE_WIDTH, 'mb-8 flex items-start gap-2 rounded-xl border border-slate-700/60 bg-slate-800/40 p-3 text-xs text-slate-400')}>
                <Languages className="mt-0.5 h-3.5 w-3.5 shrink-0 text-slate-500" />
                <span>{t('articles.fallbackNotice', { shown: t(LANGUAGE_KEYS[article.language] ?? article.language) })}</span>
              </div>
            )}

            {content && (
              <ArticleContentRenderer
                content={content}
                assets={article.assets}
                templateKey={article.templateKey}
                onImageClick={openImage}
                storageKey={`fishlover_article_check:${article.slug}`}
                gallery={galleryMode ? <ArticleCarousel images={images} onExpand={openImage} /> : undefined}
              />
            )}

            {article.requiresAuth && (
              <div className={cn(PROSE_WIDTH, 'rounded-2xl border border-sky-500/20 bg-sky-500/5 p-6 text-center sm:p-8')}>
                <Lock className="mx-auto mb-3 h-8 w-8 text-sky-400" />
                <p className="text-lg font-semibold text-white">{t('articles.memberOnly')}</p>
                <p className="mx-auto mt-2 max-w-md text-sm leading-relaxed text-slate-400">
                  {t('articles.memberOnlyDesc', { level: t(LEVEL_KEYS[article.readingLevel]) })}
                </p>
                <Link
                  to="/login"
                  className="mt-6 inline-flex min-h-[44px] items-center gap-2 rounded-xl bg-sky-500 px-5 py-2.5 text-sm font-semibold text-white transition-colors hover:bg-sky-400"
                >
                  <LogIn className="h-4 w-4" />
                  {t('articles.signInToRead')}
                </Link>
              </div>
            )}

            {contentError && (
              <div className={cn(PROSE_WIDTH, 'flex items-start gap-2.5 rounded-xl border border-amber-500/30 bg-amber-500/5 p-4 text-sm text-amber-300/90')}>
                <AlertTriangle className="mt-0.5 h-4 w-4 shrink-0" />
                <span>{t('articles.contentError')}</span>
              </div>
            )}

            {/* đặt ngay sau nội dung để đọc xong là có chỗ đi tiếp */}
            {article.tags.length > 0 && (
              <div className={cn(PROSE_WIDTH, 'mt-16 flex flex-wrap gap-1.5 border-t border-slate-800 pt-6')}>
                {article.tags.map((tag) => (
                  <span key={tag} className="rounded-full border border-slate-700/60 bg-slate-800/60 px-2.5 py-1 text-[11px] text-slate-400">
                    #{tag}
                  </span>
                ))}
              </div>
            )}

            <RelatedArticles articles={related} />
          </div>

          {toc.length >= 2 && (
            <aside className="hidden lg:block">
              <nav className="sticky top-6">
                <p className="mb-3 flex items-center gap-1.5 text-[11px] font-semibold uppercase tracking-wider text-slate-500">
                  <List className="h-3.5 w-3.5" /> {t('articles.tocTitle')}
                </p>
                <ol className="space-y-2 border-l border-slate-800">
                  {toc.map((item) => {
                    const active = activeAnchor === item.anchor;
                    return (
                      <li key={item.anchor}>
                        <a
                          href={`#${item.anchor}`}
                          className={cn(
                            '-ml-px block border-l pl-3 text-[13px] leading-snug transition-colors',
                            active
                              ? 'border-sky-500 font-semibold text-white'
                              : 'border-transparent text-slate-400 hover:border-slate-600 hover:text-slate-200',
                          )}
                        >
                          <span className={cn('mr-1.5', active ? 'text-sky-400' : 'text-slate-600')}>
                            {item.number}.
                          </span>
                          {item.text}
                        </a>
                      </li>
                    );
                  })}
                </ol>
              </nav>
            </aside>
          )}
        </div>
      </div>

      <ImageLightbox
        images={images}
        startIndex={lightboxAt}
        onClose={() => setLightboxAt(null)}
        autoPlayOnOpen={autoPlay}
      />
    </div>
  );
}

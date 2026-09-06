import { useCallback, useEffect, useRef, useState } from 'react';
import {
  getAdminArticle, updateArticle, upsertArticleTranslation, deleteArticleTranslation,
  uploadArticleAsset, deleteArticleAsset, setArticleCover,
  publishArticle, unpublishArticle, archiveArticle, deleteArticle,
  fetchArticleContent, useTranslation, cn,
  ArticleStatus, ArticleType, ReadingLevel,
} from '@fishlover/shared';
import type { AdminArticleDto, ArticleBlock } from '@fishlover/shared';
import {
  ArrowLeft, Loader2, Save, Upload, Trash2, Globe, ImagePlus, AlertTriangle, Eye, EyeOff, Archive,
} from 'lucide-react';
import BlockListEditor from './BlockListEditor';
import { ARTICLE_TYPES, READING_LEVELS, TYPE_KEYS, LEVEL_KEYS } from '../../articles/labels';

const LANGUAGES = ['vi', 'en', 'de', 'zh'];

interface Props {
  articleId: string;
  onBack: () => void;
  /** Gọi sau mỗi thay đổi để danh sách ngoài kia không hiển thị dữ liệu cũ. */
  onChanged: () => void;
}

const inputCls =
  'w-full rounded-lg border border-slate-700 bg-[#141518] px-3 py-2 text-base text-slate-200 placeholder:text-slate-600 focus:border-sky-500/50 focus:outline-none sm:text-sm';

/** Bỏ block rỗng trước khi gửi — admin hay để lại một ô trống lúc soạn, không nên vì thế mà 422. */
function prune(blocks: ArticleBlock[]): ArticleBlock[] {
  return blocks
    .map((b) => (b.type === 'list' ? { ...b, items: (b.items ?? []).map((i) => i.trim()).filter(Boolean) } : b))
    .filter((b) => {
      if (b.type === 'image') return Boolean(b.assetId);
      if (b.type === 'list') return (b.items ?? []).length > 0;
      return Boolean(b.text?.trim());
    });
}

function errorsFrom(err: unknown): string[] {
  const data = (err as { response?: { data?: { errors?: string[]; message?: string } } })?.response?.data;
  if (data?.errors?.length) return data.errors;
  if (data?.message) return [data.message];
  return [];
}

export default function ArticleEditor({ articleId, onBack, onChanged }: Props) {
  const { t } = useTranslation();

  const [article, setArticle] = useState<AdminArticleDto | null>(null);
  const [language, setLanguage] = useState('vi');
  const [loading, setLoading] = useState(true);
  const [loadingContent, setLoadingContent] = useState(false);
  const [saving, setSaving] = useState(false);
  const [errors, setErrors] = useState<string[]>([]);
  const [saved, setSaved] = useState(false);

  const [title, setTitle] = useState('');
  const [summary, setSummary] = useState('');
  const [intro, setIntro] = useState<ArticleBlock[]>([]);
  const [body, setBody] = useState<ArticleBlock[]>([]);
  const [conclusion, setConclusion] = useState<ArticleBlock[]>([]);

  const [type, setType] = useState<ArticleType>(ArticleType.Setup);
  const [level, setLevel] = useState<ReadingLevel>(ReadingLevel.Beginner);
  const [tags, setTags] = useState('');
  const [slug, setSlug] = useState('');
  const [featured, setFeatured] = useState(false);

  const coverInput = useRef<HTMLInputElement>(null);
  const assetInput = useRef<HTMLInputElement>(null);

  const applyArticle = useCallback((data: AdminArticleDto) => {
    setArticle(data);
    setType(data.type);
    setLevel(data.readingLevel);
    setTags(data.tags.join(', '));
    setSlug(data.slug);
    setFeatured(data.isFeatured);
  }, []);

  useEffect(() => {
    setLoading(true);
    getAdminArticle(articleId)
      .then((data) => {
        applyArticle(data);
        setLanguage(data.translations[0]?.language ?? 'vi');
      })
      .catch(() => setErrors([t('adminArticles.loadError')]))
      .finally(() => setLoading(false));
  }, [articleId, applyArticle, t]);

  // Đổi tab ngôn ngữ → nạp nội dung của bản dịch đó từ R2
  useEffect(() => {
    if (!article) return;
    const tr = article.translations.find((x) => x.language === language);

    setTitle(tr?.title ?? '');
    setSummary(tr?.summary ?? '');

    if (!tr?.contentUrl) {
      setIntro([]); setBody([]); setConclusion([]);
      return;
    }

    let cancelled = false;
    setLoadingContent(true);
    fetchArticleContent(tr.contentUrl)
      .then((content) => {
        if (cancelled) return;
        setIntro(content.intro ?? []);
        setBody(content.body ?? []);
        setConclusion(content.conclusion ?? []);
      })
      .catch(() => { if (!cancelled) setErrors([t('adminArticles.contentLoadError')]); })
      .finally(() => { if (!cancelled) setLoadingContent(false); });

    return () => { cancelled = true; };
  }, [article, language, t]);

  const flash = () => { setSaved(true); setTimeout(() => setSaved(false), 2500); };

  const saveContent = async () => {
    setSaving(true);
    setErrors([]);
    try {
      const data = await upsertArticleTranslation(articleId, language, {
        title: title.trim(),
        summary: summary.trim() || undefined,
        content: { intro: prune(intro), body: prune(body), conclusion: prune(conclusion) },
      });
      applyArticle(data);
      onChanged();
      flash();
    } catch (err) {
      setErrors(errorsFrom(err).length ? errorsFrom(err) : [t('adminArticles.saveError')]);
    } finally {
      setSaving(false);
    }
  };

  const saveMeta = async () => {
    setSaving(true);
    setErrors([]);
    try {
      const data = await updateArticle(articleId, {
        type,
        readingLevel: level,
        tags: tags.split(',').map((x) => x.trim()).filter(Boolean),
        slug: slug.trim() || undefined,
        isFeatured: featured,
      });
      applyArticle(data);
      onChanged();
      flash();
    } catch (err) {
      setErrors(errorsFrom(err).length ? errorsFrom(err) : [t('adminArticles.saveError')]);
    } finally {
      setSaving(false);
    }
  };

  const changeStatus = async (next: ArticleStatus) => {
    setErrors([]);
    try {
      const fn = next === ArticleStatus.Published ? publishArticle
        : next === ArticleStatus.Archived ? archiveArticle
        : unpublishArticle;
      applyArticle(await fn(articleId));
      onChanged();
    } catch (err) {
      setErrors(errorsFrom(err).length ? errorsFrom(err) : [t('adminArticles.saveError')]);
    }
  };

  // Upload ảnh tách riêng try/catch: hỏng ảnh thì phần nội dung đang soạn vẫn nguyên vẹn.
  const uploadCover = async (file: File) => {
    setErrors([]);
    try {
      applyArticle(await setArticleCover(articleId, file));
      onChanged();
    } catch (err) {
      setErrors(errorsFrom(err).length ? errorsFrom(err) : [t('adminArticles.uploadError')]);
    }
  };

  const uploadAsset = async (file: File) => {
    setErrors([]);
    try {
      await uploadArticleAsset(articleId, file);
      applyArticle(await getAdminArticle(articleId));
    } catch (err) {
      setErrors(errorsFrom(err).length ? errorsFrom(err) : [t('adminArticles.uploadError')]);
    }
  };

  const removeAsset = async (assetId: string) => {
    if (!confirm(t('adminArticles.confirmDeleteAsset'))) return;
    try {
      await deleteArticleAsset(articleId, assetId);
      applyArticle(await getAdminArticle(articleId));
    } catch {
      setErrors([t('adminArticles.saveError')]);
    }
  };

  const removeTranslation = async () => {
    if (!confirm(t('adminArticles.confirmDeleteTranslation'))) return;
    setErrors([]);
    try {
      const data = await deleteArticleTranslation(articleId, language);
      applyArticle(data);
      setLanguage(data.translations[0]?.language ?? 'vi');
      onChanged();
    } catch (err) {
      setErrors(errorsFrom(err).length ? errorsFrom(err) : [t('adminArticles.saveError')]);
    }
  };

  const removeArticle = async () => {
    if (!confirm(t('adminArticles.confirmDeleteArticle'))) return;
    try {
      await deleteArticle(articleId);
      onChanged();
      onBack();
    } catch {
      setErrors([t('adminArticles.saveError')]);
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center py-24 text-slate-600">
        <Loader2 className="h-6 w-6 animate-spin" />
      </div>
    );
  }

  if (!article) {
    return (
      <div className="py-16 text-center text-sm text-slate-500">
        {t('adminArticles.loadError')}
        <button onClick={onBack} className="ml-2 text-sky-400">{t('common.back')}</button>
      </div>
    );
  }

  const isPublished = article.status === ArticleStatus.Published;
  const missingLanguages = LANGUAGES.filter((l) => !article.translations.some((tr) => tr.language === l));

  return (
    <div className="space-y-5 pb-16">
      {/* Thanh trên cùng */}
      <div className="flex flex-wrap items-center gap-3">
        <button onClick={onBack} className="flex items-center gap-1.5 text-sm text-slate-400 hover:text-white">
          <ArrowLeft className="h-4 w-4" /> {t('adminArticles.backToList')}
        </button>

        <span className={cn(
          'rounded-full border px-2 py-0.5 text-[11px] font-semibold',
          isPublished ? 'border-emerald-500/20 bg-emerald-500/10 text-emerald-400'
            : article.status === ArticleStatus.Archived ? 'border-slate-500/20 bg-slate-500/10 text-slate-400'
            : 'border-amber-500/20 bg-amber-500/10 text-amber-400',
        )}>
          {t(`adminArticles.status${ArticleStatus[article.status]}`)}
        </span>

        <div className="ml-auto flex flex-wrap items-center gap-2">
          {saved && <span className="text-xs text-emerald-400">{t('adminArticles.saved')}</span>}
          <button
            onClick={() => changeStatus(isPublished ? ArticleStatus.Draft : ArticleStatus.Published)}
            className="flex items-center gap-1.5 rounded-lg border border-slate-700 bg-[#1A1B1F] px-3 py-2 text-xs font-semibold text-slate-200 hover:border-sky-500/40"
          >
            {isPublished ? <EyeOff className="h-3.5 w-3.5" /> : <Eye className="h-3.5 w-3.5" />}
            {isPublished ? t('adminArticles.unpublish') : t('adminArticles.publish')}
          </button>
          <button
            onClick={() => changeStatus(ArticleStatus.Archived)}
            className="flex items-center gap-1.5 rounded-lg border border-slate-700 bg-[#1A1B1F] px-3 py-2 text-xs text-slate-400 hover:border-slate-500"
          >
            <Archive className="h-3.5 w-3.5" /> {t('adminArticles.archive')}
          </button>
          <button
            onClick={removeArticle}
            className="flex items-center gap-1.5 rounded-lg border border-red-500/30 bg-red-500/10 px-3 py-2 text-xs font-semibold text-red-400 hover:bg-red-500/20"
          >
            <Trash2 className="h-3.5 w-3.5" /> {t('adminArticles.deleteArticle')}
          </button>
        </div>
      </div>

      {errors.length > 0 && (
        <div className="rounded-xl border border-red-500/30 bg-red-500/10 p-3">
          <div className="mb-1 flex items-center gap-2 text-sm font-semibold text-red-400">
            <AlertTriangle className="h-4 w-4" /> {t('adminArticles.errorTitle')}
          </div>
          <ul className="space-y-0.5 pl-6 text-xs text-red-300/90">
            {errors.map((e, i) => <li key={i} className="list-disc">{e}</li>)}
          </ul>
        </div>
      )}

      {/* Metadata */}
      <section className="rounded-xl border border-slate-800 bg-[#202226] p-4">
        <h3 className="mb-3 text-sm font-semibold text-white">{t('adminArticles.metaTitle')}</h3>

        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <label className="text-xs text-slate-400">
            {t('adminArticles.type')}
            <select value={type} onChange={(e) => setType(Number(e.target.value) as ArticleType)} className={`${inputCls} mt-1`}>
              {ARTICLE_TYPES.map((v) => <option key={v} value={v}>{t(TYPE_KEYS[v])}</option>)}
            </select>
          </label>

          <label className="text-xs text-slate-400">
            {t('adminArticles.level')}
            <select value={level} onChange={(e) => setLevel(Number(e.target.value) as ReadingLevel)} className={`${inputCls} mt-1`}>
              {READING_LEVELS.map((v) => <option key={v} value={v}>{t(LEVEL_KEYS[v])}</option>)}
            </select>
          </label>

          <label className="text-xs text-slate-400">
            {t('adminArticles.slug')}
            <input value={slug} onChange={(e) => setSlug(e.target.value)} className={`${inputCls} mt-1`} />
          </label>

          <label className="text-xs text-slate-400">
            {t('adminArticles.tags')}
            <input value={tags} onChange={(e) => setTags(e.target.value)} placeholder="medaka, thuy sinh" className={`${inputCls} mt-1`} />
          </label>
        </div>

        <div className="mt-3 flex flex-wrap items-center gap-4">
          <label className="flex items-center gap-2 text-xs text-slate-400">
            <input type="checkbox" checked={featured} onChange={(e) => setFeatured(e.target.checked)} className="h-4 w-4 accent-sky-500" />
            {t('adminArticles.featured')}
          </label>

          <button
            onClick={() => coverInput.current?.click()}
            className="flex items-center gap-1.5 rounded-lg border border-slate-700 bg-[#1A1B1F] px-3 py-2 text-xs text-slate-300 hover:border-sky-500/40"
          >
            <Upload className="h-3.5 w-3.5" /> {t('adminArticles.uploadCover')}
          </button>
          <input
            ref={coverInput} type="file" accept="image/*" className="hidden"
            onChange={(e) => { const f = e.target.files?.[0]; if (f) void uploadCover(f); e.target.value = ''; }}
          />
          {article.thumbnailUrl && (
            <img src={article.thumbnailUrl} alt="" className="h-10 w-16 rounded border border-slate-700 object-cover" />
          )}

          <button
            onClick={saveMeta}
            disabled={saving}
            className="ml-auto flex items-center gap-1.5 rounded-lg bg-sky-500 px-3 py-2 text-xs font-semibold text-white hover:bg-sky-400 disabled:opacity-50"
          >
            <Save className="h-3.5 w-3.5" /> {t('adminArticles.saveMeta')}
          </button>
        </div>
      </section>

      {/* Ảnh dùng trong bài */}
      <section className="rounded-xl border border-slate-800 bg-[#202226] p-4">
        <div className="mb-3 flex items-center gap-2">
          <h3 className="text-sm font-semibold text-white">{t('adminArticles.assetsTitle')}</h3>
          <button
            onClick={() => assetInput.current?.click()}
            className="ml-auto flex items-center gap-1.5 rounded-lg border border-slate-700 bg-[#1A1B1F] px-2.5 py-1.5 text-xs text-slate-300 hover:border-sky-500/40"
          >
            <ImagePlus className="h-3.5 w-3.5" /> {t('adminArticles.uploadAsset')}
          </button>
          <input
            ref={assetInput} type="file" accept="image/*" className="hidden"
            onChange={(e) => { const f = e.target.files?.[0]; if (f) void uploadAsset(f); e.target.value = ''; }}
          />
        </div>

        {article.assets.length === 0 ? (
          <p className="text-xs text-slate-500">{t('adminArticles.noAssets')}</p>
        ) : (
          <div className="flex flex-wrap gap-2">
            {article.assets.map((a) => (
              <div key={a.id} className="relative">
                {a.url && <img src={a.url} alt="" className="h-16 w-16 rounded-lg border border-slate-700 object-cover" />}
                <button
                  onClick={() => removeAsset(a.id)}
                  className="absolute -right-1.5 -top-1.5 rounded-full bg-red-500 p-1 text-white hover:bg-red-400"
                >
                  <Trash2 className="h-3 w-3" />
                </button>
              </div>
            ))}
          </div>
        )}
      </section>

      {/* Bản dịch */}
      <section className="rounded-xl border border-slate-800 bg-[#202226] p-4">
        <div className="mb-3 flex flex-wrap items-center gap-2">
          <Globe className="h-4 w-4 text-slate-500" />
          {article.translations.map((tr) => (
            <button
              key={tr.language}
              onClick={() => setLanguage(tr.language)}
              className={cn(
                'rounded-lg px-2.5 py-1.5 text-xs font-semibold transition-colors',
                tr.language === language ? 'bg-sky-500 text-white' : 'bg-[#1A1B1F] text-slate-400 hover:text-white',
              )}
            >
              {tr.language.toUpperCase()}
              <span className="ml-1 text-[10px] opacity-70">({tr.blockCount})</span>
            </button>
          ))}

          {missingLanguages.length > 0 && (
            <select
              value=""
              onChange={(e) => e.target.value && setLanguage(e.target.value)}
              className="rounded-lg border border-dashed border-slate-700 bg-[#1A1B1F] px-2 py-1.5 text-xs text-slate-400"
            >
              <option value="">{t('adminArticles.addTranslation')}</option>
              {missingLanguages.map((l) => <option key={l} value={l}>{l.toUpperCase()}</option>)}
            </select>
          )}

          {article.translations.length > 1 && article.translations.some((tr) => tr.language === language) && (
            <button onClick={removeTranslation} className="ml-auto text-xs text-red-400 hover:text-red-300">
              {t('adminArticles.deleteTranslation')}
            </button>
          )}
        </div>

        <div className="space-y-3">
          <label className="block text-xs text-slate-400">
            {t('adminArticles.articleTitle')}
            <input value={title} onChange={(e) => setTitle(e.target.value)} className={`${inputCls} mt-1`} />
          </label>
          <label className="block text-xs text-slate-400">
            {t('adminArticles.summary')}
            <textarea value={summary} onChange={(e) => setSummary(e.target.value)} rows={2} className={`${inputCls} mt-1`} />
          </label>
        </div>
      </section>

      {loadingContent ? (
        <div className="flex items-center justify-center py-10 text-slate-600">
          <Loader2 className="h-5 w-5 animate-spin" />
        </div>
      ) : (
        <>
          <BlockListEditor
            label={t('adminArticles.intro')}
            hint={t('adminArticles.introHint')}
            blocks={intro} assets={article.assets} onChange={setIntro}
          />
          <BlockListEditor
            label={t('adminArticles.body')}
            hint={t('adminArticles.bodyHint')}
            blocks={body} assets={article.assets} onChange={setBody} allowBulkPaste
          />
          <BlockListEditor
            label={t('adminArticles.conclusion')}
            hint={t('adminArticles.conclusionHint')}
            blocks={conclusion} assets={article.assets} onChange={setConclusion}
          />
        </>
      )}

      {/* Thanh lưu dính đáy — bài dài không phải cuộn ngược lên để bấm lưu */}
      <div className="sticky bottom-0 -mx-4 border-t border-slate-800 bg-[#141518]/95 px-4 py-3 backdrop-blur sm:-mx-6 sm:px-6">
        <button
          onClick={saveContent}
          disabled={saving || !title.trim()}
          className="flex w-full items-center justify-center gap-2 rounded-xl bg-sky-500 py-3 text-sm font-semibold text-white hover:bg-sky-400 disabled:opacity-50"
        >
          {saving ? <Loader2 className="h-4 w-4 animate-spin" /> : <Save className="h-4 w-4" />}
          {t('adminArticles.saveContent', { lang: language.toUpperCase() })}
        </button>
      </div>
    </div>
  );
}

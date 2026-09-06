import { useCallback, useEffect, useState } from 'react';
import {
  getAdminArticles, createArticle, useTranslation, useDebounce, cn,
  ArticleStatus, ArticleType, ReadingLevel,
} from '@fishlover/shared';
import type { AdminArticleDto } from '@fishlover/shared';
import { Plus, Search, Loader2, FileText, Eye, Pencil, X } from 'lucide-react';
import ArticleEditor from './components/ArticleEditor';
import { ARTICLE_TYPES, READING_LEVELS, TYPE_KEYS, LEVEL_KEYS, formatArticleDate } from '../articles/labels';

const PAGE_SIZE = 20;

const STATUS_BADGE: Record<ArticleStatus, string> = {
  [ArticleStatus.Draft]:     'border-amber-500/20 bg-amber-500/10 text-amber-400',
  [ArticleStatus.Published]: 'border-emerald-500/20 bg-emerald-500/10 text-emerald-400',
  [ArticleStatus.Archived]:  'border-slate-500/20 bg-slate-500/10 text-slate-400',
};

const inputCls =
  'w-full rounded-lg border border-slate-700 bg-[#141518] px-3 py-2 text-base text-slate-200 placeholder:text-slate-600 focus:border-sky-500/50 focus:outline-none sm:text-sm';

/** Form tạo bài: chỉ hỏi những gì bắt buộc, nội dung soạn ở trang sửa. */
function CreateForm({ onCreated, onCancel }: { onCreated: (id: string) => void; onCancel: () => void }) {
  const { t } = useTranslation();
  const [title, setTitle] = useState('');
  const [language, setLanguage] = useState('vi');
  const [type, setType] = useState<ArticleType>(ArticleType.Setup);
  const [level, setLevel] = useState<ReadingLevel>(ReadingLevel.Beginner);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');

  const submit = async () => {
    if (!title.trim()) return;
    setBusy(true);
    setError('');
    try {
      const created = await createArticle({
        type, readingLevel: level, language, title: title.trim(),
      });
      onCreated(created.id);
    } catch {
      setError(t('adminArticles.createError'));
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="rounded-xl border border-slate-800 bg-[#202226] p-4">
      <div className="mb-3 flex items-center">
        <h3 className="text-sm font-semibold text-white">{t('adminArticles.createTitle')}</h3>
        <button onClick={onCancel} className="ml-auto text-slate-500 hover:text-white">
          <X className="h-4 w-4" />
        </button>
      </div>

      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
        <label className="text-xs text-slate-400 sm:col-span-2">
          {t('adminArticles.articleTitle')}
          <input
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            placeholder={t('adminArticles.titlePlaceholder')}
            className={`${inputCls} mt-1`}
          />
        </label>

        <label className="text-xs text-slate-400">
          {t('adminArticles.language')}
          <select value={language} onChange={(e) => setLanguage(e.target.value)} className={`${inputCls} mt-1`}>
            {['vi', 'en', 'de', 'zh'].map((l) => <option key={l} value={l}>{l.toUpperCase()}</option>)}
          </select>
        </label>

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
      </div>

      {error && <p className="mt-2 text-xs text-red-400">{error}</p>}

      <button
        onClick={submit}
        disabled={busy || !title.trim()}
        className="mt-3 flex items-center gap-2 rounded-lg bg-sky-500 px-4 py-2 text-sm font-semibold text-white hover:bg-sky-400 disabled:opacity-50"
      >
        {busy ? <Loader2 className="h-4 w-4 animate-spin" /> : <Plus className="h-4 w-4" />}
        {t('adminArticles.createAction')}
      </button>
    </div>
  );
}

/**
 * Quản lý bài viết — ContentAdmin/SystemAdmin (route đã bọc RoleGuard).
 * Danh sách ở ngoài, bấm vào một bài thì chuyển hẳn sang trình soạn thay vì mở modal:
 * soạn bài là việc dài, không nên nhốt trong hộp thoại trên màn 390px.
 */
export default function AdminArticlesPage() {
  const { t, i18n } = useTranslation();
  const lang = i18n.language?.slice(0, 2) ?? 'vi';

  const [articles, setArticles] = useState<AdminArticleDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(false);
  const [status, setStatus] = useState<ArticleStatus | undefined>();
  const [query, setQuery] = useState('');
  const debounced = useDebounce(query, 400);
  const [creating, setCreating] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(false);
    try {
      const res = await getAdminArticles({ status, q: debounced.trim() || undefined, page: 1, pageSize: PAGE_SIZE });
      setArticles(res.items);
    } catch {
      setError(true);
    } finally {
      setLoading(false);
    }
  }, [status, debounced]);

  useEffect(() => { void load(); }, [load]);

  if (editingId) {
    return (
      <div className="px-4 pt-6 sm:px-6">
        <ArticleEditor articleId={editingId} onBack={() => setEditingId(null)} onChanged={() => void load()} />
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-5 px-4 pb-10 pt-6 sm:px-6">
      <header className="flex flex-wrap items-center gap-3">
        <div>
          <h1 className="text-xl font-semibold text-white sm:text-2xl">{t('nav.articlesManager')}</h1>
          <p className="mt-1 text-sm text-slate-400">{t('adminArticles.subtitle')}</p>
        </div>
        <button
          onClick={() => setCreating((v) => !v)}
          className="ml-auto flex min-h-[44px] items-center gap-2 rounded-xl bg-sky-500 px-4 py-2 text-sm font-semibold text-white hover:bg-sky-400"
        >
          <Plus className="h-4 w-4" /> {t('adminArticles.newArticle')}
        </button>
      </header>

      {creating && (
        <CreateForm
          onCancel={() => setCreating(false)}
          onCreated={(id) => { setCreating(false); void load(); setEditingId(id); }}
        />
      )}

      <div className="flex flex-wrap items-center gap-2">
        <div className="relative min-w-[200px] flex-1">
          <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-500" />
          <input
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder={t('adminArticles.searchPlaceholder')}
            className="min-h-[44px] w-full rounded-lg border border-slate-700 bg-[#1E293B] py-2 pl-10 pr-4 text-base text-slate-200 placeholder:text-slate-500 focus:border-sky-500/50 focus:outline-none sm:text-sm"
          />
        </div>
        <select
          value={status ?? ''}
          onChange={(e) => setStatus(e.target.value === '' ? undefined : Number(e.target.value) as ArticleStatus)}
          className="min-h-[44px] rounded-lg border border-slate-700 bg-[#1E293B] px-2.5 py-2 text-base text-white sm:min-h-0 sm:text-sm"
        >
          <option value="">{t('adminArticles.allStatuses')}</option>
          <option value={ArticleStatus.Draft}>{t('adminArticles.statusDraft')}</option>
          <option value={ArticleStatus.Published}>{t('adminArticles.statusPublished')}</option>
          <option value={ArticleStatus.Archived}>{t('adminArticles.statusArchived')}</option>
        </select>
      </div>

      {loading && (
        <div className="flex items-center justify-center py-16 text-slate-600">
          <Loader2 className="h-6 w-6 animate-spin" />
        </div>
      )}

      {!loading && error && (
        <div className="rounded-xl border border-red-500/30 bg-red-500/10 p-4 text-sm text-red-400">
          {t('adminArticles.loadError')}
        </div>
      )}

      {!loading && !error && articles.length === 0 && (
        <div className="flex flex-col items-center justify-center py-16 text-center">
          <FileText className="mb-3 h-12 w-12 text-slate-700" />
          <p className="text-sm text-slate-500">{t('adminArticles.empty')}</p>
        </div>
      )}

      {!loading && !error && articles.length > 0 && (
        <div className="space-y-2">
          {articles.map((a) => {
            const primary = a.translations[0];
            return (
              <button
                key={a.id}
                onClick={() => setEditingId(a.id)}
                className="flex w-full items-center gap-3 rounded-xl border border-slate-800 bg-[#202226] p-3 text-left transition-colors hover:border-sky-500/40"
              >
                {a.thumbnailUrl
                  ? <img src={a.thumbnailUrl} alt="" className="h-12 w-16 shrink-0 rounded-lg border border-slate-700 object-cover" />
                  : <div className="flex h-12 w-16 shrink-0 items-center justify-center rounded-lg border border-slate-800 bg-[#141518]">
                      <FileText className="h-4 w-4 text-slate-700" />
                    </div>
                }

                <div className="min-w-0 flex-1">
                  <p className="truncate text-sm font-semibold text-white">
                    {primary?.title ?? a.slug}
                  </p>
                  <p className="truncate text-xs text-slate-500">/{a.slug}</p>
                  <div className="mt-1.5 flex flex-wrap items-center gap-1.5 text-[10px]">
                    <span className={cn('rounded-full border px-1.5 py-0.5 font-semibold', STATUS_BADGE[a.status])}>
                      {t(`adminArticles.status${ArticleStatus[a.status]}`)}
                    </span>
                    <span className="rounded-full border border-slate-700 px-1.5 py-0.5 text-slate-400">
                      {t(TYPE_KEYS[a.type])}
                    </span>
                    {a.translations.map((tr) => (
                      <span key={tr.language} className="rounded-full bg-slate-800 px-1.5 py-0.5 text-slate-400">
                        {tr.language.toUpperCase()}
                      </span>
                    ))}
                  </div>
                </div>

                <div className="hidden shrink-0 flex-col items-end gap-1 text-xs text-slate-500 sm:flex">
                  <span className="flex items-center gap-1"><Eye className="h-3.5 w-3.5" />{a.viewCount}</span>
                  <span>{formatArticleDate(a.updatedAt, lang)}</span>
                </div>

                <Pencil className="h-4 w-4 shrink-0 text-slate-600" />
              </button>
            );
          })}
        </div>
      )}
    </div>
  );
}

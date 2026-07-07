import { useEffect, useState } from 'react';
import {
  getAllContests, createContest, updateContest,
  getPendingReviewEntries, approveContestEntry, rejectContestEntry,
  ContestStatus, useTranslation, cn,
} from '@fishlover/shared';
import type { ContestDto, ContestEntryDto, CreateContestRequest } from '@fishlover/shared';
import { Trophy, Plus, Pencil, Youtube, Check, X, Loader2, ClipboardCheck } from 'lucide-react';

function formatDate(iso: string, locale: string) {
  return new Date(iso).toLocaleDateString(locale, { day: '2-digit', month: '2-digit', year: 'numeric' });
}

// datetime-local input cần format yyyy-MM-ddTHH:mm
function toInputValue(iso: string | null): string {
  if (!iso) return '';
  return new Date(iso).toISOString().slice(0, 16);
}

const STATUS_STYLES: Record<number, string> = {
  [ContestStatus.Draft]:  'bg-white/5 text-slate-400 border-white/10',
  [ContestStatus.Active]: 'bg-emerald-500/10 text-emerald-400 border-emerald-500/30',
  [ContestStatus.Ended]:  'bg-slate-500/10 text-slate-500 border-slate-500/30',
};

// ── Contest form modal (create / edit) ──────────────────────────────────────
function ContestFormModal({ contest, onClose, onSaved, t }: {
  contest: ContestDto | null;
  onClose: () => void;
  onSaved: () => void;
  t: ReturnType<typeof useTranslation>['t'];
}) {
  const [title, setTitle] = useState(contest?.title ?? '');
  const [description, setDescription] = useState(contest?.description ?? '');
  const [playlistId, setPlaylistId] = useState(contest?.youTubePlaylistId ?? '');
  const [startAt, setStartAt] = useState(toInputValue(contest?.startAt ?? null));
  const [endAt, setEndAt] = useState(toInputValue(contest?.endAt ?? null));
  const [status, setStatus] = useState<ContestStatus>(contest?.status ?? ContestStatus.Draft);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState(false);

  const canSave = title.trim() && startAt && endAt;

  const handleSave = async () => {
    if (!canSave) return;
    setSaving(true);
    setError(false);
    try {
      const payload: CreateContestRequest = {
        title: title.trim(),
        description: description.trim() || null,
        youTubePlaylistId: playlistId.trim() || null,
        startAt: new Date(startAt).toISOString(),
        endAt: new Date(endAt).toISOString(),
      };
      if (contest) {
        await updateContest(contest.id, { ...payload, status });
      } else {
        await createContest(payload);
      }
      onSaved();
      onClose();
    } catch {
      setError(true);
    } finally {
      setSaving(false);
    }
  };

  const inputCls = 'w-full bg-[#0F172A] border border-slate-700 rounded-xl px-3 py-3 text-base text-white min-h-[44px] placeholder:text-slate-600';
  const labelCls = 'block text-xs font-bold text-slate-500 uppercase tracking-wider mb-2';

  return (
    <>
      <div className="fixed inset-0 bg-black/60 z-50" onClick={onClose} />
      <div className="fixed inset-0 z-50 flex items-center justify-center p-4 pointer-events-none">
        <div className="w-full max-w-md max-h-[90vh] overflow-y-auto bg-[#172033] border border-slate-700 rounded-2xl pointer-events-auto">
          <div className="flex items-center justify-between px-5 py-4 border-b border-slate-800">
            <h2 className="text-base font-bold text-white">
              {contest ? t('adminContests.editTitle') : t('adminContests.createTitle')}
            </h2>
            <button onClick={onClose} className="p-2 rounded-lg hover:bg-white/10 text-slate-500 min-w-[44px] min-h-[44px] flex items-center justify-center">
              <X className="w-4 h-4" />
            </button>
          </div>

          <div className="p-5 space-y-4">
            {error && (
              <div className="rounded-xl border border-red-500/30 bg-red-500/10 p-3 text-sm text-red-400">
                {t('adminContests.saveError')}
              </div>
            )}

            <div>
              <label className={labelCls}>{t('adminContests.fieldTitle')}</label>
              <input value={title} onChange={e => setTitle(e.target.value)} className={inputCls} />
            </div>
            <div>
              <label className={labelCls}>{t('adminContests.fieldDescription')}</label>
              <textarea value={description} onChange={e => setDescription(e.target.value)} rows={3} className={inputCls} />
            </div>
            <div>
              <label className={labelCls}>{t('adminContests.fieldPlaylist')}</label>
              <input value={playlistId} onChange={e => setPlaylistId(e.target.value)} placeholder="PLxxxxxxxx" className={inputCls} />
            </div>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <div>
                <label className={labelCls}>{t('adminContests.fieldStartAt')}</label>
                <input type="datetime-local" value={startAt} onChange={e => setStartAt(e.target.value)} className={inputCls} />
              </div>
              <div>
                <label className={labelCls}>{t('adminContests.fieldEndAt')}</label>
                <input type="datetime-local" value={endAt} onChange={e => setEndAt(e.target.value)} className={inputCls} />
              </div>
            </div>
            {contest && (
              <div>
                <label className={labelCls}>{t('adminContests.fieldStatus')}</label>
                <select value={status} onChange={e => setStatus(Number(e.target.value) as ContestStatus)} className={inputCls}>
                  <option value={ContestStatus.Draft}>{t('adminContests.statusDraft')}</option>
                  <option value={ContestStatus.Active}>{t('adminContests.statusActive')}</option>
                  <option value={ContestStatus.Ended}>{t('adminContests.statusEnded')}</option>
                </select>
              </div>
            )}

            <button
              onClick={handleSave}
              disabled={!canSave || saving}
              className="w-full py-3 rounded-xl bg-sky-500 text-white text-sm font-bold hover:bg-sky-400 transition-colors disabled:opacity-40 flex items-center justify-center gap-2 min-h-[44px]"
            >
              {saving && <Loader2 className="w-4 h-4 animate-spin" />}
              {t('adminContests.save')}
            </button>
          </div>
        </div>
      </div>
    </>
  );
}

// ── Entry review row ────────────────────────────────────────────────────────
function EntryReviewRow({ entry, onDone, t }: {
  entry: ContestEntryDto;
  onDone: () => void;
  t: ReturnType<typeof useTranslation>['t'];
}) {
  const [busy, setBusy] = useState<'approve' | 'reject' | null>(null);

  const act = async (action: 'approve' | 'reject') => {
    if (action === 'reject' && !window.confirm(t('adminContests.rejectConfirm'))) return;
    setBusy(action);
    try {
      if (action === 'approve') await approveContestEntry(entry.contestId, entry.id);
      else await rejectContestEntry(entry.contestId, entry.id);
      onDone();
    } catch (err) {
      console.error(err);
      setBusy(null);
    }
  };

  return (
    <div className="flex flex-col sm:flex-row sm:items-center gap-3 rounded-xl border border-slate-800/60 bg-[#1E293B] p-4">
      <div className="flex-1 min-w-0">
        <p className="text-sm font-semibold text-white">
          {t('adminContests.entrySubmittedAt')}: {formatDate(entry.submittedAt, t('aquarium.dateLocale'))}
        </p>
        {entry.youTubeVideoId && (
          <a
            href={`https://www.youtube.com/watch?v=${entry.youTubeVideoId}`}
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex items-center gap-1.5 text-xs text-red-400 hover:underline mt-1"
          >
            <Youtube className="w-3.5 h-3.5" /> {t('adminContests.watchUnlisted')}
          </a>
        )}
      </div>
      <div className="flex gap-2 shrink-0">
        <button
          onClick={() => act('approve')}
          disabled={busy !== null}
          className="flex items-center gap-1.5 px-4 py-2.5 rounded-xl bg-emerald-500/15 border border-emerald-500/40 text-emerald-400 text-xs font-bold hover:bg-emerald-500/25 transition-colors disabled:opacity-40 min-h-[44px]"
        >
          {busy === 'approve' ? <Loader2 className="w-3.5 h-3.5 animate-spin" /> : <Check className="w-3.5 h-3.5" />}
          {t('adminContests.approve')}
        </button>
        <button
          onClick={() => act('reject')}
          disabled={busy !== null}
          className="flex items-center gap-1.5 px-4 py-2.5 rounded-xl bg-red-500/15 border border-red-500/40 text-red-400 text-xs font-bold hover:bg-red-500/25 transition-colors disabled:opacity-40 min-h-[44px]"
        >
          {busy === 'reject' ? <Loader2 className="w-3.5 h-3.5 animate-spin" /> : <X className="w-3.5 h-3.5" />}
          {t('adminContests.reject')}
        </button>
      </div>
    </div>
  );
}

// ── Page ────────────────────────────────────────────────────────────────────
export default function AdminContestsPage() {
  const { t } = useTranslation();

  const [contests, setContests] = useState<ContestDto[]>([]);
  const [entries, setEntries] = useState<ContestEntryDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [formContest, setFormContest] = useState<ContestDto | null>(null);
  const [formOpen, setFormOpen] = useState(false);

  const load = () => {
    setLoading(true);
    Promise.all([
      getAllContests().catch(() => [] as ContestDto[]),
      getPendingReviewEntries().catch(() => [] as ContestEntryDto[]),
    ])
      .then(([contestList, entryList]) => {
        setContests(contestList);
        setEntries(entryList);
      })
      .finally(() => setLoading(false));
  };

  useEffect(load, []);

  const statusLabel = (status: ContestStatus) => {
    switch (status) {
      case ContestStatus.Active: return t('adminContests.statusActive');
      case ContestStatus.Ended:  return t('adminContests.statusEnded');
      default: return t('adminContests.statusDraft');
    }
  };

  return (
    <div className="px-4 sm:px-6 pt-5 pb-10">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-xl sm:text-2xl font-black text-white">{t('adminContests.title')}</h1>
          <p className="text-sm text-slate-500 mt-1">{t('adminContests.subtitle')}</p>
        </div>
        <button
          onClick={() => { setFormContest(null); setFormOpen(true); }}
          className="flex items-center gap-1.5 px-3.5 py-2.5 rounded-xl bg-sky-500 text-white text-xs font-bold hover:bg-sky-400 transition-colors min-h-[44px]"
        >
          <Plus className="w-3.5 h-3.5" /> {t('adminContests.createBtn')}
        </button>
      </div>

      {loading && (
        <div className="flex items-center justify-center py-20 text-slate-600">
          <Loader2 className="w-6 h-6 animate-spin" />
        </div>
      )}

      {!loading && (
        <>
          {/* Contest list */}
          <div className="space-y-2 mb-10">
            {contests.length === 0 && (
              <div className="flex flex-col items-center justify-center py-12 text-center rounded-2xl border border-dashed border-slate-800/60">
                <Trophy className="w-10 h-10 text-slate-700 mb-2" />
                <p className="text-slate-500 text-sm">{t('adminContests.noContests')}</p>
              </div>
            )}
            {contests.map(c => (
              <div key={c.id} className="flex items-center gap-3 rounded-xl border border-slate-800/60 bg-[#1E293B] p-4">
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2">
                    <p className="text-sm font-bold text-white truncate">{c.title}</p>
                    <span className={cn('text-[10px] font-bold px-2 py-0.5 rounded-full border shrink-0', STATUS_STYLES[c.status])}>
                      {statusLabel(c.status)}
                    </span>
                  </div>
                  <p className="text-xs text-slate-500 mt-0.5">
                    {formatDate(c.startAt, t('aquarium.dateLocale'))} → {formatDate(c.endAt, t('aquarium.dateLocale'))}
                  </p>
                </div>
                <button
                  onClick={() => { setFormContest(c); setFormOpen(true); }}
                  className="p-2.5 rounded-lg bg-white/5 hover:bg-white/10 text-slate-400 transition-colors min-w-[44px] min-h-[44px] flex items-center justify-center"
                  title={t('adminContests.editTitle')}
                >
                  <Pencil className="w-4 h-4" />
                </button>
              </div>
            ))}
          </div>

          {/* Pending review entries */}
          <h2 className="flex items-center gap-2 text-sm font-bold text-slate-400 uppercase tracking-wider mb-4">
            <ClipboardCheck className="w-4 h-4" />
            {t('adminContests.pendingReview')} ({entries.length})
          </h2>
          <div className="space-y-2">
            {entries.length === 0 && (
              <p className="text-sm text-slate-600 py-6 text-center">{t('adminContests.noPending')}</p>
            )}
            {entries.map(e => (
              <EntryReviewRow key={e.id} entry={e} onDone={load} t={t} />
            ))}
          </div>
        </>
      )}

      {formOpen && (
        <ContestFormModal
          contest={formContest}
          onClose={() => setFormOpen(false)}
          onSaved={load}
          t={t}
        />
      )}
    </div>
  );
}

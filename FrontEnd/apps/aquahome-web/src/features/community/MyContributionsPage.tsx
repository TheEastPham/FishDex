import { useEffect, useState } from 'react';
import {
  getMyCommunitySpecies, getMyCommonNames, deleteCommunitySpecies, deleteCommonName,
  useTranslation, CommunitySpeciesKind,
} from '@fishlover/shared';
import type { CommunitySpeciesDto, CommunityCommonNameDto } from '@fishlover/shared';
import { Loader2, Fish, Languages, Sparkles, Pencil, Trash2 } from 'lucide-react';
import SubmitCommunitySpeciesModal from './SubmitCommunitySpeciesModal';
import AddLocalNameModal from './AddLocalNameModal';

type T = ReturnType<typeof useTranslation>['t'];
type Tab = 'species' | 'names';

const isPending = <T extends { isVerified: boolean; rejectionReason: string | null }>(n: T) =>
  !n.isVerified && !n.rejectionReason;

/** User chỉ được tự xoá khi CHƯA được duyệt (pending hoặc đã bị admin từ chối) — verified rồi thì không cho. */
const canDelete = <T extends { isVerified: boolean }>(n: T) => !n.isVerified;

function StatusBadge({ isVerified, rejectionReason, t }: { isVerified: boolean; rejectionReason: string | null; t: T }) {
  const [label, cls] = isVerified
    ? [t('myContributions.statusVerified'), 'bg-emerald-500/15 text-emerald-400 border-emerald-500/30']
    : rejectionReason
      ? [t('myContributions.statusRejected'), 'bg-red-500/10 text-red-400 border-red-500/30']
      : [t('myContributions.statusPending'), 'bg-amber-500/10 text-amber-300 border-amber-500/30'];
  return <span className={`shrink-0 px-2.5 py-1 rounded-full border text-xs font-bold ${cls}`}>{label}</span>;
}

function KindBadge({ suggestedKind, kind, t }: { suggestedKind: CommunitySpeciesKind | null; kind: CommunitySpeciesKind | null; t: T }) {
  const value = kind ?? suggestedKind;
  if (value === null || value === undefined) return null;
  const label = value === CommunitySpeciesKind.Hybrid ? t('contribute.kindHybrid') : t('contribute.kindNatural');
  return <span className="shrink-0 px-2.5 py-1 rounded-full border border-slate-700 bg-slate-800/60 text-xs font-bold text-slate-300">{label}</span>;
}

function EditButton({ onClick, t }: { onClick: () => void; t: T }) {
  return (
    <button
      onClick={onClick}
      className="shrink-0 flex items-center justify-center w-11 h-11 rounded-lg border border-slate-700 text-slate-400 hover:bg-white/5 hover:text-white"
      title={t('myContributions.edit')}
    >
      <Pencil className="w-4 h-4" />
    </button>
  );
}

function DeleteButton({ onClick, busy, t }: { onClick: () => void; busy: boolean; t: T }) {
  return (
    <button
      onClick={onClick}
      disabled={busy}
      className="shrink-0 flex items-center justify-center w-11 h-11 rounded-lg border border-red-500/30 text-red-400 hover:bg-red-500/10 disabled:opacity-50"
      title={t('myContributions.delete')}
    >
      {busy ? <Loader2 className="w-4 h-4 animate-spin" /> : <Trash2 className="w-4 h-4" />}
    </button>
  );
}

export default function MyContributionsPage() {
  const { t } = useTranslation();
  const [tab, setTab] = useState<Tab>('species');
  const [species, setSpecies] = useState<CommunitySpeciesDto[]>([]);
  const [names, setNames] = useState<CommunityCommonNameDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [editingSpecies, setEditingSpecies] = useState<CommunitySpeciesDto | null>(null);
  const [editingName, setEditingName] = useState<CommunityCommonNameDto | null>(null);
  const [deletingKey, setDeletingKey] = useState<string | null>(null);

  const load = () => {
    setLoading(true);
    Promise.all([
      getMyCommunitySpecies().catch(() => [] as CommunitySpeciesDto[]),
      getMyCommonNames().catch(() => [] as CommunityCommonNameDto[]),
    ])
      .then(([sp, nm]) => { setSpecies(sp); setNames(nm); })
      .finally(() => setLoading(false));
  };
  useEffect(load, []);

  const isEmpty = species.length === 0 && names.length === 0;

  const handleDeleteSpecies = async (s: CommunitySpeciesDto) => {
    if (!window.confirm(t('myContributions.deleteConfirm'))) return;
    setDeletingKey(`species-${s.specCode}`);
    try {
      await deleteCommunitySpecies(s.specCode);
      load();
    } finally {
      setDeletingKey(null);
    }
  };

  const handleDeleteName = async (n: CommunityCommonNameDto) => {
    if (!window.confirm(t('myContributions.deleteConfirm'))) return;
    setDeletingKey(`name-${n.autoCtr}`);
    try {
      await deleteCommonName(n.autoCtr);
      load();
    } finally {
      setDeletingKey(null);
    }
  };

  return (
    <div className="max-w-3xl mx-auto w-full px-4 py-8 flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-bold text-white flex items-center gap-2">
          <Sparkles className="w-6 h-6 text-sky-400" />
          {t('myContributions.title')}
        </h1>
        <p className="text-slate-400 text-sm mt-1">{t('myContributions.subtitle')}</p>
      </div>

      {loading ? (
        <div className="flex items-center justify-center py-16 text-slate-600">
          <Loader2 className="w-6 h-6 animate-spin" />
        </div>
      ) : isEmpty ? (
        <p className="text-sm text-slate-500 py-10 text-center bg-[#202226] rounded-2xl border border-dashed border-slate-700/50">
          {t('myContributions.empty')}
        </p>
      ) : (
        <>
          <div className="flex gap-2 border-b border-slate-800">
            <button
              onClick={() => setTab('species')}
              className={`flex items-center gap-2 px-4 py-3 text-sm font-bold border-b-2 -mb-px transition-colors ${
                tab === 'species' ? 'border-sky-500 text-white' : 'border-transparent text-slate-500 hover:text-slate-300'
              }`}
            >
              <Fish className="w-4 h-4" /> {t('myContributions.speciesSection')} ({species.length})
            </button>
            <button
              onClick={() => setTab('names')}
              className={`flex items-center gap-2 px-4 py-3 text-sm font-bold border-b-2 -mb-px transition-colors ${
                tab === 'names' ? 'border-sky-500 text-white' : 'border-transparent text-slate-500 hover:text-slate-300'
              }`}
            >
              <Languages className="w-4 h-4" /> {t('myContributions.namesSection')} ({names.length})
            </button>
          </div>

          {tab === 'species' && (
            <section className="space-y-3">
              {species.length === 0 ? (
                <p className="text-sm text-slate-500 py-10 text-center bg-[#202226] rounded-2xl border border-dashed border-slate-700/50">
                  {t('myContributions.empty')}
                </p>
              ) : (
                species.map((s) => (
                  <div key={s.specCode} className="flex items-center gap-3 p-4 rounded-xl bg-[#0F172A] border border-slate-800">
                    <div className="min-w-0 flex-1">
                      <p className="text-sm font-bold text-white truncate">{s.commonName || s.speciesName}</p>
                      <p className="text-xs text-slate-500 truncate">{s.speciesName}{s.rejectionReason ? ` · ${s.rejectionReason}` : ''}</p>
                    </div>
                    <KindBadge suggestedKind={s.suggestedKind} kind={s.kind} t={t} />
                    <StatusBadge isVerified={s.isVerified} rejectionReason={s.rejectionReason} t={t} />
                    {isPending(s) && <EditButton onClick={() => setEditingSpecies(s)} t={t} />}
                    {canDelete(s) && (
                      <DeleteButton onClick={() => handleDeleteSpecies(s)} busy={deletingKey === `species-${s.specCode}`} t={t} />
                    )}
                  </div>
                ))
              )}
            </section>
          )}

          {tab === 'names' && (
            <section className="space-y-3">
              {names.length === 0 ? (
                <p className="text-sm text-slate-500 py-10 text-center bg-[#202226] rounded-2xl border border-dashed border-slate-700/50">
                  {t('myContributions.empty')}
                </p>
              ) : (
                names.map((n) => (
                  <div key={n.autoCtr} className="flex items-center gap-3 p-4 rounded-xl bg-[#0F172A] border border-slate-800">
                    <div className="min-w-0 flex-1">
                      <p className="text-sm font-bold text-white truncate">{n.comName}</p>
                      <p className="text-xs text-slate-500 truncate">
                        {[n.language, n.countryCode].filter(Boolean).join(' · ') || '—'}{` · #${n.specCode}`}
                        {n.rejectionReason ? ` · ${n.rejectionReason}` : ''}
                      </p>
                    </div>
                    <StatusBadge isVerified={n.isVerified} rejectionReason={n.rejectionReason} t={t} />
                    {isPending(n) && <EditButton onClick={() => setEditingName(n)} t={t} />}
                    {canDelete(n) && (
                      <DeleteButton onClick={() => handleDeleteName(n)} busy={deletingKey === `name-${n.autoCtr}`} t={t} />
                    )}
                  </div>
                ))
              )}
            </section>
          )}
        </>
      )}

      {editingSpecies && (
        <SubmitCommunitySpeciesModal
          editing={editingSpecies}
          onClose={() => { setEditingSpecies(null); load(); }}
        />
      )}

      {editingName && (
        <AddLocalNameModal
          specCode={editingName.specCode}
          initialLanguage={editingName.language ?? 'Vietnamese'}
          onSaved={load}
          onClose={() => setEditingName(null)}
        />
      )}
    </div>
  );
}

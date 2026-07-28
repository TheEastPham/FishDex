import { useEffect, useState } from 'react';
import {
  getPendingCommunitySpecies, verifyCommunitySpecies, rejectCommunitySpecies,
  getPendingCommonNames, verifyCommonName, verifyCommonNamesBatch, rejectCommonName,
  useTranslation, CommunitySpeciesKind,
} from '@fishlover/shared';
import type { CommunitySpeciesDto, CommunityCommonNameDto } from '@fishlover/shared';
import { Loader2, Check, X, Fish, Languages, ShieldCheck } from 'lucide-react';

type T = ReturnType<typeof useTranslation>['t'];

/* ── Species pending row ─────────────────────────────────── */
function SpeciesRow({ item, t, onDone }: { item: CommunitySpeciesDto; t: T; onDone: () => void }) {
  const [busy, setBusy] = useState<'approve' | 'reject' | null>(null);
  const [kind, setKind] = useState<CommunitySpeciesKind | ''>(item.suggestedKind ?? '');

  const act = async (action: 'approve' | 'reject') => {
    let reason = '';
    if (action === 'reject') {
      const r = window.prompt(t('adminCommunity.rejectPrompt'));
      if (!r || !r.trim()) return;
      reason = r.trim();
    }
    setBusy(action);
    try {
      if (action === 'approve') await verifyCommunitySpecies(item.specCode, kind === '' ? null : kind);
      else await rejectCommunitySpecies(item.specCode, reason);
      onDone();
    } catch {
      setBusy(null);
    }
  };

  return (
    <div className="flex flex-col sm:flex-row sm:items-center gap-3 p-4 rounded-xl bg-[#0F172A] border border-slate-800">
      <div className="min-w-0 flex-1">
        <p className="text-sm font-bold text-white truncate">{item.commonName || item.speciesName}</p>
        <p className="text-xs text-slate-500 truncate">
          {item.speciesName}
          {item.familyName ? ` · ${item.familyName}` : ''}
          {` · #${item.specCode}`}
        </p>
      </div>
      <select value={kind} onChange={(e) => setKind(e.target.value === '' ? '' : (Number(e.target.value) as CommunitySpeciesKind))}
        className="shrink-0 rounded-lg bg-[#141518] border border-slate-700 px-2.5 py-2 text-xs text-slate-300 min-h-[44px]">
        <option value="">{t('contribute.kindNone')}</option>
        <option value={CommunitySpeciesKind.Natural}>{t('contribute.kindNatural')}</option>
        <option value={CommunitySpeciesKind.Hybrid}>{t('contribute.kindHybrid')}</option>
      </select>
      <div className="flex gap-2 shrink-0">
        <button onClick={() => act('approve')} disabled={!!busy}
          className="flex items-center gap-1.5 px-3 py-2 rounded-lg bg-emerald-500/15 text-emerald-400 border border-emerald-500/30 text-xs font-bold hover:bg-emerald-500/25 disabled:opacity-50 min-h-[44px]">
          {busy === 'approve' ? <Loader2 className="w-4 h-4 animate-spin" /> : <Check className="w-4 h-4" />}
          {t('adminCommunity.approve')}
        </button>
        <button onClick={() => act('reject')} disabled={!!busy}
          className="flex items-center gap-1.5 px-3 py-2 rounded-lg bg-red-500/10 text-red-400 border border-red-500/30 text-xs font-bold hover:bg-red-500/20 disabled:opacity-50 min-h-[44px]">
          {busy === 'reject' ? <Loader2 className="w-4 h-4 animate-spin" /> : <X className="w-4 h-4" />}
          {t('adminCommunity.reject')}
        </button>
      </div>
    </div>
  );
}

/* ── Common-name pending row (with checkbox for bulk) ────── */
function NameRow({ item, t, checked, onToggle, onDone }: {
  item: CommunityCommonNameDto; t: T; checked: boolean; onToggle: () => void; onDone: () => void;
}) {
  const [busy, setBusy] = useState<'approve' | 'reject' | null>(null);

  const act = async (action: 'approve' | 'reject') => {
    let reason = '';
    if (action === 'reject') {
      const r = window.prompt(t('adminCommunity.rejectPrompt'));
      if (!r || !r.trim()) return;
      reason = r.trim();
    }
    setBusy(action);
    try {
      if (action === 'approve') await verifyCommonName(item.autoCtr);
      else await rejectCommonName(item.autoCtr, reason);
      onDone();
    } catch {
      setBusy(null);
    }
  };

  return (
    <div className="flex items-center gap-3 p-4 rounded-xl bg-[#0F172A] border border-slate-800">
      <input type="checkbox" checked={checked} onChange={onToggle}
        className="w-5 h-5 shrink-0 accent-sky-500" />
      <div className="min-w-0 flex-1">
        <p className="text-sm font-bold text-white truncate">{item.comName}</p>
        <p className="text-xs text-slate-500 truncate">
          {[item.language, item.countryCode].filter(Boolean).join(' · ') || '—'}{` · #${item.specCode}`}
        </p>
      </div>
      <div className="flex gap-2 shrink-0">
        <button onClick={() => act('approve')} disabled={!!busy}
          className="flex items-center justify-center w-11 h-11 rounded-lg bg-emerald-500/15 text-emerald-400 border border-emerald-500/30 hover:bg-emerald-500/25 disabled:opacity-50"
          title={t('adminCommunity.approve')}>
          {busy === 'approve' ? <Loader2 className="w-4 h-4 animate-spin" /> : <Check className="w-4 h-4" />}
        </button>
        <button onClick={() => act('reject')} disabled={!!busy}
          className="flex items-center justify-center w-11 h-11 rounded-lg bg-red-500/10 text-red-400 border border-red-500/30 hover:bg-red-500/20 disabled:opacity-50"
          title={t('adminCommunity.reject')}>
          {busy === 'reject' ? <Loader2 className="w-4 h-4 animate-spin" /> : <X className="w-4 h-4" />}
        </button>
      </div>
    </div>
  );
}

export default function AdminCommunityPage() {
  const { t } = useTranslation();
  const [species, setSpecies] = useState<CommunitySpeciesDto[]>([]);
  const [names, setNames] = useState<CommunityCommonNameDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [selected, setSelected] = useState<Set<number>>(new Set());
  const [bulkBusy, setBulkBusy] = useState(false);

  const load = () => {
    setLoading(true);
    Promise.all([
      getPendingCommunitySpecies().catch(() => [] as CommunitySpeciesDto[]),
      getPendingCommonNames().catch(() => [] as CommunityCommonNameDto[]),
    ])
      .then(([sp, nm]) => { setSpecies(sp); setNames(nm); setSelected(new Set()); })
      .finally(() => setLoading(false));
  };
  useEffect(load, []);

  const toggle = (id: number) =>
    setSelected((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });

  const toggleAll = () =>
    setSelected((prev) => (prev.size === names.length ? new Set() : new Set(names.map((n) => n.autoCtr))));

  const approveSelected = async () => {
    if (selected.size === 0) return;
    setBulkBusy(true);
    try {
      await verifyCommonNamesBatch([...selected]);
      load();
    } catch {
      setBulkBusy(false);
    }
  };

  return (
    <div className="max-w-4xl mx-auto w-full px-4 py-8 flex flex-col gap-8">
      <div>
        <h1 className="text-2xl font-bold text-white flex items-center gap-2">
          <ShieldCheck className="w-6 h-6 text-sky-400" />
          {t('adminCommunity.title')}
        </h1>
        <p className="text-slate-400 text-sm mt-1">{t('adminCommunity.subtitle')}</p>
      </div>

      {loading ? (
        <div className="flex items-center justify-center py-16 text-slate-600">
          <Loader2 className="w-6 h-6 animate-spin" />
        </div>
      ) : (
        <>
          {/* Pending species */}
          <section className="space-y-3">
            <h2 className="text-sm font-bold text-slate-300 uppercase tracking-wider flex items-center gap-2">
              <Fish className="w-4 h-4 text-sky-400" /> {t('adminCommunity.speciesSection')} ({species.length})
            </h2>
            {species.length === 0 ? (
              <p className="text-sm text-slate-500 py-4 text-center bg-[#202226] rounded-xl border border-dashed border-slate-700/50">
                {t('adminCommunity.empty')}
              </p>
            ) : (
              species.map((s) => <SpeciesRow key={s.specCode} item={s} t={t} onDone={load} />)
            )}
          </section>

          {/* Pending common names — with bulk approve */}
          <section className="space-y-3">
            <div className="flex items-center justify-between gap-3 flex-wrap">
              <h2 className="text-sm font-bold text-slate-300 uppercase tracking-wider flex items-center gap-2">
                <Languages className="w-4 h-4 text-sky-400" /> {t('adminCommunity.namesSection')} ({names.length})
              </h2>
              {names.length > 0 && (
                <div className="flex items-center gap-2">
                  <button onClick={toggleAll}
                    className="px-3 py-2 rounded-lg border border-slate-700 text-slate-300 text-xs font-bold hover:bg-white/5 min-h-[44px]">
                    {t('adminCommunity.selectAll')}
                  </button>
                  <button onClick={approveSelected} disabled={selected.size === 0 || bulkBusy}
                    className="flex items-center gap-1.5 px-3 py-2 rounded-lg bg-sky-500 text-white text-xs font-bold hover:bg-sky-400 disabled:opacity-40 min-h-[44px]">
                    {bulkBusy ? <Loader2 className="w-4 h-4 animate-spin" /> : <Check className="w-4 h-4" />}
                    {t('adminCommunity.approveSelected', { count: selected.size })}
                  </button>
                </div>
              )}
            </div>
            {names.length === 0 ? (
              <p className="text-sm text-slate-500 py-4 text-center bg-[#202226] rounded-xl border border-dashed border-slate-700/50">
                {t('adminCommunity.empty')}
              </p>
            ) : (
              names.map((n) => (
                <NameRow key={n.autoCtr} item={n} t={t} checked={selected.has(n.autoCtr)}
                  onToggle={() => toggle(n.autoCtr)} onDone={load} />
              ))
            )}
          </section>
        </>
      )}
    </div>
  );
}

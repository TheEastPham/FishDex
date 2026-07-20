import { useEffect, useState } from 'react';
import { getMyCommunitySpecies, getMyCommonNames, useTranslation } from '@fishlover/shared';
import type { CommunitySpeciesDto, CommunityCommonNameDto } from '@fishlover/shared';
import { Loader2, Fish, Languages, Sparkles } from 'lucide-react';

type T = ReturnType<typeof useTranslation>['t'];

function StatusBadge({ isVerified, rejectionReason, t }: { isVerified: boolean; rejectionReason: string | null; t: T }) {
  const [label, cls] = isVerified
    ? [t('myContributions.statusVerified'), 'bg-emerald-500/15 text-emerald-400 border-emerald-500/30']
    : rejectionReason
      ? [t('myContributions.statusRejected'), 'bg-red-500/10 text-red-400 border-red-500/30']
      : [t('myContributions.statusPending'), 'bg-amber-500/10 text-amber-300 border-amber-500/30'];
  return <span className={`shrink-0 px-2.5 py-1 rounded-full border text-xs font-bold ${cls}`}>{label}</span>;
}

export default function MyContributionsPage() {
  const { t } = useTranslation();
  const [species, setSpecies] = useState<CommunitySpeciesDto[]>([]);
  const [names, setNames] = useState<CommunityCommonNameDto[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    setLoading(true);
    Promise.all([
      getMyCommunitySpecies().catch(() => [] as CommunitySpeciesDto[]),
      getMyCommonNames().catch(() => [] as CommunityCommonNameDto[]),
    ])
      .then(([sp, nm]) => { setSpecies(sp); setNames(nm); })
      .finally(() => setLoading(false));
  }, []);

  const isEmpty = species.length === 0 && names.length === 0;

  return (
    <div className="max-w-3xl mx-auto w-full px-4 py-8 flex flex-col gap-8">
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
          {species.length > 0 && (
            <section className="space-y-3">
              <h2 className="text-sm font-bold text-slate-300 uppercase tracking-wider flex items-center gap-2">
                <Fish className="w-4 h-4 text-sky-400" /> {t('myContributions.speciesSection')} ({species.length})
              </h2>
              {species.map((s) => (
                <div key={s.specCode} className="flex items-center gap-3 p-4 rounded-xl bg-[#0F172A] border border-slate-800">
                  <div className="min-w-0 flex-1">
                    <p className="text-sm font-bold text-white truncate">{s.commonName || s.speciesName}</p>
                    <p className="text-xs text-slate-500 truncate">{s.speciesName}{s.rejectionReason ? ` · ${s.rejectionReason}` : ''}</p>
                  </div>
                  <StatusBadge isVerified={s.isVerified} rejectionReason={s.rejectionReason} t={t} />
                </div>
              ))}
            </section>
          )}

          {names.length > 0 && (
            <section className="space-y-3">
              <h2 className="text-sm font-bold text-slate-300 uppercase tracking-wider flex items-center gap-2">
                <Languages className="w-4 h-4 text-sky-400" /> {t('myContributions.namesSection')} ({names.length})
              </h2>
              {names.map((n) => (
                <div key={n.autoCtr} className="flex items-center gap-3 p-4 rounded-xl bg-[#0F172A] border border-slate-800">
                  <div className="min-w-0 flex-1">
                    <p className="text-sm font-bold text-white truncate">{n.comName}</p>
                    <p className="text-xs text-slate-500 truncate">
                      {[n.language, n.countryCode].filter(Boolean).join(' · ') || '—'}{` · #${n.specCode}`}
                      {n.rejectionReason ? ` · ${n.rejectionReason}` : ''}
                    </p>
                  </div>
                  <StatusBadge isVerified={n.isVerified} rejectionReason={n.rejectionReason} t={t} />
                </div>
              ))}
            </section>
          )}
        </>
      )}
    </div>
  );
}

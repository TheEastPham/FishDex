import { useNavigate } from 'react-router-dom';
import { useMyAquariums, useMyFavorites, useSpeciesSummaries, cn, useTranslation, WaterType, AquariumStyle } from '@fishlover/shared';
import type { AquariumDto, SpeciesSummary } from '@fishlover/shared';
import {
  Droplets, Heart, Fish, ArrowRight, Layers,
  FlaskConical, Sparkles
} from 'lucide-react';


const WATER_TYPE_STYLES: Record<number, { bg: string; text: string; border: string }> = {
  [WaterType.Freshwater]: { bg: 'bg-emerald-500/10', text: 'text-emerald-400', border: 'border-emerald-500/20' },
  [WaterType.Saltwater]:  { bg: 'bg-sky-500/10',     text: 'text-sky-400',     border: 'border-sky-500/20'     },
  [WaterType.Brackish]:   { bg: 'bg-teal-500/10',    text: 'text-teal-400',    border: 'border-teal-500/20'    },
};
const DEFAULT_WATER_STYLE = { bg: 'bg-slate-700/20', text: 'text-slate-400', border: 'border-slate-700/30' };

const STYLE_I18N_KEY: Record<number, string> = {
  [AquariumStyle.Nature]:     'st_nature',
  [AquariumStyle.Dutch]:      'st_dutch',
  [AquariumStyle.Iwagumi]:    'st_iwagumi',
  [AquariumStyle.Biotope]:    'st_biotope',
  [AquariumStyle.Reef]:       'st_reef',
  [AquariumStyle.Blackwater]: 'st_blackwater',
  [AquariumStyle.Community]:  'st_community',
  [AquariumStyle.Predator]:   'st_predator',
  [AquariumStyle.Paludarium]: 'st_paludarium',
};

function getWaterLabel(waterType: WaterType | null, t: (k: string) => string): string | null {
  switch (waterType) {
    case WaterType.Freshwater: return t('tanks.wt_freshwater').replace(/\s*[🐠🐡🦐]/gu, '').trim();
    case WaterType.Saltwater:  return t('tanks.wt_saltwater').replace(/\s*[🐠🐡🦐]/gu, '').trim();
    case WaterType.Brackish:   return t('tanks.wt_brackish').replace(/\s*[🐠🐡🦐]/gu, '').trim();
    default: return null;
  }
}

function StatCard({ icon, label, value, sub, color, loading }: {
  icon: React.ReactNode; label: string; value: string | number; sub?: string; color: string; loading?: boolean;
}) {
  return (
    <div className="bg-[#1E293B] rounded-xl border border-slate-800/60 p-2.5 sm:p-4 flex flex-col items-center sm:items-start gap-1 sm:gap-3 hover:bg-[#263348] transition-colors group text-center sm:text-left">
      <div className={cn('p-1.5 sm:p-2.5 rounded-lg sm:rounded-xl shrink-0 transition-transform group-hover:scale-110', color)}>
        {icon}
      </div>
      <div className="min-w-0 w-full">
        <p className="text-[9px] sm:text-[11px] text-slate-500 font-semibold uppercase tracking-widest truncate">{label}</p>
        {loading
          ? <div className="h-5 sm:h-7 w-10 sm:w-16 bg-slate-800 rounded-lg animate-pulse mt-1 mx-auto sm:mx-0" />
          : <p className="text-lg sm:text-2xl font-black text-white leading-tight">{value}</p>
        }
        {sub && <p className="text-[9px] sm:text-xs text-slate-500">{sub}</p>}
      </div>
    </div>
  );
}

function MarqueeText({ text, className }: { text: string; className?: string }) {
  return (
    <div className={cn('overflow-hidden relative', className)}>
      <p
        className="font-bold text-white text-sm whitespace-nowrap animate-marquee hover:[animation-play-state:paused]"
        style={{ display: 'inline-block' }}
        title={text}
      >
        {text}&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;{text}
      </p>
    </div>
  );
}

function AquariumPreviewCard({ tank, onClick, compact }: { tank: AquariumDto; onClick: () => void; compact?: boolean }) {
  const { t } = useTranslation();
  const waterStyle = (tank.waterType != null ? WATER_TYPE_STYLES[tank.waterType] : null) ?? DEFAULT_WATER_STYLE;
  const waterLabel = getWaterLabel(tank.waterType, t);
  const styleKey = tank.style != null ? STYLE_I18N_KEY[tank.style] : null;
  const styleLabel = styleKey ? t(`tanks.${styleKey}`).split(' — ')[0] : null;
  const speciesCount = tank.fishCount;
  const totalQuantity = tank.totalQuantity;

  const fishLabel = t('dashboard.speciesCount', { species: speciesCount, count: totalQuantity });

  if (compact) {
    return (
      <div
        onClick={onClick}
        className="bg-[#1E293B] border border-slate-800/60 rounded-2xl p-3 cursor-pointer hover:bg-[#263348] transition-all duration-200 group aspect-[4/3] flex flex-col justify-between overflow-hidden"
      >
        <div className="flex flex-wrap gap-1">
          {waterLabel && (
            <span className={cn('text-[9px] font-bold uppercase tracking-wider px-1.5 py-0.5 rounded-full border', waterStyle.bg, waterStyle.text, waterStyle.border)}>
              {waterLabel}
            </span>
          )}
          {styleLabel && (
            <span className="text-[9px] font-medium px-1.5 py-0.5 rounded-full bg-slate-700/30 text-slate-400 border border-slate-700/30">
              {styleLabel}
            </span>
          )}
        </div>
        <MarqueeText text={tank.name} className="my-1" />
        <div className="flex items-center gap-2 text-[10px] text-slate-400 pt-2 border-t border-slate-800/50">
          {tank.volumeLiters != null && (
            <span className="flex items-center gap-0.5">
              <FlaskConical className="w-3 h-3" />{tank.volumeLiters.toFixed(0)}L
            </span>
          )}
          <span className="flex items-center gap-0.5 font-semibold text-slate-300">
            <Fish className="w-3 h-3" />{fishLabel}
          </span>
        </div>
      </div>
    );
  }

  return (
    <div
      onClick={onClick}
      className="bg-[#1E293B] border border-slate-800/60 rounded-2xl p-5 cursor-pointer hover:bg-[#263348] hover:-translate-y-1 hover:shadow-xl hover:border-slate-700/60 transition-all duration-300 group"
    >
      <div className="flex items-center gap-2 flex-wrap mb-3">
        {waterLabel && (
          <span className={cn('text-[11px] font-bold uppercase tracking-wider px-2.5 py-1 rounded-full border', waterStyle.bg, waterStyle.text, waterStyle.border)}>
            {waterLabel}
          </span>
        )}
        {styleLabel && (
          <span className="text-[11px] font-medium px-2.5 py-1 rounded-full bg-slate-700/30 text-slate-400 border border-slate-700/30">
            {styleLabel}
          </span>
        )}
      </div>
      <h3 className="font-bold text-white text-base mb-3 truncate">{tank.name}</h3>
      <div className="flex items-center justify-between pt-3 border-t border-slate-800/50 text-sm text-slate-400">
        <div className="flex items-center gap-3">
          {tank.volumeLiters != null && (
            <span className="flex items-center gap-1">
              <FlaskConical className="w-3.5 h-3.5" />{tank.volumeLiters.toFixed(0)}L
            </span>
          )}
          <span className="flex items-center gap-1 font-semibold text-slate-300">
            <Fish className="w-3.5 h-3.5" />{fishLabel}
          </span>
        </div>
        <ArrowRight className="w-4 h-4 text-slate-600 group-hover:text-slate-400 group-hover:translate-x-1 transition-all" />
      </div>
    </div>
  );
}

function FavoritePreview({ summary, onClick }: { summary: SpeciesSummary; onClick: () => void }) {
  return (
    <div
      onClick={onClick}
      className="relative rounded-xl overflow-hidden aspect-square cursor-pointer group border border-slate-800/50 bg-[#172033]"
    >
      {summary.imageUrl ? (
        <img src={summary.imageUrl} alt={summary.commonName || summary.speciesName} className="w-full h-full object-cover group-hover:scale-110 transition-transform duration-500" />
      ) : (
        <div className="w-full h-full flex items-center justify-center">
          <Fish className="w-8 h-8 text-slate-700" />
        </div>
      )}
      <div className="absolute inset-0 bg-gradient-to-t from-black/80 via-transparent to-transparent opacity-0 group-hover:opacity-100 transition-opacity flex items-end p-2">
        <p className="text-white text-xs font-bold truncate">{summary.commonName || summary.speciesName}</p>
      </div>
    </div>
  );
}

export default function DashboardPage() {
  const navigate = useNavigate();
  const { t, i18n } = useTranslation();

  const { aquariums, loading: tanksLoading } = useMyAquariums();
  const { favorites, loading: favsLoading } = useMyFavorites();

  const previewCodes = favorites.slice(0, 6).map((f) => f.specCode);
  const { summaries, loading: summariesLoading } = useSpeciesSummaries(previewCodes, i18n.language);

  const totalVolume = aquariums.reduce((sum, t) => sum + (t.volumeLiters ?? 0), 0);
  const totalSpecies = aquariums.reduce((sum, t) => sum + t.fishCount, 0);

  return (
    <div className="min-h-screen bg-[#0F172A] p-4 sm:p-6 pb-20 font-sans">
      <div className="max-w-6xl mx-auto space-y-6 sm:space-y-8">

        {/* ── Header ── */}
        <div>
          <div className="flex items-center gap-2 mb-1">
            <Sparkles className="w-5 h-5 text-amber-400" />
            <span className="text-amber-400 text-sm font-bold uppercase tracking-widest">AquaHome</span>
          </div>
          <h1 className="text-2xl sm:text-3xl font-black text-white">{t('dashboard.title')}</h1>
          <p className="text-slate-400 mt-1 text-sm sm:text-base">{t('dashboard.subtitle')}</p>
        </div>

        {/* ── Stats — 4 columns, always 1 row ── */}
        <div className="grid grid-cols-4 gap-2 sm:gap-4">
          <StatCard
            icon={<Layers className="w-4 h-4 sm:w-5 sm:h-5 text-sky-400" />}
            label={t('dashboard.statTanks')} value={aquariums.length} sub={t('dashboard.statTankSub')}
            color="bg-sky-500/10" loading={tanksLoading} />
          <StatCard
            icon={<FlaskConical className="w-4 h-4 sm:w-5 sm:h-5 text-emerald-400" />}
            label={t('dashboard.statVolume')} value={`${totalVolume.toFixed(0)}L`} sub={t('dashboard.statVolumeSub')}
            color="bg-emerald-500/10" loading={tanksLoading} />
          <StatCard
            icon={<Fish className="w-4 h-4 sm:w-5 sm:h-5 text-cyan-400" />}
            label={t('dashboard.statSpecies')} value={totalSpecies} sub={t('dashboard.statSpeciesSub')}
            color="bg-cyan-500/10" loading={tanksLoading} />
          <StatCard
            icon={<Heart className="w-4 h-4 sm:w-5 sm:h-5 text-rose-400" />}
            label={t('dashboard.statFavorites')} value={favorites.length} sub={t('dashboard.statFavoritesSub')}
            color="bg-rose-500/10" loading={favsLoading} />
        </div>

        {/* ── My Aquariums preview ── */}
        <div>
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-lg font-bold text-white flex items-center gap-2">
              <Droplets className="w-5 h-5 text-sky-400" /> {t('dashboard.myTanks')}
            </h2>
            <button onClick={() => navigate('/tanks')} className="text-sm text-sky-400 hover:text-sky-300 font-semibold flex items-center gap-1 transition-colors">
              {t('dashboard.viewAll')} <ArrowRight className="w-4 h-4" />
            </button>
          </div>

          {tanksLoading ? (
            <>
              <div className="grid grid-cols-2 gap-3 sm:hidden">
                {[1, 2].map(i => <div key={i} className="aspect-[4/3] bg-[#1E293B] border border-slate-800/60 rounded-2xl animate-pulse" />)}
              </div>
              <div className="hidden sm:grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
                {[1, 2, 3].map(i => <div key={i} className="bg-[#1E293B] border border-slate-800/60 rounded-2xl h-44 animate-pulse" />)}
              </div>
            </>
          ) : aquariums.length === 0 ? (
            <div className="bg-[#1E293B] border border-dashed border-slate-700/60 rounded-2xl p-10 text-center">
              <Droplets className="w-12 h-12 text-slate-700 mx-auto mb-3" />
              <p className="text-slate-400 font-medium mb-4">{t('dashboard.noTanks')}</p>
              <button onClick={() => navigate('/tanks')} className="inline-flex items-center gap-2 bg-sky-500 hover:bg-sky-400 text-white font-bold px-4 py-2 rounded-xl transition-colors text-sm">
                {t('dashboard.createFirst')}
              </button>
            </div>
          ) : (
            <>
              <div className="grid grid-cols-2 gap-3 sm:hidden">
                {aquariums.slice(0, 2).map(tank => (
                  <AquariumPreviewCard key={tank.id} tank={tank} onClick={() => navigate('/tanks')} compact />
                ))}
              </div>
              <div className="hidden sm:grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
                {aquariums.slice(0, 3).map(tank => (
                  <AquariumPreviewCard key={tank.id} tank={tank} onClick={() => navigate('/tanks')} />
                ))}
              </div>
            </>
          )}
        </div>

        {/* ── Favorites preview ── */}
        <div>
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-lg font-bold text-white flex items-center gap-2">
              <Heart className="w-5 h-5 text-rose-400" /> {t('dashboard.myFavorites')}
            </h2>
            <button onClick={() => navigate('/favorites')} className="text-sm text-rose-400 hover:text-rose-300 font-semibold flex items-center gap-1 transition-colors">
              {t('dashboard.viewAll')} <ArrowRight className="w-4 h-4" />
            </button>
          </div>

          {favsLoading || summariesLoading ? (
            <div className="grid grid-cols-3 sm:grid-cols-6 gap-3">
              {[1, 2, 3, 4, 5, 6].map(i => <div key={i} className="aspect-square rounded-xl bg-[#1E293B] animate-pulse" />)}
            </div>
          ) : Object.keys(summaries).length > 0 ? (
            <div className="grid grid-cols-3 sm:grid-cols-6 gap-3">
              {Object.values(summaries).map(s => (
                <FavoritePreview key={s.specCode} summary={s} onClick={() => navigate(`/fish/${s.specCode}`)} />
              ))}
            </div>
          ) : (
            <div className="bg-[#1E293B] border border-dashed border-slate-700/60 rounded-2xl p-8 text-center">
              <Heart className="w-10 h-10 text-slate-700 mx-auto mb-3" />
              <p className="text-slate-500 text-sm">{t('dashboard.noFavorites')}</p>
            </div>
          )}
        </div>

      </div>
    </div>
  );
}

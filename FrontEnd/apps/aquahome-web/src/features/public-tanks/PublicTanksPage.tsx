import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { getPublicSnapshots, WaterType, AquariumStyle, useTranslation, cn } from '@fishlover/shared';
import type { AquariumSnapshotDto, GetPublicSnapshotsParams } from '@fishlover/shared';
import { Heart, Fish, Trophy, Loader2, Waves, ChevronLeft, ChevronRight } from 'lucide-react';
import { displayNameFromSlug, waterTypeLabel, STYLE_LABELS, awardBadgeStyle } from './labels';

const PAGE_SIZE = 12;

const WATER_GRADIENTS: Record<number, string> = {
  [WaterType.Freshwater]: 'from-emerald-950 to-teal-900',
  [WaterType.Saltwater]:  'from-sky-950 to-indigo-900',
  [WaterType.Brackish]:   'from-teal-950 to-slate-900',
};

function SnapshotCard({ snapshot, onClick, t }: {
  snapshot: AquariumSnapshotDto;
  onClick: () => void;
  t: ReturnType<typeof useTranslation>['t'];
}) {
  const wtLabel = waterTypeLabel(t, snapshot.waterType);
  const styleLabel = STYLE_LABELS[snapshot.style] ?? null;
  const awardName = snapshot.awardTierName;
  const awardStyle = awardBadgeStyle(snapshot.awardTierLevel);
  const gradient = WATER_GRADIENTS[snapshot.waterType] ?? 'from-slate-900 to-slate-800';

  return (
    <button
      onClick={onClick}
      className="text-left rounded-2xl overflow-hidden border border-slate-800/60 bg-[#1E293B] hover:border-sky-500/40 transition-colors group"
    >
      {/* Cover */}
      <div className={cn('relative h-36 bg-gradient-to-br', gradient)}>
        {snapshot.coverImageUrl
          ? <img src={snapshot.coverImageUrl} alt="" className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300" />
          : <div className="w-full h-full flex items-center justify-center"><Waves className="w-10 h-10 text-white/20" /></div>
        }
        {awardName && (
          <span className={cn('absolute top-2 left-2 flex items-center gap-1 text-[10px] font-bold px-2 py-0.5 rounded-full', awardStyle)}>
            <Trophy className="w-3 h-3" /> {awardName}
          </span>
        )}
      </div>

      {/* Info */}
      <div className="p-3">
        <p className="text-sm font-bold text-white truncate">{displayNameFromSlug(snapshot.slug)}</p>
        <div className="flex items-center gap-1.5 mt-1.5 flex-wrap">
          {wtLabel && (
            <span className="text-[10px] font-semibold px-1.5 py-0.5 rounded-full bg-sky-500/10 text-sky-400 border border-sky-500/20">
              {wtLabel}
            </span>
          )}
          {styleLabel && (
            <span className="text-[10px] font-semibold px-1.5 py-0.5 rounded-full bg-white/5 text-slate-400 border border-white/10">
              {styleLabel}
            </span>
          )}
        </div>
        <div className="flex items-center gap-3 mt-2 text-xs text-slate-500">
          <span className="flex items-center gap-1">
            <Heart className={cn('w-3.5 h-3.5', snapshot.likedByMe && 'fill-red-400 text-red-400')} />
            {snapshot.likeCount}
          </span>
          <span className="flex items-center gap-1">
            <Fish className="w-3.5 h-3.5" />
            {snapshot.fishSpeciesCount} {t('aquarium.speciesUnit')}
          </span>
        </div>
      </div>
    </button>
  );
}

export default function PublicTanksPage() {
  const navigate = useNavigate();
  const { t } = useTranslation();

  const [snapshots, setSnapshots] = useState<AquariumSnapshotDto[]>([]);
  const [totalPages, setTotalPages] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(false);

  const [waterType, setWaterType] = useState<WaterType | undefined>();
  const [style, setStyle] = useState<AquariumStyle | undefined>();
  const [contest, setContest] = useState<'any' | 'winners' | undefined>();
  const [sort, setSort] = useState<'likes' | 'newest'>('likes');
  const [page, setPage] = useState(1);

  useEffect(() => {
    setLoading(true);
    setError(false);
    const params: GetPublicSnapshotsParams = { waterType, style, contest, sort, page, pageSize: PAGE_SIZE };
    getPublicSnapshots(params)
      .then(result => {
        setSnapshots(result.items);
        setTotalPages(result.totalPages);
      })
      .catch(() => setError(true))
      .finally(() => setLoading(false));
  }, [waterType, style, contest, sort, page]);

  const resetPage = () => setPage(1);

  // Select style ≥16px font để tránh Safari auto-zoom trên mobile
  const selectCls = 'bg-[#1E293B] border border-slate-700 rounded-lg px-2.5 py-2 text-base sm:text-sm text-white min-h-[44px] sm:min-h-0';

  return (
    <div className="px-4 sm:px-6 pt-5 pb-10">
      <div className="mb-6">
        <h1 className="text-xl sm:text-2xl font-black text-white">{t('publicTanks.title')}</h1>
        <p className="text-sm text-slate-500 mt-1">{t('publicTanks.subtitle')}</p>
      </div>

      {/* Filter bar */}
      <div className="flex flex-wrap items-center gap-2 mb-6">
        <select
          value={waterType ?? ''}
          onChange={e => { setWaterType(e.target.value ? Number(e.target.value) as WaterType : undefined); resetPage(); }}
          className={selectCls}
        >
          <option value="">{t('publicTanks.allWaterTypes')}</option>
          <option value={WaterType.Freshwater}>{t('tanks.wt_freshwater')}</option>
          <option value={WaterType.Saltwater}>{t('tanks.wt_saltwater')}</option>
          <option value={WaterType.Brackish}>{t('tanks.wt_brackish')}</option>
        </select>

        <select
          value={style ?? ''}
          onChange={e => { setStyle(e.target.value ? Number(e.target.value) as AquariumStyle : undefined); resetPage(); }}
          className={selectCls}
        >
          <option value="">{t('publicTanks.allStyles')}</option>
          {Object.entries(STYLE_LABELS).map(([value, label]) => (
            <option key={value} value={value}>{label}</option>
          ))}
        </select>

        <select
          value={contest ?? ''}
          onChange={e => { setContest((e.target.value || undefined) as 'any' | 'winners' | undefined); resetPage(); }}
          className={selectCls}
        >
          <option value="">{t('publicTanks.allTanks')}</option>
          <option value="any">{t('publicTanks.contestAny')}</option>
          <option value="winners">{t('publicTanks.contestWinners')}</option>
        </select>

        <select
          value={sort}
          onChange={e => { setSort(e.target.value as 'likes' | 'newest'); resetPage(); }}
          className={selectCls}
        >
          <option value="likes">{t('publicTanks.sortLikes')}</option>
          <option value="newest">{t('publicTanks.sortNewest')}</option>
        </select>
      </div>

      {/* Grid */}
      {loading && (
        <div className="flex items-center justify-center py-20 text-slate-600">
          <Loader2 className="w-6 h-6 animate-spin" />
        </div>
      )}

      {!loading && error && (
        <div className="rounded-xl border border-red-500/30 bg-red-500/10 p-4 text-sm text-red-400">
          {t('publicTanks.loadError')}
        </div>
      )}

      {!loading && !error && snapshots.length === 0 && (
        <div className="flex flex-col items-center justify-center py-20 text-center">
          <Waves className="w-12 h-12 text-slate-700 mb-3" />
          <p className="text-slate-500 text-sm">{t('publicTanks.empty')}</p>
        </div>
      )}

      {!loading && !error && snapshots.length > 0 && (
        <>
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
            {snapshots.map(s => (
              <SnapshotCard
                key={s.id}
                snapshot={s}
                onClick={() => navigate(`/public/tanks/${s.slug}`)}
                t={t}
              />
            ))}
          </div>

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="flex items-center justify-center gap-3 mt-8">
              <button
                onClick={() => setPage(p => Math.max(1, p - 1))}
                disabled={page <= 1}
                className="p-2.5 rounded-lg bg-[#1E293B] border border-slate-700 text-slate-400 disabled:opacity-40 hover:border-sky-500/40 transition-colors min-w-[44px] min-h-[44px] flex items-center justify-center"
              >
                <ChevronLeft className="w-4 h-4" />
              </button>
              <span className="text-sm text-slate-400">
                {page} / {totalPages}
              </span>
              <button
                onClick={() => setPage(p => Math.min(totalPages, p + 1))}
                disabled={page >= totalPages}
                className="p-2.5 rounded-lg bg-[#1E293B] border border-slate-700 text-slate-400 disabled:opacity-40 hover:border-sky-500/40 transition-colors min-w-[44px] min-h-[44px] flex items-center justify-center"
              >
                <ChevronRight className="w-4 h-4" />
              </button>
            </div>
          )}
        </>
      )}
    </div>
  );
}

import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  getPublicSnapshotBySlug, likeSnapshot, unlikeSnapshot,
  useAuthStore, useTranslation, cn, WaterType,
} from '@fishlover/shared';
import type { AquariumSnapshotDto } from '@fishlover/shared';
import {
  Heart, Fish, Layers, FlaskConical, Ruler, Calendar, Trophy,
  Loader2, ArrowLeft, Youtube, Droplets,
} from 'lucide-react';
import SnapshotFishSection from './components/SnapshotFishSection';
import { displayNameFromSlug, waterTypeLabel, STYLE_LABELS, awardBadgeStyle } from './labels';

const TANK_HERO: Record<number, { from: string; via: string; to: string; accent: string }> = {
  [WaterType.Freshwater]: { from: 'from-emerald-950', via: 'via-emerald-900/80', to: 'to-teal-950',   accent: 'text-emerald-400' },
  [WaterType.Saltwater]:  { from: 'from-sky-950',     via: 'via-blue-900/80',    to: 'to-indigo-950', accent: 'text-sky-400'     },
  [WaterType.Brackish]:   { from: 'from-teal-950',    via: 'via-cyan-900/80',    to: 'to-slate-950',  accent: 'text-teal-400'    },
};

const DEFAULT_HERO = { from: 'from-slate-900', via: 'via-slate-800/80', to: 'to-slate-950', accent: 'text-slate-400' };

function formatDate(iso: string, locale: string) {
  return new Date(iso).toLocaleDateString(locale, { day: '2-digit', month: '2-digit', year: 'numeric' });
}

interface StatItemProps { icon: React.ReactNode; label: string; value: string }
function StatItem({ icon, label, value }: StatItemProps) {
  return (
    <div className="bg-[#1E293B] rounded-xl p-2.5 sm:p-4 border border-slate-800/60 flex flex-col gap-0.5 sm:gap-1">
      <div className="flex items-center gap-1 text-slate-500 text-[10px] sm:text-xs font-medium">
        {icon}
        <span className="truncate">{label}</span>
      </div>
      <p className="text-white font-bold text-xs sm:text-sm leading-tight">{value}</p>
    </div>
  );
}

export default function PublicTankDetailPage() {
  const { slug } = useParams<{ slug: string }>();
  const navigate = useNavigate();
  const { t } = useTranslation();
  const isAuthenticated = useAuthStore(s => s.isAuthenticated);

  const [snapshot, setSnapshot] = useState<AquariumSnapshotDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [notFound, setNotFound] = useState(false);
  const [likeBusy, setLikeBusy] = useState(false);

  useEffect(() => {
    if (!slug) return;
    setLoading(true);
    getPublicSnapshotBySlug(slug)
      .then(setSnapshot)
      .catch(() => setNotFound(true))
      .finally(() => setLoading(false));
  }, [slug]);

  const toggleLike = async () => {
    if (!snapshot || likeBusy) return;
    if (!isAuthenticated) {
      navigate('/login');
      return;
    }
    setLikeBusy(true);
    try {
      if (snapshot.likedByMe) {
        await unlikeSnapshot(snapshot.id);
        setSnapshot({ ...snapshot, likedByMe: false, likeCount: Math.max(0, snapshot.likeCount - 1) });
      } else {
        await likeSnapshot(snapshot.id);
        setSnapshot({ ...snapshot, likedByMe: true, likeCount: snapshot.likeCount + 1 });
      }
    } catch (err) {
      console.error(err);
    } finally {
      setLikeBusy(false);
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center py-24 text-slate-600">
        <Loader2 className="w-6 h-6 animate-spin" />
      </div>
    );
  }

  if (notFound || !snapshot) {
    return (
      <div className="px-6 pt-10 text-center">
        <p className="text-slate-500">{t('publicTanks.notFound')}</p>
        <button
          onClick={() => navigate('/public/tanks')}
          className="mt-4 text-sky-400 text-sm hover:underline min-h-[44px]"
        >
          ← {t('publicTanks.backToGallery')}
        </button>
      </div>
    );
  }

  const data = snapshot.snapshotData;
  const hero = TANK_HERO[snapshot.waterType] ?? DEFAULT_HERO;
  const wtLabel = waterTypeLabel(t, snapshot.waterType);
  const styleLabel = STYLE_LABELS[snapshot.style] ?? null;
  const awardName = snapshot.awardTierName;
  const awardStyle = awardBadgeStyle(snapshot.awardTierLevel);
  const name = data?.aquariumName ?? displayNameFromSlug(snapshot.slug);

  const volumeLabel = data?.volumeLiters != null ? `${data.volumeLiters.toFixed(1)} L` : '—';
  const dimLabel = (data?.lengthCm || data?.widthCm || data?.heightCm)
    ? `${data?.lengthCm ?? '?'} × ${data?.widthCm ?? '?'} × ${data?.heightCm ?? '?'} cm`
    : '—';
  const totalFish = data?.fish.reduce((s, f) => s + f.quantity, 0) ?? 0;

  return (
    <div className="px-4 sm:px-6 pt-5 pb-10">
      {/* Back */}
      <button
        onClick={() => navigate('/public/tanks')}
        className="flex items-center gap-1.5 text-sm text-slate-500 hover:text-sky-400 transition-colors mb-4 min-h-[44px]"
      >
        <ArrowLeft className="w-4 h-4" /> {t('publicTanks.backToGallery')}
      </button>

      {/* Hero */}
      <div className={cn('relative h-56 sm:h-64 rounded-2xl overflow-hidden mb-6 bg-gradient-to-br', hero.from, hero.via, hero.to)}>
        {snapshot.coverImageUrl && (
          <img src={snapshot.coverImageUrl} alt="" className="absolute inset-0 w-full h-full object-cover opacity-50" />
        )}
        <div className="absolute inset-0 opacity-5">
          <Droplets className="w-64 h-64 absolute -bottom-12 -right-8 text-white" />
        </div>

        <div className="absolute inset-0 flex flex-col justify-between p-4 sm:p-5">
          <div className="flex items-start justify-between gap-2">
            <div className="flex items-center gap-2 flex-wrap">
              {wtLabel && (
                <span className={cn('text-[11px] font-bold uppercase tracking-wider px-2.5 py-1 rounded-full bg-white/10 border border-white/15', hero.accent)}>
                  {wtLabel}
                </span>
              )}
              {styleLabel && (
                <span className="text-[11px] font-bold uppercase tracking-wider px-2.5 py-1 rounded-full bg-white/10 border border-white/15 text-slate-300">
                  {styleLabel}
                </span>
              )}
              {awardName && (
                <span className={cn('flex items-center gap-1 text-[11px] font-bold uppercase tracking-wider px-2.5 py-1 rounded-full', awardStyle)}>
                  <Trophy className="w-3 h-3" /> {awardName}
                </span>
              )}
            </div>

            {/* Like button */}
            <button
              onClick={toggleLike}
              disabled={likeBusy}
              className={cn(
                'flex items-center gap-1.5 px-3 py-2 rounded-full border transition-colors min-h-[44px]',
                snapshot.likedByMe
                  ? 'bg-red-500/20 border-red-500/40 text-red-400'
                  : 'bg-white/10 border-white/15 text-white hover:bg-white/20',
              )}
              title={isAuthenticated ? undefined : t('publicTanks.loginToLike')}
            >
              <Heart className={cn('w-4 h-4', snapshot.likedByMe && 'fill-red-400')} />
              <span className="text-sm font-bold">{snapshot.likeCount}</span>
            </button>
          </div>

          <div>
            <h1 className="text-xl sm:text-2xl font-black text-white leading-tight">{name}</h1>
            {data?.ownerNickname && (
              <p className="text-white/70 text-sm mt-0.5 font-medium">{t('publicTanks.byOwner', { name: data.ownerNickname })}</p>
            )}
            {data?.description && (
              <p className="text-white/50 text-sm mt-1 line-clamp-2">{data.description}</p>
            )}
          </div>
        </div>
      </div>

      {/* Stats grid */}
      <div className="grid grid-cols-3 sm:grid-cols-5 gap-2 sm:gap-3 mb-8">
        <StatItem icon={<FlaskConical className="w-3.5 h-3.5" />} label={t('aquarium.volume')} value={volumeLabel} />
        <StatItem icon={<Ruler className="w-3.5 h-3.5" />} label={t('aquarium.dimensions')} value={dimLabel} />
        <StatItem icon={<Calendar className="w-3.5 h-3.5" />} label={t('publicTanks.publishedAt')} value={formatDate(snapshot.createdAt, t('aquarium.dateLocale'))} />
        <StatItem icon={<Fish className="w-3.5 h-3.5" />} label={t('aquarium.speciesCount')} value={`${snapshot.fishSpeciesCount} ${t('aquarium.speciesUnit')}`} />
        <StatItem icon={<Layers className="w-3.5 h-3.5" />} label={t('aquarium.fishCount')} value={`${totalFish} ${t('aquarium.fishUnit')}`} />
      </div>

      {/* Contest video */}
      {snapshot.youtubeVideoUrl && (
        <div className="mb-8">
          <h3 className="text-sm font-bold text-slate-400 uppercase tracking-wider mb-4 flex items-center gap-2">
            <Youtube className="w-4 h-4 text-red-500" /> {t('publicTanks.contestVideo')}
          </h3>
          <a
            href={snapshot.youtubeVideoUrl}
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex items-center gap-2 px-4 py-2.5 rounded-xl bg-red-500/10 border border-red-500/30 text-red-400 text-sm font-semibold hover:bg-red-500/20 transition-colors min-h-[44px]"
          >
            <Youtube className="w-4 h-4" /> {t('publicTanks.watchOnYoutube')}
          </a>
        </div>
      )}

      {/* Fish list + world map (từ SnapshotData denorm — không gọi API) */}
      {data && (
        <SnapshotFishSection
          fish={data.fish}
          onNavigateFish={specCode => navigate(`/fish/${specCode}`)}
        />
      )}
    </div>
  );
}

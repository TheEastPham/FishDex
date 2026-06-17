import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { getAquariumFish, getCached, setCached, CacheKeys, USER_DATA_TTL, cn, WaterType, useTranslation } from '@fishlover/shared';
import type { AquariumDto, AquariumFishDto } from '@fishlover/shared';
import { Pencil, Trash2, FlaskConical, Ruler, Calendar, Fish, Layers, Droplets } from 'lucide-react';
import FishInventorySection from './FishInventorySection';

const TANK_HERO: Record<number, { from: string; via: string; to: string; accent: string }> = {
  [WaterType.Freshwater]: { from: 'from-emerald-950', via: 'via-emerald-900/80', to: 'to-teal-950',   accent: 'text-emerald-400' },
  [WaterType.Saltwater]:  { from: 'from-sky-950',     via: 'via-blue-900/80',    to: 'to-indigo-950',  accent: 'text-sky-400'     },
  [WaterType.Brackish]:   { from: 'from-teal-950',    via: 'via-cyan-900/80',    to: 'to-slate-950',   accent: 'text-teal-400'    },
};

const DEFAULT_HERO = { from: 'from-slate-900', via: 'via-slate-800/80', to: 'to-slate-950', accent: 'text-slate-400' };

function getHeroStyle(waterType: WaterType | null) {
  if (waterType == null) return DEFAULT_HERO;
  return TANK_HERO[waterType] ?? DEFAULT_HERO;
}

const WATER_TYPE_LABELS: Record<number, string> = {
  [WaterType.Freshwater]: 'Nước ngọt',
  [WaterType.Saltwater]:  'Nước mặn',
  [WaterType.Brackish]:   'Lợ',
};

function formatDate(iso: string, locale: string) {
  return new Date(iso).toLocaleDateString(locale, { day: '2-digit', month: '2-digit', year: 'numeric' });
}

interface StatItemProps { icon: React.ReactNode; label: string; value: string; loading?: boolean }
function StatItem({ icon, label, value, loading }: StatItemProps) {
  return (
    <div className="bg-[#1E293B] rounded-xl p-4 border border-slate-800/60 flex flex-col gap-1">
      <div className="flex items-center gap-1.5 text-slate-500 text-xs font-medium">
        {icon}
        {label}
      </div>
      {loading
        ? <div className="h-5 w-16 bg-slate-700/50 rounded animate-pulse mt-0.5" />
        : <p className="text-white font-bold text-sm">{value}</p>
      }
    </div>
  );
}

interface Props {
  tank: AquariumDto;
  onEdit: (tank: AquariumDto) => void;
  onDelete: (id: string) => void;
}

export default function AquariumDetail({ tank, onEdit, onDelete }: Props) {
  const navigate = useNavigate();
  const { t } = useTranslation();
  const hero = getHeroStyle(tank.waterType);
  const typeLabel = tank.waterType != null ? (WATER_TYPE_LABELS[tank.waterType] ?? null) : null;

  const [fishList, setFishList]     = useState<AquariumFishDto[]>([]);
  const [fishLoading, setFishLoading] = useState(true);

  useEffect(() => {
    setFishLoading(true);
    const cached = getCached<AquariumFishDto[]>(CacheKeys.aquariumFish(tank.id));
    if (cached) { setFishList(cached); setFishLoading(false); return; }
    getAquariumFish(tank.id)
      .then(data => {
        setCached(CacheKeys.aquariumFish(tank.id), data, USER_DATA_TTL);
        setFishList(data);
      })
      .catch(console.error)
      .finally(() => setFishLoading(false));
  }, [tank.id]);

  const volumeLabel = tank.volumeLiters != null ? `${tank.volumeLiters.toFixed(1)} L` : '—';
  const dimLabel = (tank.lengthCm || tank.widthCm || tank.heightCm)
    ? `${tank.lengthCm ?? '?'} × ${tank.widthCm ?? '?'} × ${tank.heightCm ?? '?'} cm`
    : '—';
  const totalFish = fishList.reduce((s, f) => s + f.quantity, 0);

  return (
    <div className="px-6 pt-5 pb-10">

      {/* Hero */}
      <div className={cn('relative h-44 rounded-2xl overflow-hidden mb-6 bg-gradient-to-br', hero.from, hero.via, hero.to)}>
        {/* Decorative circles */}
        <div className="absolute -top-10 -right-10 w-48 h-48 rounded-full bg-white/5 blur-2xl pointer-events-none" />
        <div className="absolute -bottom-8 -left-8 w-36 h-36 rounded-full bg-white/5 blur-xl pointer-events-none" />
        <div className="absolute inset-0 opacity-5">
          <Droplets className="w-64 h-64 absolute -bottom-12 -right-8 text-white" />
        </div>

        {/* Content */}
        <div className="absolute inset-0 flex flex-col justify-between p-5">
          <div className="flex items-start justify-between">
            {typeLabel && (
              <span className={cn('text-[11px] font-bold uppercase tracking-wider px-2.5 py-1 rounded-full bg-white/10 border border-white/15', hero.accent)}>
                {typeLabel}
              </span>
            )}
            <div className="flex items-center gap-1.5 ml-auto">
              <button
                onClick={() => onEdit(tank)}
                className="p-2 rounded-lg bg-white/10 hover:bg-white/20 transition-colors"
                title={t('aquarium.editTitle')}
              >
                <Pencil className="w-3.5 h-3.5 text-white" />
              </button>
              <button
                onClick={() => onDelete(tank.id)}
                className="p-2 rounded-lg bg-red-500/20 hover:bg-red-500/40 transition-colors"
                title={t('aquarium.deleteTitle')}
              >
                <Trash2 className="w-3.5 h-3.5 text-red-400" />
              </button>
            </div>
          </div>
          <div>
            <h2 className="text-2xl font-black text-white leading-tight">{tank.name}</h2>
            {tank.description && (
              <p className="text-white/50 text-sm mt-1 line-clamp-1">{tank.description}</p>
            )}
          </div>
        </div>
      </div>

      {/* Stats grid */}
      <div className="grid grid-cols-2 sm:grid-cols-5 gap-3 mb-8">
        <StatItem icon={<FlaskConical className="w-3.5 h-3.5" />} label={t('aquarium.volume')} value={volumeLabel} />
        <StatItem icon={<Ruler className="w-3.5 h-3.5" />} label={t('aquarium.dimensions')} value={dimLabel} />
        <StatItem icon={<Calendar className="w-3.5 h-3.5" />} label={t('aquarium.createdAt')} value={formatDate(tank.createdAt, t('aquarium.dateLocale'))} />
        <StatItem
          icon={<Fish className="w-3.5 h-3.5" />}
          label={t('aquarium.speciesCount')}
          value={fishLoading ? '' : `${fishList.length} ${t('aquarium.speciesUnit')}`}
          loading={fishLoading}
        />
        <StatItem
          icon={<Layers className="w-3.5 h-3.5" />}
          label={t('aquarium.fishCount')}
          value={fishLoading ? '' : `${totalFish} ${t('aquarium.fishUnit')}`}
          loading={fishLoading}
        />
      </div>

      {/* Fish inventory */}
      <FishInventorySection
        aquariumId={tank.id}
        fishList={fishList}
        loading={fishLoading}
        onNavigateFish={specCode => navigate(`/fish/${specCode}`)}
      />
    </div>
  );
}

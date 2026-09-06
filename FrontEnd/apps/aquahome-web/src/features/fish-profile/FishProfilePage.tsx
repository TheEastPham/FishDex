import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  useTranslation, cn, useAuthStore, useAnonQuotaStore,
  checkFavorite, addFavorite, removeFavorite, recordView,
  useFishProfile, getCached, setCached, invalidateCache, CacheKeys, FAVORITE_CHECK_TTL,
  MAP_TILE_LAYER,
} from '@fishlover/shared';
import type { CountryDistributionDto, OccurrencePointDto } from '@fishlover/shared';
import {
  ArrowLeft, Share2, Heart, Fish, Ruler, Droplets, Map as MapIcon,
  Image as ImageIcon, Scale, AlertTriangle, Shield,
  Thermometer, TestTube, BookOpen, FileText, Activity, Clock,
  ChevronLeft, ChevronRight, Layers, Pencil
} from 'lucide-react';
import { MapContainer, TileLayer, Marker, Popup, useMap } from 'react-leaflet';
import 'leaflet/dist/leaflet.css';
import L from 'leaflet';
import SpeciesCard from '../fish-search/components/SpeciesCard';
import AddLocalNameModal from '../community/AddLocalNameModal';
import AddToCountryButton from '../market/components/AddToCountryButton';
import SoldInBadge from '../market/components/SoldInBadge';
import QuotaWall from './components/QuotaWall';
import GuestQuotaBanner from './components/GuestQuotaBanner';

// Fix leaflet default icon issue
import iconUrl from 'leaflet/dist/images/marker-icon.png';
import iconRetinaUrl from 'leaflet/dist/images/marker-icon-2x.png';
import shadowUrl from 'leaflet/dist/images/marker-shadow.png';

delete (L.Icon.Default.prototype as any)._getIconUrl;
L.Icon.Default.mergeOptions({ iconRetinaUrl, iconUrl, shadowUrl });

/* ── Helpers ──────────────────────────────────────────────── */

const IUCN_COLORS: Record<string, { bg: string; text: string; border: string }> = {
  LC: { bg: 'bg-emerald-500/15', text: 'text-emerald-400', border: 'border-emerald-500/30' },
  NT: { bg: 'bg-yellow-500/15', text: 'text-yellow-400', border: 'border-yellow-500/30' },
  VU: { bg: 'bg-orange-500/15', text: 'text-orange-400', border: 'border-orange-500/30' },
  EN: { bg: 'bg-rose-500/15', text: 'text-rose-400', border: 'border-rose-500/30' },
  CR: { bg: 'bg-red-600/15', text: 'text-red-400', border: 'border-red-600/30' },
  EW: { bg: 'bg-purple-500/15', text: 'text-purple-400', border: 'border-purple-500/30' },
  EX: { bg: 'bg-slate-500/15', text: 'text-slate-400', border: 'border-slate-500/30' },
};

const DANGEROUS_COLORS: Record<string, string> = {
  harmless: 'text-emerald-400',
  venomous: 'text-rose-500',
  traumatogenic: 'text-orange-500',
  'poisonous to eat': 'text-purple-500',
  ciguatera: 'text-red-500',
  'reports of ciguatera poisoning': 'text-red-500',
};

const DANGEROUS_KEYS: Record<string, string> = {
  harmless: 'harmless',
  venomous: 'venomous',
  traumatogenic: 'traumatogenic',
  'poisonous to eat': 'poisonous_to_eat',
  ciguatera: 'ciguatera',
  'reports of ciguatera poisoning': 'ciguatera',
};

function InfoRow({ label, value, icon }: { label: string; value: string | null | undefined; icon?: React.ReactNode }) {
  return (
    <div className="flex justify-between items-center bg-[#141518] p-4 rounded-xl border border-slate-800/50">
      <span className="text-sm text-slate-400 font-medium flex items-center gap-2">{icon}{label}</span>
      <span className="font-semibold text-slate-200 text-right max-w-[55%] truncate">{value || '—'}</span>
    </div>
  );
}

function StatCard({ icon, label, value, sub }: { icon: React.ReactNode; label: string; value: string; sub?: string }) {
  return (
    <div className="bg-[#1e2024]/80 backdrop-blur-xl rounded-2xl p-5 shadow-xl border border-slate-700/50 flex flex-col items-center justify-center text-center hover:bg-[#25282d]/90 transition-all min-h-[130px] hover:-translate-y-1 hover:shadow-2xl">
      {icon}
      <span className="text-[11px] text-slate-400 uppercase tracking-widest font-bold mt-3 mb-1">{label}</span>
      <span className="text-lg font-black text-slate-100">{value}</span>
      {sub && <span className="text-xs text-slate-500 mt-0.5">{sub}</span>}
    </div>
  );
}

function SectionHeader({ icon, title }: { icon: React.ReactNode; title: string }) {
  return (
    <h3 className="text-lg font-bold text-white mb-5 flex items-center gap-3">
      <div className="p-2 rounded-lg">{icon}</div>
      {title}
    </h3>
  );
}

/* ── MapController — fitBounds on point change (no remount) ── */

function MapController({ points }: { points: OccurrencePointDto[] }) {
  const map = useMap();
  useEffect(() => {
    if (!points.length) return;
    const bounds = L.latLngBounds(points.map(p => [p.lat, p.lon] as [number, number]));
    map.fitBounds(bounds, { padding: [30, 30], maxZoom: 9 });
  }, [points, map]);
  return null;
}

/* ── Helpers ──────────────────────────────────────────────── */

function formatRange(min: number | null | undefined, max: number | null | undefined): string {
  if (min == null && max == null) return '—';
  if (min == null) return `≤ ${max}`;
  if (max == null) return `≥ ${min}`;
  return `${min} – ${max}`;
}

function HabitatFlag({ label, icon }: { label: string; icon: string }) {
  return (
    <span className="inline-flex items-center gap-1.5 text-xs bg-slate-800 text-slate-300 border border-slate-700/60 px-2.5 py-1 rounded-full">
      <span>{icon}</span>{label}
    </span>
  );
}

function resilienceColor(level: string): string {
  switch (level) {
    case 'High':    return 'bg-emerald-900/60 text-emerald-300';
    case 'Medium':  return 'bg-yellow-900/60 text-yellow-300';
    case 'Low':     return 'bg-orange-900/60 text-orange-300';
    case 'VeryLow': return 'bg-red-900/60 text-red-300';
    default:        return 'bg-slate-800 text-slate-400';
  }
}

/* ── Main Component ───────────────────────────────────────── */

export default function FishProfilePage() {
  const { specCode } = useParams<{ specCode: string }>();
  const navigate = useNavigate();
  const { t, i18n } = useTranslation();

  const id = specCode ? parseInt(specCode, 10) : null;
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const [showAddName, setShowAddName] = useState(false);
  // Chỉ loài FishBase (< 500000) mới đóng góp tên được — loài community sửa tên trên snapshot.
  const canAddName = isAuthenticated && id !== null && id < 500000;
  const { detail, media, distribution, relatedSpecies, loading, quotaExceeded } = useFishProfile(id, i18n.language);
  const quota = useAnonQuotaStore();

  const [isFavorite, setIsFavorite] = useState(false);
  const [favoriteLoading, setFavoriteLoading] = useState(false);
  const [selectedCountry, setSelectedCountry] = useState<CountryDistributionDto | null>(null);
  const [selectedImageIndex, setSelectedImageIndex] = useState<number | null>(null);

  // Reset UI state when navigating between fish profiles
  useEffect(() => {
    setSelectedCountry(null);
    setSelectedImageIndex(null);
  }, [id]);

  // Record view for recently-viewed history — fire-and-forget, auth failure is silent.
  // Khách thì bỏ hẳn: lịch sử xem là tính năng của tài khoản, gọi cũng chỉ nhận 401 và
  // rác console — mà giờ trang này khách vào được nên rác đó xuất hiện ở mọi lượt xem.
  useEffect(() => {
    if (!id || !isAuthenticated) return;
    recordView(id).catch(() => {});
  }, [id, isAuthenticated]);

  // Check favorite status — cached, AquaHome failure must not crash this page
  useEffect(() => {
    if (!id || !isAuthenticated) return;
    const key = CacheKeys.favoriteCheck(id);
    const cached = getCached<boolean>(key);
    if (cached !== null) { setIsFavorite(cached); return; }
    checkFavorite(id)
      .then((val) => { setCached(key, val, FAVORITE_CHECK_TTL); setIsFavorite(val); })
      .catch(() => {});
  }, [id, isAuthenticated]);

  const handleToggleFavorite = async () => {
    if (!id || favoriteLoading) return;
    // Khách bấm tim: đây là lúc nút này có ích nhất — dẫn sang đăng nhập thay vì im lặng 401.
    if (!isAuthenticated) { navigate('/login'); return; }
    setFavoriteLoading(true);
    try {
      if (isFavorite) {
        await removeFavorite(id);
        setIsFavorite(false);
      } else {
        await addFavorite(id);
        setIsFavorite(true);
      }
      // Bust caches so next visit reflects the change
      invalidateCache(CacheKeys.favoriteCheck(id));
      invalidateCache(CacheKeys.myFavorites());
    } catch (err) {
      console.error(err);
    } finally {
      setFavoriteLoading(false);
    }
  };

  /* ── Loading / Error ─────────────────────────────────────── */
  if (loading) {
    return (
      <div className="min-h-screen bg-[#141518] flex items-center justify-center">
        <div className="animate-pulse flex flex-col items-center">
          <Fish className="w-16 h-16 text-slate-600 mb-4 animate-bounce" />
          <p className="text-slate-400 font-medium">Loading profile...</p>
        </div>
      </div>
    );
  }

  // Khách xem được profile loài, nhưng chỉ N loài khác nhau mỗi ngày (đếm ở BE theo specCode).
  // Hết lượt thì soft wall — vẫn có tên và ảnh loài, xem QuotaWall.
  //
  // Chặn ở thẻ trên trang market/tra cứu là chưa đủ — còn URL trực tiếp, link chia sẻ, nút back,
  // và PublicTankDetailPage (cũng public) cũng điều hướng sang đây. Chặn ở trang là chặn mọi đường vào.
  if (quotaExceeded && id !== null) {
    return <QuotaWall specCode={id} limit={quota.limit} resetsInSeconds={quota.resetsInSeconds} />;
  }

  if (!detail) {
    return (
      <div className="min-h-screen bg-[#141518] flex items-center justify-center px-6">
        <div className="max-w-sm text-center">
          <Fish className="w-12 h-12 text-slate-600 mx-auto mb-4" />
          <p className="text-slate-400">{t('fish.notFound')}</p>
          <button
            onClick={() => navigate(-1)}
            className="mt-4 text-sm text-slate-300 underline underline-offset-2 hover:text-white"
          >
            {t('common.back')}
          </button>
        </div>
      </div>
    );
  }

  /* ── Derived data ─────────────────────────────────────────── */
  const filteredPoints: OccurrencePointDto[] = selectedCountry
    ? selectedCountry.occurrences
    : distribution?.countries.flatMap(c => c.occurrences) ?? [];

  const iucnCode = detail.conservation?.iucnCode?.toUpperCase() ?? '';
  const iucnStyle = IUCN_COLORS[iucnCode] ?? IUCN_COLORS['LC'];
  const iucnLabel = iucnCode ? t(`iucn.${iucnCode}`) : t('iucn.NE');
  const isEndangered = ['VU', 'EN', 'CR', 'EW'].includes(iucnCode);

  const rawDangerous = detail.dangerous?.toLowerCase() || '';
  const dangerousKey = DANGEROUS_KEYS[rawDangerous] || 'unknown';
  const dangerousColor = DANGEROUS_COLORS[rawDangerous] || 'text-amber-400';
  const dangerousLabel = rawDangerous ? t(`dangerous.${dangerousKey}`) : t('dangerous.unknown');

  const getSocialBehavior = () => {
    if (!detail.ecology) return null;
    const { schooling, shoaling, solitary } = detail.ecology;
    const behaviors = [];
    if (schooling) behaviors.push(t('fish.schooling'));
    if (shoaling) behaviors.push(t('fish.shoaling'));
    if (solitary) behaviors.push(t('fish.solitary'));
    return behaviors.length > 0 ? behaviors.join(', ') : null;
  };
  const socialBehavior = getSocialBehavior();

  /* ── Render ───────────────────────────────────────────────── */
  return (
    <div className="flex flex-col min-h-screen bg-[#141518] pb-20 font-sans">

      <GuestQuotaBanner />

      {/* ═══════════════════ HERO HEADER ═══════════════════ */}
      <div className="relative h-[380px] md:h-[440px] bg-[#0e0f11] w-full overflow-hidden flex flex-col justify-end pb-[110px]">
        {detail.preferredImageUrl ? (
          <img src={detail.preferredImageUrl} alt={detail.speciesName} className="absolute inset-0 w-full h-full object-cover opacity-40 mix-blend-luminosity" />
        ) : (
          <div className="absolute inset-0 w-full h-full flex items-center justify-center opacity-10 bg-gradient-to-br from-slate-700 to-slate-900">
            <Fish className="w-32 h-32 text-white" />
          </div>
        )}
        <div className="absolute inset-0 bg-gradient-to-t from-[#141518] via-[#141518]/60 to-black/30" />

        {/* Top bar */}
        <div className="absolute top-0 w-full p-4 flex justify-between items-center z-20 max-w-7xl mx-auto left-0 right-0">
          <button onClick={() => navigate(-1)} className="p-2.5 bg-black/30 backdrop-blur-md text-white rounded-full hover:bg-black/60 transition-colors border border-white/10">
            <ArrowLeft className="w-5 h-5" />
          </button>
          <div className="flex gap-2">
            {/* Chỉ admin thấy — tự ẩn nếu không đủ quyền */}
            {id !== null && <AddToCountryButton specCode={id} className="!min-h-[42px] rounded-full bg-black/30 backdrop-blur-md border-white/10" />}
            {canAddName && (
              <button onClick={() => setShowAddName(true)} className="p-2.5 bg-black/30 backdrop-blur-md text-white rounded-full hover:bg-black/60 transition-colors border border-white/10" title={t('contribute.addLocalName')}>
                <Pencil className="w-5 h-5" />
              </button>
            )}
            <button className="p-2.5 bg-black/30 backdrop-blur-md text-white rounded-full hover:bg-black/60 transition-colors border border-white/10" title={t('fish.share')}>
              <Share2 className="w-5 h-5" />
            </button>
            <button onClick={handleToggleFavorite} disabled={favoriteLoading} className="p-2.5 bg-black/30 backdrop-blur-md text-white rounded-full hover:bg-black/60 transition-colors border border-white/10 disabled:opacity-60" title={t('fish.addToFavorites')}>
              <Heart className={cn("w-5 h-5 transition-all", isFavorite ? "fill-rose-500 text-rose-500 scale-110" : "text-white")} />
            </button>
          </div>
        </div>

        {showAddName && id !== null && (
          <AddLocalNameModal specCode={id} onClose={() => setShowAddName(false)} />
        )}

        {/* Title content */}
        <div className="relative z-10 w-full px-6 md:px-12 text-center max-w-5xl mx-auto">
          {/* Full Taxonomy breadcrumb */}
          <div className="inline-flex flex-wrap justify-center items-center gap-1.5 mb-5 bg-[#1a1c20]/60 backdrop-blur-xl px-5 py-2 rounded-full border border-slate-700/50 text-xs font-bold text-slate-300 uppercase tracking-widest shadow-lg">
            <span className="w-2 h-2 rounded-full bg-emerald-400 shrink-0 shadow-[0_0_8px_rgba(52,211,153,0.8)]" />
            {detail.kingdom && <><span className="hidden sm:inline">{detail.kingdom}</span><span className="text-slate-500/80 hidden sm:inline">/</span></>}
            {detail.phylum && <><span className="hidden sm:inline">{detail.phylum}</span><span className="text-slate-500/80 hidden sm:inline">/</span></>}
            {detail.className && <><span className="hidden md:inline">{detail.className}</span><span className="text-slate-500/80 hidden md:inline">/</span></>}
            {detail.orderName && <><span className="hidden lg:inline">{detail.orderName}</span><span className="text-slate-500/80 hidden lg:inline">/</span></>}
            {detail.familyName && <><span>{detail.familyName}</span><span className="text-slate-500/80">/</span></>}
            {detail.genusName && <><span>{detail.genusName}</span><span className="text-slate-500/80">/</span></>}
            <span className="text-white normal-case italic">{detail.speciesName.split(' ').pop()}</span>
          </div>
          
          <h1 className="text-4xl md:text-6xl font-black text-transparent bg-clip-text bg-gradient-to-r from-white via-[#f9e5b9] to-amber-200 drop-shadow-2xl mb-2 tracking-tight">
            {detail.preferredCommonName || detail.speciesName}
          </h1>
          {detail.preferredCommonName && (
            <h2 className="text-xl md:text-2xl text-slate-300/80 italic font-medium drop-shadow-md">
              {detail.speciesName}
            </h2>
          )}

          {/* Chỉ báo sang lớp market — không đổi hành vi gì khác của trang tra cứu */}
          {id !== null && <SoldInBadge specCode={id} />}
        </div>
      </div>

      {/* ═══════════════════ MAIN CONTENT ═══════════════════ */}
      <div className="max-w-6xl mx-auto w-full px-4 -mt-20 md:-mt-24 relative z-30 space-y-6">

        {/* ─── Row 1: Quick Facts ─── */}
        <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
          <StatCard icon={<Droplets className="w-7 h-7 text-sky-400" />}   label={t('fish.waterType')} value={detail.waterType || '—'} />
          <StatCard icon={<Ruler className="w-7 h-7 text-emerald-400" />}   label={t('fish.length')}    value={detail.length ? `${detail.length.toFixed(2)} cm` : '—'} />
          <StatCard icon={<Scale className="w-7 h-7 text-orange-400" />}    label={t('fish.weight')}    value={detail.weight ? `${detail.weight.toFixed(2)} kg` : '—'} />
          <StatCard icon={<Clock className="w-7 h-7 text-indigo-400" />}    label={t('fish.longevityWild')} value={detail.longevityWild ? `${detail.longevityWild} Yrs` : '—'} />
        </div>

        {/* ─── Row 2: Water Params + Ecology ─── */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {/* Water Parameters */}
          <div className="bg-[#202226] rounded-2xl p-6 shadow-lg border border-slate-800/80">
            <SectionHeader icon={<Droplets className="w-5 h-5 text-sky-400" />} title={t('fish.waterParameters')} />
            <div className="grid grid-cols-3 gap-3">
              <div className="bg-[#141518] rounded-xl p-4 border border-slate-800/50 text-center">
                <Thermometer className="w-5 h-5 text-red-400 mx-auto mb-2" />
                <p className="text-[10px] text-slate-500 font-bold uppercase tracking-widest mb-1">Temp</p>
                <p className="text-xl font-black text-slate-200">
                  {formatRange(detail.environment?.tempMin, detail.environment?.tempMax)}
                </p>
                <p className="text-xs text-slate-500">°C</p>
                {detail.environment?.tempPreferred != null && (
                  <p className="text-[10px] text-amber-400 mt-1">{t('fish.tempPreferred')}: {detail.environment.tempPreferred}°C</p>
                )}
              </div>
              <div className="bg-[#141518] rounded-xl p-4 border border-slate-800/50 text-center">
                <TestTube className="w-5 h-5 text-violet-400 mx-auto mb-2" />
                <p className="text-[10px] text-slate-500 font-bold uppercase tracking-widest mb-1">pH</p>
                <p className="text-xl font-black text-slate-200">
                  {formatRange(detail.environment?.phMin, detail.environment?.phMax)}
                </p>
                <p className="text-xs text-slate-500">Level</p>
              </div>
              <div className="bg-[#141518] rounded-xl p-4 border border-slate-800/50 text-center">
                <Activity className="w-5 h-5 text-cyan-400 mx-auto mb-2" />
                <p className="text-[10px] text-slate-500 font-bold uppercase tracking-widest mb-1">{t('fish.waterHardness')}</p>
                <p className="text-xl font-black text-slate-200">
                  {formatRange(detail.environment?.dHMin, detail.environment?.dHMax)}
                </p>
                <p className="text-xs text-slate-500">dGH</p>
              </div>
            </div>
            {detail.environment?.resilience && (
              <div className="mt-3 flex items-center gap-2">
                <span className="text-xs text-slate-400">{t('fish.resilience')}:</span>
                <span className={`text-xs font-semibold px-2 py-0.5 rounded-full ${resilienceColor(detail.environment.resilience)}`}>
                  {t(`fish.resilience${detail.environment.resilience}`)}
                </span>
                {detail.environment.resilienceRemark && (
                  <span className="text-xs text-slate-500 truncate">{detail.environment.resilienceRemark}</span>
                )}
              </div>
            )}
          </div>

          {/* Ecology */}
          <div className="bg-[#202226] rounded-2xl p-6 shadow-lg border border-slate-800/80">
            <SectionHeader icon={<Fish className="w-5 h-5 text-teal-400" />} title={t('fish.ecology')} />
            <div className="space-y-3">
              <InfoRow label={t('fish.feedingType')} value={detail.ecology?.feedingType} />
              <InfoRow label={t('fish.trophicLevel')} value={detail.ecology?.dietTroph != null ? `${detail.ecology.dietTroph.toFixed(1)}` : null} />
              <InfoRow label={t('fish.demerspelagic')} value={detail.demersPelag} />
              <InfoRow label={t('fish.socialBehavior')} value={socialBehavior} />
            </div>
          </div>
        </div>

        {/* ─── Row 2.5: Habitat Preferences ─── */}
        {detail.habitat && (detail.habitat.preferredSubstrates.length > 0 || detail.habitat.specialHabitats.length > 0 || detail.habitat.requiresCaves || detail.habitat.requiresDriftwood || detail.habitat.requiresVegetation || detail.habitat.requiresCoralReefs) && (
          <div className="bg-[#202226] rounded-2xl p-6 shadow-lg border border-slate-800/80">
            <SectionHeader icon={<Layers className="w-5 h-5 text-lime-400" />} title={t('fish.habitat')} />
            <div className="space-y-4">
              {detail.habitat.preferredSubstrates.length > 0 && (
                <div>
                  <p className="text-xs text-slate-500 uppercase tracking-widest mb-2">{t('fish.preferredSubstrates')}</p>
                  <div className="flex flex-wrap gap-2">
                    {detail.habitat.preferredSubstrates.map(s => (
                      <span key={s} className="text-xs bg-amber-900/40 text-amber-300 border border-amber-700/40 px-2.5 py-1 rounded-full">
                        {t(`fish.substrate_${s}`, { defaultValue: s })}
                      </span>
                    ))}
                  </div>
                </div>
              )}
              {detail.habitat.specialHabitats.length > 0 && (
                <div>
                  <p className="text-xs text-slate-500 uppercase tracking-widest mb-2">{t('fish.specialHabitats')}</p>
                  <div className="flex flex-wrap gap-2">
                    {detail.habitat.specialHabitats.map(h => (
                      <span key={h} className="text-xs bg-teal-900/40 text-teal-300 border border-teal-700/40 px-2.5 py-1 rounded-full">
                        {t(`fish.habitat_${h}`, { defaultValue: h })}
                      </span>
                    ))}
                  </div>
                </div>
              )}
              <div className="flex flex-wrap gap-3">
                {detail.habitat.burrowingCapable && <HabitatFlag label={t('fish.burrowingCapable')} icon="⛏️" />}
                {detail.habitat.requiresCaves && <HabitatFlag label={t('fish.requiresCaves')} icon="🕳️" />}
                {detail.habitat.requiresDriftwood && <HabitatFlag label={t('fish.requiresDriftwood')} icon="🪵" />}
                {detail.habitat.requiresVegetation && <HabitatFlag label={t('fish.requiresVegetation')} icon="🌿" />}
                {detail.habitat.requiresCoralReefs && <HabitatFlag label={t('fish.requiresCoralReefs')} icon="🪸" />}
              </div>
            </div>
          </div>
        )}

        {/* ─── Row 3: Conservation + Dangerous ─── */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          {/* IUCN Status — larger card */}
          <div className="md:col-span-2 bg-[#202226] rounded-2xl p-6 shadow-lg border border-slate-800/80">
            <SectionHeader icon={<Shield className="w-5 h-5 text-rose-400" />} title={t('fish.conservation')} />
            {isEndangered && (
              <div className="mb-4 bg-rose-500/10 border border-rose-500/30 p-3.5 rounded-xl flex items-start gap-3">
                <AlertTriangle className="w-5 h-5 text-rose-400 shrink-0 mt-0.5" />
                <p className="text-sm text-rose-200/90 leading-relaxed font-medium">{t('fish.endangeredWarning')}</p>
              </div>
            )}
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              {/* IUCN Badge */}
              <div className={cn("rounded-xl p-5 border flex flex-col items-center justify-center text-center", iucnStyle.bg, iucnStyle.border)}>
                <span className={cn("text-3xl font-black mb-1", iucnStyle.text)}>{iucnCode || 'NE'}</span>
                <span className={cn("text-sm font-semibold", iucnStyle.text)}>{iucnLabel}</span>
                {detail.conservation?.iucnDateAssessed && (
                  <span className="text-[11px] text-slate-500 mt-1">Assessed: {detail.conservation.iucnDateAssessed.slice(0, 10)}</span>
                )}
              </div>
              {/* Conservation details */}
              <div className="space-y-3">
                <InfoRow label={t('fish.citesCode')} value={detail.conservation?.citesCode || t('fish.notListed')} />
                {detail.conservation?.iucnAssessment && (
                  <div className="bg-[#141518] p-4 rounded-xl border border-slate-800/50">
                    <p className="text-xs text-slate-500 font-bold uppercase tracking-widest mb-2">{t('fish.assessment')}</p>
                    <p className="text-sm text-slate-300 leading-relaxed line-clamp-3">{detail.conservation.iucnAssessment}</p>
                  </div>
                )}
              </div>
            </div>
          </div>

          {/* Dangerous */}
          <div className="bg-[#202226] rounded-2xl p-6 shadow-lg border border-slate-800/80 flex flex-col">
            <SectionHeader icon={<AlertTriangle className="w-5 h-5 text-amber-400" />} title={t('fish.dangerous')} />
            <div className="flex-1 flex flex-col items-center justify-center text-center">
              <span className={cn("text-2xl font-black mb-1 text-center", dangerousColor)}>
                {dangerousLabel}
              </span>
              <span className="text-xs text-slate-500">{t('fish.safetyRating')}</span>
            </div>
          </div>
        </div>

        {/* ─── Row 4: Remark / Description ─── */}
        {detail.remark && (
          <div className="bg-[#202226] rounded-2xl p-6 shadow-lg border border-slate-800/80">
            <SectionHeader icon={<BookOpen className="w-5 h-5 text-amber-400" />} title={t('fish.description')} />
            <p className="text-slate-300 text-[15px] leading-relaxed whitespace-pre-line">{detail.remark}</p>
          </div>
        )}

        {/* ─── Row 5: Male vs Female Comparison ─── */}
        {(detail.maleImageUrl || detail.femaleImageUrl) && (
          <div className="bg-[#202226] rounded-2xl p-6 shadow-lg border border-slate-800/80">
            <SectionHeader icon={<FileText className="w-5 h-5 text-pink-400" />} title={t('fish.sexualDimorphism')} />
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              {detail.maleImageUrl && (
                <div className="relative rounded-xl overflow-hidden border border-slate-800/50 group">
                  <img src={detail.maleImageUrl} alt="Male" className="w-full h-56 object-cover group-hover:scale-105 transition-transform duration-500" />
                  <div className="absolute bottom-0 w-full bg-gradient-to-t from-black/80 to-transparent p-3">
                    <span className="text-white font-bold text-sm flex items-center gap-1.5">
                      <span className="w-3 h-3 rounded-full bg-sky-400" /> {t('fish.male')}
                    </span>
                  </div>
                </div>
              )}
              {detail.femaleImageUrl && (
                <div className="relative rounded-xl overflow-hidden border border-slate-800/50 group">
                  <img src={detail.femaleImageUrl} alt="Female" className="w-full h-56 object-cover group-hover:scale-105 transition-transform duration-500" />
                  <div className="absolute bottom-0 w-full bg-gradient-to-t from-black/80 to-transparent p-3">
                    <span className="text-white font-bold text-sm flex items-center gap-1.5">
                      <span className="w-3 h-3 rounded-full bg-pink-400" /> {t('fish.female')}
                    </span>
                  </div>
                </div>
              )}
            </div>
          </div>
        )}

        {/* ─── Row 6: Distribution Map + Country Sidebar ─── */}
        {distribution && distribution.countries.length > 0 && (
          <div className="bg-[#202226] rounded-2xl p-6 shadow-lg border border-slate-800/80">
            {/* Header */}
            <div className="flex items-center justify-between mb-5">
              <h3 className="text-lg font-bold text-white flex items-center gap-3">
                <div className="p-2 rounded-lg"><MapIcon className="w-5 h-5 text-green-400" /></div>
                {t('fish.occurrencesMap')}
                <span className="text-sm font-normal text-slate-500">
                  · {distribution.totalOccurrences} {t('fish.distributionRecords')}
                  · {distribution.countries.length} {t('fish.distributionCountries')}
                </span>
              </h3>
              {selectedCountry && (
                <button
                  onClick={() => setSelectedCountry(null)}
                  className="text-xs px-3 py-1.5 rounded-lg bg-slate-700/50 hover:bg-slate-700 text-slate-300 transition-colors border border-slate-700/50"
                >
                  {t('fish.showAll')}
                </button>
              )}
            </div>

            {/* Two-column layout: country list | map — stacks vertically on mobile */}
            <div className="flex flex-col md:flex-row gap-4">
              {/* Country list */}
              <div className="md:w-44 md:shrink-0 flex flex-row md:flex-col gap-1 overflow-x-auto md:overflow-x-visible md:max-h-[400px] md:overflow-y-auto pb-1 md:pb-0 pr-0 md:pr-1">
                {distribution.countries.map(c => {
                  const isSelected = selectedCountry?.code === c.code;
                  return (
                    <button
                      key={c.code}
                      onClick={() => setSelectedCountry(prev => prev?.code === c.code ? null : c)}
                      className={cn(
                        "w-full flex items-center gap-2 px-2.5 py-2 rounded-lg text-left transition-colors border",
                        isSelected
                          ? "bg-sky-500/20 border-sky-500/50 text-sky-300"
                          : "bg-[#141518] border-slate-800/50 text-slate-300 hover:bg-slate-800"
                      )}
                    >
                      {c.alpha2 ? (
                        <span className={`fi fi-${c.alpha2} rounded-sm shrink-0`} style={{ fontSize: '1em' }} />
                      ) : (
                        <span className="text-sm shrink-0">🌍</span>
                      )}
                      <span className="flex-1 truncate text-xs leading-tight">{c.name}</span>
                      <span className="text-[10px] text-slate-500 font-mono shrink-0">{c.count}</span>
                    </button>
                  );
                })}
              </div>

              {/* Map — no remount on filter change */}
              <div className="flex-1 h-[280px] md:h-[400px] rounded-xl overflow-hidden border border-slate-800/50 relative z-0">
                <MapContainer center={[0, 0]} zoom={2} scrollWheelZoom={true} className="h-full w-full z-0" style={{ background: '#141518' }}>
                  <TileLayer {...MAP_TILE_LAYER} />
                  <MapController points={filteredPoints} />
                  {filteredPoints.map((p, i) => (
                    <Marker key={i} position={[p.lat, p.lon]}>
                      <Popup>
                        <div className="text-slate-800">
                          {p.locality && <p className="font-bold text-sm">{p.locality}</p>}
                          {p.province && <p className="text-xs text-slate-600">{p.province}</p>}
                        </div>
                      </Popup>
                    </Marker>
                  ))}
                </MapContainer>
                {!isAuthenticated && filteredPoints.length === 0 && (
                  <div className="absolute inset-x-0 bottom-0 z-[400] bg-[#141518]/90 backdrop-blur-sm border-t border-slate-700/60 px-4 py-3 text-center">
                    <p className="text-xs text-slate-300">{t('quota.mapLocked')}</p>
                    <button
                      onClick={() => navigate('/login')}
                      className="mt-1 text-xs font-semibold text-emerald-300 underline underline-offset-2"
                    >
                      {t('quota.loginCta')}
                    </button>
                  </div>
                )}
              </div>
            </div>
          </div>
        )}

        {/* ─── Row 7: Gallery (Carousel) ─── */}
        {media.length > 0 && (
          <div className="bg-[#202226] rounded-2xl p-6 shadow-lg border border-slate-800/80">
            <SectionHeader icon={<ImageIcon className="w-5 h-5 text-indigo-400" />} title={`${t('fish.gallery')} (${media.length})`} />
            <div className="relative w-full h-[280px] md:h-[400px] rounded-xl overflow-hidden border border-slate-800/50 bg-[#141518] group">
              {/* Display Current Image */}
              {media[selectedImageIndex || 0].url ? (
                <img 
                  src={media[selectedImageIndex || 0].url || undefined} 
                  alt={media[selectedImageIndex || 0].name || undefined} 
                  className="w-full h-full object-contain" 
                />
              ) : (
                <div className="w-full h-full flex items-center justify-center">
                  <Fish className="w-16 h-16 text-slate-700" />
                </div>
              )}
              
              {/* Navigation overlays */}
              <div className="absolute inset-0 flex items-center justify-between p-4 opacity-0 group-hover:opacity-100 transition-opacity">
                <button 
                  className="p-2 bg-black/50 hover:bg-black/80 text-white rounded-full transition-colors"
                  onClick={() => setSelectedImageIndex(prev => prev === null || prev === 0 ? media.length - 1 : prev - 1)}
                >
                  <ChevronLeft className="w-6 h-6" />
                </button>
                <button 
                  className="p-2 bg-black/50 hover:bg-black/80 text-white rounded-full transition-colors"
                  onClick={() => setSelectedImageIndex(prev => prev === null || prev === media.length - 1 ? 0 : prev + 1)}
                >
                  <ChevronRight className="w-6 h-6" />
                </button>
              </div>

              {/* Bottom Info Bar */}
              <div className="absolute bottom-0 w-full bg-black/60 backdrop-blur-md p-3 flex justify-between items-center text-white">
                <div>
                  <p className="font-bold text-sm">{media[selectedImageIndex || 0].gender}</p>
                  <p className="text-slate-400 text-xs">{media[selectedImageIndex || 0].pictureType}</p>
                </div>
                <div className="text-sm font-semibold bg-white/10 px-3 py-1 rounded-full">
                  {(selectedImageIndex || 0) + 1} / {media.length}
                </div>
              </div>
            </div>

            {/* Thumbnail Navigation */}
            <div className="mt-3 flex gap-2 overflow-x-auto pb-2">
              {media.map((m, i) => (
                <button 
                  key={m.id} 
                  onClick={() => setSelectedImageIndex(i)}
                  className={cn(
                    "relative h-16 w-16 shrink-0 rounded-lg overflow-hidden border-2 transition-all",
                    (selectedImageIndex || 0) === i ? "border-indigo-500 scale-105" : "border-transparent opacity-50 hover:opacity-100"
                  )}
                >
                  {m.url ? (
                    <img src={m.url || undefined} className="w-full h-full object-cover" />
                  ) : (
                    <div className="w-full h-full bg-[#141518] flex items-center justify-center"><Fish className="w-4 h-4 text-slate-600"/></div>
                  )}
                </button>
              ))}
            </div>

            {/* Khách chỉ nhận ảnh đại diện từ endpoint public — nói thẳng còn gì phía sau. */}
            {!isAuthenticated && (
              <button
                onClick={() => navigate('/login')}
                className="mt-2 w-full min-h-[44px] rounded-lg border border-slate-700/60 bg-[#141518] text-xs text-slate-300 hover:bg-slate-800 transition-colors"
              >
                {t('quota.galleryLocked')}
              </button>
            )}
          </div>
        )}

        {/* ─── Row 8: Related Species ─── */}
        {relatedSpecies.length > 0 && (
          <div className="pt-6 mt-8 border-t border-slate-800/50">
            <SectionHeader icon={<Fish className="w-5 h-5 text-fuchsia-400" />} title={t('fish.relatedSpecies')} />
            <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-5">
              {relatedSpecies.map((species, i) => (
                <SpeciesCard key={species.specCode} species={species} index={i} />
              ))}
            </div>
          </div>
        )}

      </div>
    </div>
  );
}

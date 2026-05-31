import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { 
  useTranslation, cn, getSpeciesDetail, getSpeciesMedia, 
  getSpeciesOccurrences, getSpeciesCountries, getRelatedSpecies, getCountryCode 
} from '@fishlover/shared';
import type { 
  SpeciesDetail, SystemImageDto, OccurrenceDto, 
  CountryDto, SpeciesSearchResult 
} from '@fishlover/shared';
import {
  ArrowLeft, Share2, Heart, Fish, Ruler, Droplets, Map as MapIcon,
  Image as ImageIcon, Scale, AlertTriangle, Shield,
  Thermometer, TestTube, BookOpen, FileText, Activity, Clock
} from 'lucide-react';
import { MapContainer, TileLayer, Marker, Popup } from 'react-leaflet';
import 'leaflet/dist/leaflet.css';
import L from 'leaflet';
import SpeciesCard from '../fish-search/components/SpeciesCard';

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
    <div className="bg-[#202226] rounded-2xl p-5 shadow-lg border border-slate-800/80 flex flex-col items-center justify-center text-center hover:bg-[#25282d] transition-colors min-h-[130px]">
      {icon}
      <span className="text-[11px] text-slate-500 uppercase tracking-widest font-bold mt-3 mb-1">{label}</span>
      <span className="text-lg font-bold text-slate-200">{value}</span>
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

/* ── Main Component ───────────────────────────────────────── */

export default function FishProfilePage() {
  const { specCode } = useParams<{ specCode: string }>();
  const navigate = useNavigate();
  const { t, i18n } = useTranslation();

  const [detail, setDetail] = useState<SpeciesDetail | null>(null);
  const [media, setMedia] = useState<SystemImageDto[]>([]);
  const [occurrences, setOccurrences] = useState<OccurrenceDto[]>([]);
  const [countries, setCountries] = useState<CountryDto[]>([]);
  const [relatedSpecies, setRelatedSpecies] = useState<SpeciesSearchResult[]>([]);
  const [loading, setLoading] = useState(true);
  const [isFavorite, setIsFavorite] = useState(false);

  useEffect(() => {
    if (!specCode) return;
    const fetchAll = async () => {
      setLoading(true);
      try {
        const id = parseInt(specCode, 10);
        const [detailData, mediaData, occData, countryData, relatedData] = await Promise.all([
          getSpeciesDetail(id, i18n.language),
          getSpeciesMedia(id),
          getSpeciesOccurrences(id),
          getSpeciesCountries(id),
          getRelatedSpecies(id, 6, i18n.language),
        ]);
        setDetail(detailData);
        setMedia(mediaData);
        setOccurrences(occData);
        setCountries(countryData);
        setRelatedSpecies(relatedData);
      } catch (err) {
        console.error(err);
      } finally {
        setLoading(false);
      }
    };
    fetchAll();
  }, [specCode, i18n.language]);

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

  if (!detail) {
    return <div className="p-8 text-center text-red-500">Species not found.</div>;
  }

  /* ── Derived data ─────────────────────────────────────────── */
  const mapCenter: [number, number] =
    occurrences.length > 0 && occurrences[0].latitudeDec && occurrences[0].longitudeDec
      ? [occurrences[0].latitudeDec, occurrences[0].longitudeDec]
      : [0, 0];

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

      {/* ═══════════════════ HERO HEADER ═══════════════════ */}
      <div className="relative h-[420px] md:h-[480px] bg-slate-900 w-full overflow-hidden">
        {detail.preferredImageUrl ? (
          <img src={detail.preferredImageUrl} alt={detail.speciesName} className="w-full h-full object-cover opacity-50" />
        ) : (
          <div className="w-full h-full flex items-center justify-center opacity-20 bg-gradient-to-br from-slate-700 to-slate-900">
            <Fish className="w-24 h-24 text-white" />
          </div>
        )}
        <div className="absolute inset-0 bg-gradient-to-t from-[#141518] via-[#141518]/50 to-black/20" />

        {/* Top bar */}
        <div className="absolute top-0 w-full p-4 flex justify-between items-center z-10 max-w-7xl mx-auto left-0 right-0">
          <button onClick={() => navigate(-1)} className="p-2.5 bg-black/40 backdrop-blur-md text-white rounded-full hover:bg-black/60 transition-colors">
            <ArrowLeft className="w-5 h-5" />
          </button>
          <div className="flex gap-2">
            <button className="p-2.5 bg-black/40 backdrop-blur-md text-white rounded-full hover:bg-black/60 transition-colors" title={t('fish.share')}>
              <Share2 className="w-5 h-5" />
            </button>
            <button onClick={() => setIsFavorite(!isFavorite)} className="p-2.5 bg-black/40 backdrop-blur-md text-white rounded-full hover:bg-black/60 transition-colors" title={t('fish.addToFavorites')}>
              <Heart className={cn("w-5 h-5 transition-colors", isFavorite ? "fill-rose-500 text-rose-500" : "text-white")} />
            </button>
          </div>
        </div>

        {/* Title overlay */}
        <div className="absolute bottom-8 w-full px-6 md:px-12 text-center max-w-5xl mx-auto left-0 right-0">
          {/* Full Taxonomy breadcrumb */}
          <div className="inline-flex flex-wrap justify-center items-center gap-1.5 mb-4 bg-white/10 backdrop-blur-md px-4 py-1.5 rounded-full border border-white/10 text-xs font-semibold text-slate-300 uppercase tracking-wider max-w-full">
            <span className="w-2 h-2 rounded-full bg-emerald-400 shrink-0" />
            {detail.kingdom && <><span className="hidden sm:inline">{detail.kingdom}</span><span className="text-slate-500 hidden sm:inline">›</span></>}
            {detail.phylum && <><span className="hidden sm:inline">{detail.phylum}</span><span className="text-slate-500 hidden sm:inline">›</span></>}
            {detail.className && <><span className="hidden md:inline">{detail.className}</span><span className="text-slate-500 hidden md:inline">›</span></>}
            {detail.orderName && <><span className="hidden lg:inline">{detail.orderName}</span><span className="text-slate-500 hidden lg:inline">›</span></>}
            {detail.familyName && <><span>{detail.familyName}</span><span className="text-slate-500">›</span></>}
            {detail.genusName && <><span>{detail.genusName}</span><span className="text-slate-500">›</span></>}
            <span className="text-white normal-case italic">{detail.speciesName.split(' ').pop()}</span>
          </div>
          <h1 className="text-3xl md:text-5xl font-extrabold text-[#f9e5b9] drop-shadow-2xl mb-1.5 leading-tight">
            {detail.preferredCommonName || detail.speciesName}
          </h1>
          {detail.preferredCommonName && (
            <h2 className="text-lg md:text-xl text-slate-300/90 italic font-light drop-shadow-md">
              {detail.speciesName}
            </h2>
          )}
        </div>
      </div>

      {/* ═══════════════════ MAIN CONTENT ═══════════════════ */}
      <div className="max-w-6xl mx-auto w-full px-4 -mt-4 relative z-20 space-y-4">

        {/* ─── Row 1: Quick Facts ─── */}
        <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
          <StatCard icon={<Droplets className="w-7 h-7 text-sky-400" />}   label={t('fish.waterType')} value={detail.waterType || '—'} />
          <StatCard icon={<Ruler className="w-7 h-7 text-emerald-400" />}   label={t('fish.length')}    value={detail.length ? `${detail.length} cm` : '—'} />
          <StatCard icon={<Scale className="w-7 h-7 text-orange-400" />}    label={t('fish.weight')}    value={detail.weight ? `${detail.weight} kg` : '—'} />
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
                  {detail.environment?.tempMin ?? '?'} – {detail.environment?.tempMax ?? '?'}
                </p>
                <p className="text-xs text-slate-500">°C</p>
              </div>
              <div className="bg-[#141518] rounded-xl p-4 border border-slate-800/50 text-center">
                <TestTube className="w-5 h-5 text-violet-400 mx-auto mb-2" />
                <p className="text-[10px] text-slate-500 font-bold uppercase tracking-widest mb-1">pH</p>
                <p className="text-xl font-black text-slate-200">
                  {detail.environment?.phMin ?? '?'} – {detail.environment?.phMax ?? '?'}
                </p>
                <p className="text-xs text-slate-500">Level</p>
              </div>
              <div className="bg-[#141518] rounded-xl p-4 border border-slate-800/50 text-center">
                <Activity className="w-5 h-5 text-cyan-400 mx-auto mb-2" />
                <p className="text-[10px] text-slate-500 font-bold uppercase tracking-widest mb-1">{t('fish.waterHardness')}</p>
                <p className="text-xl font-black text-slate-200">
                  {detail.environment?.dHMin ?? '?'} – {detail.environment?.dHMax ?? '?'}
                </p>
                <p className="text-xs text-slate-500">dGH</p>
              </div>
            </div>
          </div>

          {/* Ecology */}
          <div className="bg-[#202226] rounded-2xl p-6 shadow-lg border border-slate-800/80">
            <SectionHeader icon={<Fish className="w-5 h-5 text-teal-400" />} title={t('fish.ecology')} />
            <div className="space-y-3">
              <InfoRow label={t('fish.feedingType')} value={detail.ecology?.feedingType} />
              <InfoRow label={t('fish.trophicLevel')} value={detail.ecology?.dietTroph ? `${detail.ecology.dietTroph.toFixed(1)}` : null} />
              <InfoRow label={t('fish.demerspelagic')} value={detail.demersPelag} />
              <InfoRow label={t('fish.socialBehavior')} value={socialBehavior} />
            </div>
          </div>
        </div>

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

        {/* ─── Row 6: Map & Countries Combined ─── */}
        {occurrences.length > 0 && (
          <div className="bg-[#202226] rounded-2xl p-6 shadow-lg border border-slate-800/80">
            <SectionHeader icon={<MapIcon className="w-5 h-5 text-green-400" />} title={`${t('fish.occurrencesMap')} (${occurrences.length} records)`} />
            <div className="h-[400px] w-full rounded-xl overflow-hidden border border-slate-800/50 relative z-0 mb-4">
              <MapContainer center={mapCenter} zoom={3} scrollWheelZoom={false} className="h-full w-full z-0" style={{ background: '#141518' }}>
                <TileLayer
                  attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
                  url="https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png"
                />
                {occurrences.map(occ => occ.latitudeDec && occ.longitudeDec && (
                  <Marker key={occ.id} position={[occ.latitudeDec, occ.longitudeDec]}>
                    <Popup>
                      <div className="text-slate-800">
                        <p className="font-bold">{occ.locality || 'Unknown'}</p>
                        {occ.province && <p className="text-xs text-slate-600">{occ.province}</p>}
                        {occ.countryCode && <p className="text-xs text-slate-500">{occ.countryCode}</p>}
                      </div>
                    </Popup>
                  </Marker>
                ))}
              </MapContainer>
            </div>
            
            {countries.length > 0 && (
              <div className="pt-2 border-t border-slate-800/50">
                <p className="text-sm font-semibold text-slate-400 mb-3">{t('fish.countriesOfOrigin')}</p>
                <div className="flex flex-wrap gap-2">
                  {countries.map(c => {
                    const alpha2 = getCountryCode(c.code);
                    return (
                      <span key={c.code} className="inline-flex items-center gap-1.5 bg-[#141518] border border-slate-800/50 rounded-lg px-3 py-1.5 text-sm font-semibold text-slate-300 shadow-sm">
                        {alpha2 ? (
                          <span className={`fi fi-${alpha2} rounded-sm shadow-sm`} style={{ fontSize: '1.2em' }} />
                        ) : (
                          <span className="text-base leading-none">🌍</span>
                        )}
                        {c.name}
                      </span>
                    );
                  })}
                </div>
              </div>
            )}
          </div>
        )}

        {/* ─── Row 7: Gallery ─── */}
        {media.length > 0 && (
          <div className="bg-[#202226] rounded-2xl p-6 shadow-lg border border-slate-800/80">
            <SectionHeader icon={<ImageIcon className="w-5 h-5 text-indigo-400" />} title={`${t('fish.gallery')} (${media.length})`} />
            <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
              {media.map(m => (
                <div key={m.id} className="aspect-square rounded-xl overflow-hidden bg-[#141518] relative group border border-slate-800/50">
                  {m.url ? (
                    <img src={m.url} alt={m.name} className="w-full h-full object-cover group-hover:scale-110 transition-transform duration-700" />
                  ) : (
                    <div className="w-full h-full flex items-center justify-center">
                      <Fish className="w-8 h-8 text-slate-700" />
                    </div>
                  )}
                  <div className="absolute bottom-0 w-full bg-black/70 backdrop-blur-sm px-3 py-1.5 text-[11px] font-semibold text-white flex justify-between translate-y-full group-hover:translate-y-0 transition-transform duration-300">
                    <span>{m.gender}</span>
                    <span className="text-slate-400">{m.pictureType}</span>
                  </div>
                </div>
              ))}
            </div>
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

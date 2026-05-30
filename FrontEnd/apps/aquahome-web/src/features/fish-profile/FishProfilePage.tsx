import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useTranslation, cn, getSpeciesDetail, getSpeciesMedia, getSpeciesOccurrences } from '@fishlover/shared';
import type { SpeciesDetail, SystemImageDto, OccurrenceDto } from '@fishlover/shared';
import { ArrowLeft, Share2, Heart, Fish, Ruler, Droplets, Map, Image as ImageIcon, Info, Scale } from 'lucide-react';
import { MapContainer, TileLayer, Marker, Popup } from 'react-leaflet';
import 'leaflet/dist/leaflet.css';
import L from 'leaflet';

// Fix leaflet default icon issue in react-leaflet
import iconUrl from 'leaflet/dist/images/marker-icon.png';
import iconRetinaUrl from 'leaflet/dist/images/marker-icon-2x.png';
import shadowUrl from 'leaflet/dist/images/marker-shadow.png';

delete (L.Icon.Default.prototype as any)._getIconUrl;
L.Icon.Default.mergeOptions({
  iconRetinaUrl,
  iconUrl,
  shadowUrl,
});

export default function FishProfilePage() {
  const { specCode } = useParams<{ specCode: string }>();
  const navigate = useNavigate();
  const { t, i18n } = useTranslation();
  
  const [detail, setDetail] = useState<SpeciesDetail | null>(null);
  const [media, setMedia] = useState<SystemImageDto[]>([]);
  const [occurrences, setOccurrences] = useState<OccurrenceDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [isFavorite, setIsFavorite] = useState(false);

  useEffect(() => {
    if (!specCode) return;
    const fetchAll = async () => {
      setLoading(true);
      try {
        const id = parseInt(specCode, 10);
        const [detailData, mediaData, occData] = await Promise.all([
          getSpeciesDetail(id, i18n.language),
          getSpeciesMedia(id),
          getSpeciesOccurrences(id)
        ]);
        setDetail(detailData);
        setMedia(mediaData);
        setOccurrences(occData);
      } catch (err) {
        console.error(err);
      } finally {
        setLoading(false);
      }
    };
    fetchAll();
  }, [specCode, i18n.language]);

  if (loading) {
    return (
      <div className="min-h-screen bg-[#202226] flex items-center justify-center">
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

  const mapCenter = occurrences.length > 0 && occurrences[0].latitudeDec && occurrences[0].longitudeDec 
    ? [occurrences[0].latitudeDec, occurrences[0].longitudeDec] as [number, number]
    : [0, 0] as [number, number];

  return (
    <div className="flex flex-col min-h-screen bg-[#141518] pb-20 font-sans">
      {/* Hero Header */}
      <div className="relative h-[500px] bg-slate-900 w-full overflow-hidden">
        {detail.preferredImageUrl ? (
          <img src={detail.preferredImageUrl} alt={detail.speciesName} className="w-full h-full object-cover opacity-60" />
        ) : (
          <div className="w-full h-full flex items-center justify-center opacity-30 bg-gradient-to-br from-slate-700 to-slate-900">
            <Fish className="w-24 h-24 text-white" />
          </div>
        )}
        <div className="absolute inset-0 bg-gradient-to-t from-[#141518] via-[#141518]/60 to-transparent" />
        
        {/* Top bar */}
        <div className="absolute top-0 w-full p-4 flex justify-between items-center z-10 max-w-7xl mx-auto left-0 right-0">
          <button onClick={() => navigate(-1)} className="p-2.5 bg-black/40 backdrop-blur-md text-white rounded-full hover:bg-black/60 transition-colors">
            <ArrowLeft className="w-5 h-5" />
          </button>
          <div className="flex gap-2">
            <button className="p-2.5 bg-black/40 backdrop-blur-md text-white rounded-full hover:bg-black/60 transition-colors">
              <Share2 className="w-5 h-5" />
            </button>
            <button onClick={() => setIsFavorite(!isFavorite)} className="p-2.5 bg-black/40 backdrop-blur-md text-white rounded-full hover:bg-black/60 transition-colors">
              <Heart className={cn("w-5 h-5 transition-colors", isFavorite ? "fill-rose-500 text-rose-500" : "text-white")} />
            </button>
          </div>
        </div>

        {/* Title overlay */}
        <div className="absolute bottom-12 w-full px-6 md:px-12 text-center max-w-5xl mx-auto left-0 right-0">
          <div className="inline-flex items-center gap-2 mb-3 bg-white/10 backdrop-blur-md px-3 py-1 rounded-full border border-white/10">
            <span className="w-2 h-2 rounded-full bg-emerald-400"></span>
            <p className="text-xs font-bold text-slate-200 uppercase tracking-widest">
              Animalia &gt; {detail.familyName || t('fish.unknown')}
            </p>
          </div>
          <h1 className="text-4xl md:text-6xl font-extrabold text-[#f9e5b9] drop-shadow-2xl mb-2">
            {detail.preferredCommonName || detail.speciesName}
          </h1>
          {detail.preferredCommonName && (
            <h2 className="text-xl md:text-2xl text-slate-300 italic font-light drop-shadow-md">
              {detail.speciesName}
            </h2>
          )}
        </div>
      </div>

      {/* Main Content: Bento Grid */}
      <div className="max-w-6xl mx-auto w-full px-4 -mt-6 relative z-20 space-y-4">
        
        {/* Grid Row 1 (Quick Facts) */}
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <div className="bg-[#202226] rounded-2xl p-5 shadow-lg border border-slate-800/80 flex flex-col items-center justify-center text-center hover:bg-[#25282d] transition-colors">
            <Droplets className="w-7 h-7 text-sky-400 mb-3" />
            <span className="text-[11px] text-slate-500 uppercase tracking-widest font-bold mb-1">{t('fish.waterType')}</span>
            <span className="text-lg font-bold text-slate-200">{detail.waterType || '—'}</span>
          </div>
          <div className="bg-[#202226] rounded-2xl p-5 shadow-lg border border-slate-800/80 flex flex-col items-center justify-center text-center hover:bg-[#25282d] transition-colors">
            <Ruler className="w-7 h-7 text-emerald-400 mb-3" />
            <span className="text-[11px] text-slate-500 uppercase tracking-widest font-bold mb-1">{t('fish.length')}</span>
            <span className="text-lg font-bold text-slate-200">{detail.length ? `${detail.length} cm` : '—'}</span>
          </div>
          <div className="bg-[#202226] rounded-2xl p-5 shadow-lg border border-slate-800/80 flex flex-col items-center justify-center text-center hover:bg-[#25282d] transition-colors">
            <Scale className="w-7 h-7 text-orange-400 mb-3" />
            <span className="text-[11px] text-slate-500 uppercase tracking-widest font-bold mb-1">{t('fish.weight')}</span>
            <span className="text-lg font-bold text-slate-200">{detail.weight ? `${detail.weight} kg` : '—'}</span>
          </div>
          <div className="bg-[#202226] rounded-2xl p-5 shadow-lg border border-slate-800/80 flex flex-col items-center justify-center text-center hover:bg-[#25282d] transition-colors">
            <Info className="w-7 h-7 text-purple-400 mb-3" />
            <span className="text-[11px] text-slate-500 uppercase tracking-widest font-bold mb-1">{t('fish.lifeCycle')}</span>
            <span className="text-lg font-bold text-slate-200 capitalize">{detail.lifeCycle || '—'}</span>
          </div>
        </div>

        {/* Ecology & Parameters */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {/* Environment */}
          <div className="bg-[#202226] rounded-2xl p-6 shadow-lg border border-slate-800/80">
            <h3 className="text-lg font-bold text-white mb-5 flex items-center gap-2">
              <div className="p-2 bg-sky-500/20 rounded-lg">
                <Droplets className="w-5 h-5 text-sky-400" />
              </div>
              {t('fish.waterParameters')}
            </h3>
            <div className="grid grid-cols-2 gap-4">
              <div className="bg-[#141518] rounded-xl p-5 border border-slate-800/50">
                <p className="text-[11px] text-slate-500 font-bold uppercase tracking-widest mb-2">Temperature</p>
                <p className="text-2xl font-black text-slate-200">
                  {detail.environment?.tempMin ?? '?'} - {detail.environment?.tempMax ?? '?'} <span className="text-sm font-medium text-slate-500">°C</span>
                </p>
              </div>
              <div className="bg-[#141518] rounded-xl p-5 border border-slate-800/50">
                <p className="text-[11px] text-slate-500 font-bold uppercase tracking-widest mb-2">pH Level</p>
                <p className="text-2xl font-black text-slate-200">
                  {detail.environment?.phMin ?? '?'} - {detail.environment?.phMax ?? '?'}
                </p>
              </div>
            </div>
          </div>

          {/* Ecology */}
          <div className="bg-[#202226] rounded-2xl p-6 shadow-lg border border-slate-800/80">
             <h3 className="text-lg font-bold text-white mb-5 flex items-center gap-2">
              <div className="p-2 bg-teal-500/20 rounded-lg">
                <Fish className="w-5 h-5 text-teal-400" />
              </div>
              {t('fish.ecology')}
            </h3>
            <div className="space-y-4">
              <div className="flex justify-between items-center bg-[#141518] p-4 rounded-xl border border-slate-800/50">
                <span className="text-sm text-slate-400 font-medium">{t('fish.feedingType')}</span>
                <span className="font-bold text-slate-200">{detail.ecology?.feedingType || '—'}</span>
              </div>
              <div className="flex justify-between items-center bg-[#141518] p-4 rounded-xl border border-slate-800/50">
                <span className="text-sm text-slate-400 font-medium">{t('fish.habitatZones')}</span>
                <span className="font-bold text-slate-200">
                  {detail.ecology?.habitatZones?.length ? detail.ecology.habitatZones.join(', ') : '—'}
                </span>
              </div>
               <div className="flex justify-between items-center bg-[#141518] p-4 rounded-xl border border-slate-800/50">
                <span className="text-sm text-slate-400 font-medium">{t('fish.iucnStatus')}</span>
                <span className="font-bold text-rose-400 bg-rose-500/10 px-3 py-1 rounded-lg border border-rose-500/20">
                  {detail.conservation?.iucnCode || 'Not Evaluated'}
                </span>
              </div>
            </div>
          </div>
        </div>

        {/* Gallery */}
        {media.length > 0 && (
          <div className="bg-[#202226] rounded-2xl p-6 shadow-lg border border-slate-800/80">
            <h3 className="text-lg font-bold text-white mb-5 flex items-center gap-2">
              <div className="p-2 bg-indigo-500/20 rounded-lg">
                <ImageIcon className="w-5 h-5 text-indigo-400" />
              </div>
              {t('fish.gallery')}
            </h3>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
              {media.slice(0, 8).map(m => (
                <div key={m.id} className="aspect-square rounded-xl overflow-hidden bg-[#141518] relative group border border-slate-800/50">
                  <img src={m.url || ''} alt={m.name} className="w-full h-full object-cover group-hover:scale-110 transition-transform duration-700" />
                  <div className="absolute bottom-0 w-full bg-black/60 p-2 text-xs font-medium text-white text-center backdrop-blur-md translate-y-full group-hover:translate-y-0 transition-transform duration-300">
                    {m.gender}
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* Map */}
        {occurrences.length > 0 && (
          <div className="bg-[#202226] rounded-2xl p-6 shadow-lg border border-slate-800/80">
            <h3 className="text-lg font-bold text-white mb-5 flex items-center gap-2">
              <div className="p-2 bg-green-500/20 rounded-lg">
                <Map className="w-5 h-5 text-green-400" />
              </div>
              {t('fish.occurrencesMap')}
            </h3>
            <div className="h-[450px] w-full rounded-xl overflow-hidden border border-slate-800/50 relative z-0">
              <MapContainer center={mapCenter} zoom={3} scrollWheelZoom={false} className="h-full w-full z-0" style={{ background: '#141518' }}>
                <TileLayer
                  attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
                  url="https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png"
                />
                {occurrences.map(occ => occ.latitudeDec && occ.longitudeDec && (
                  <Marker key={occ.id} position={[occ.latitudeDec, occ.longitudeDec]}>
                    <Popup>
                      <div className="text-slate-800 font-medium">
                        {occ.locality || occ.countryCode || 'Unknown location'}
                      </div>
                    </Popup>
                  </Marker>
                ))}
              </MapContainer>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}

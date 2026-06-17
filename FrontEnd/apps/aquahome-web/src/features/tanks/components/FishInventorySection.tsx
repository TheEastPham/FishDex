import { useEffect, useState, useMemo, useRef } from 'react';
import { useMap, MapContainer, TileLayer, CircleMarker, Popup } from 'react-leaflet';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import {
  getSpeciesSummaries, getSpeciesDistribution,
  getCached, setCached, CacheKeys, SPECIES_DATA_TTL, useTranslation,
} from '@fishlover/shared';
import type { AquariumFishDto, SpeciesSummary, SpeciesDistributionDto } from '@fishlover/shared';
import { Fish, Globe, MapPin, ExternalLink, Loader2 } from 'lucide-react';

// ── MapController: update bounds on point change without remounting MapContainer ──
function MapController({ points }: { points: Array<{ lat: number; lon: number }> }) {
  const map = useMap();
  useEffect(() => {
    if (!points.length) return;
    const bounds = L.latLngBounds(points.map(p => [p.lat, p.lon] as [number, number]));
    map.fitBounds(bounds, { padding: [30, 30], maxZoom: 8 });
  }, [points, map]);
  return null;
}

interface Props {
  aquariumId: string;
  fishList: AquariumFishDto[];
  loading: boolean;
  onNavigateFish: (specCode: number) => void;
}

export default function FishInventorySection({ aquariumId: _aquariumId, fishList, loading, onNavigateFish }: Props) {
  const { t } = useTranslation();
  const [summaries, setSummaries]         = useState<Record<number, SpeciesSummary>>({});
  const [distributions, setDistributions] = useState<Record<number, SpeciesDistributionDto>>({});
  const [selected, setSelected]           = useState<number | null>(null);
  const [summaryLoading, setSummaryLoading] = useState(false);
  const fetchedRef = useRef<string>(''); // track which aquarium's data was fetched

  // ── Batch fetch summaries + pre-fetch all distributions ──
  useEffect(() => {
    if (loading || fishList.length === 0) return;
    const key = fishList.map(f => f.specCode).sort().join(',');
    if (fetchedRef.current === key) return;
    fetchedRef.current = key;

    const specCodes = fishList.map(f => f.specCode);

    setSummaryLoading(true);
    getSpeciesSummaries(specCodes, 'vi')
      .then(list => {
        const map: Record<number, SpeciesSummary> = {};
        list.forEach(s => { map[s.specCode] = s; });
        setSummaries(map);
        setSelected(prev => prev ?? specCodes[0]);
      })
      .catch(console.error)
      .finally(() => setSummaryLoading(false));

    // Pre-fetch distributions in parallel, respecting 48h cache
    specCodes.forEach(specCode => {
      const cacheKey = CacheKeys.speciesDistribution(specCode);
      const cached = getCached<SpeciesDistributionDto>(cacheKey);
      if (cached) {
        setDistributions(prev => ({ ...prev, [specCode]: cached }));
        return;
      }
      getSpeciesDistribution(specCode)
        .then(data => {
          setCached(cacheKey, data, SPECIES_DATA_TTL);
          setDistributions(prev => ({ ...prev, [specCode]: data }));
        })
        .catch(() => {});
    });
  }, [loading, fishList]);

  // ── Map points: 1 point per country for selected fish ──
  const mapPoints = useMemo(() => {
    if (selected === null) return [];
    const dist = distributions[selected];
    if (!dist) return [];
    return dist.countries
      .filter(c => c.occurrences.length > 0)
      .map(c => ({
        lat: c.occurrences[0].lat,
        lon: c.occurrences[0].lon,
        countryName: c.name,
        count: c.count,
      }));
  }, [selected, distributions]);

  // ── Empty / loading states ──
  if (loading) {
    return (
      <div className="flex items-center justify-center py-16 text-slate-600">
        <Loader2 className="w-6 h-6 animate-spin" />
      </div>
    );
  }

  if (fishList.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center py-16 text-center rounded-2xl border border-dashed border-slate-800/60 bg-[#1E293B]/30">
        <div className="w-14 h-14 rounded-2xl bg-sky-500/10 border border-sky-500/20 flex items-center justify-center mb-3">
          <Fish className="w-7 h-7 text-sky-500/60" />
        </div>
        <p className="text-slate-500 text-sm">{t('aquarium.empty')}</p>
        <p className="text-slate-600 text-xs mt-1">{t('aquarium.emptyHint')}</p>
      </div>
    );
  }

  return (
    <div>
      <h3 className="text-sm font-bold text-slate-400 uppercase tracking-wider mb-4">
        {t('aquarium.fishListTitle')}
      </h3>

      <div className="flex gap-4 min-h-[480px]">

        {/* ── Left: Fish list (55%) ── */}
        <div className="w-[55%] flex flex-col gap-1 overflow-y-auto pr-1 max-h-[480px]">
          {summaryLoading && fishList.map(f => (
            <div key={f.specCode} className="flex items-center gap-3 px-3 py-2.5 rounded-xl bg-[#1E293B] border border-slate-800/60 animate-pulse">
              <div className="w-10 h-10 rounded-lg bg-slate-700/50 shrink-0" />
              <div className="flex-1 space-y-1.5">
                <div className="h-3.5 bg-slate-700/50 rounded w-2/3" />
                <div className="h-2.5 bg-slate-700/30 rounded w-1/2" />
              </div>
            </div>
          ))}

          {!summaryLoading && fishList.map(f => {
            const summary = summaries[f.specCode];
            const dist    = distributions[f.specCode];
            const isSelected = selected === f.specCode;

            return (
              <button
                key={f.specCode}
                onClick={() => setSelected(f.specCode)}
                className={`w-full flex items-center gap-3 px-3 py-2.5 rounded-xl border text-left transition-all ${
                  isSelected
                    ? 'bg-sky-500/10 border-sky-500/30 shadow-sm'
                    : 'bg-[#1E293B] border-slate-800/60 hover:bg-[#263348] hover:border-slate-700/60'
                }`}
              >
                {/* Fish image */}
                <div className="w-10 h-10 rounded-lg overflow-hidden bg-slate-800 shrink-0">
                  {summary?.imageUrl
                    ? <img src={summary.imageUrl} alt="" className="w-full h-full object-cover" />
                    : <div className="w-full h-full flex items-center justify-center"><Fish className="w-5 h-5 text-slate-600" /></div>
                  }
                </div>

                {/* Name */}
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-semibold text-white truncate leading-snug">
                    {summary?.commonName ?? summary?.speciesName ?? `SpecCode ${f.specCode}`}
                  </p>
                  {summary?.commonName && (
                    <p className="text-xs text-slate-500 italic truncate">{summary.speciesName}</p>
                  )}
                </div>

                {/* Quantity */}
                <span className="text-xs font-bold text-sky-300 bg-sky-500/10 border border-sky-500/20 px-2 py-0.5 rounded-full shrink-0">
                  ×{f.quantity}
                </span>

                {/* Countries */}
                <div className="flex items-center gap-1 text-xs text-slate-500 shrink-0 w-14 justify-end">
                  <Globe className="w-3 h-3" />
                  {dist ? dist.countries.length : <span className="opacity-40">—</span>}
                </div>

                {/* Occurrences */}
                <div className="flex items-center gap-1 text-xs text-slate-500 shrink-0 w-16 justify-end">
                  <MapPin className="w-3 h-3" />
                  {dist ? dist.totalOccurrences.toLocaleString() : <span className="opacity-40">—</span>}
                </div>

                {/* Navigate to detail */}
                <button
                  onClick={e => { e.stopPropagation(); onNavigateFish(f.specCode); }}
                  className="p-1.5 rounded-lg hover:bg-white/10 text-slate-500 hover:text-sky-400 transition-colors shrink-0"
                  title="Xem hồ sơ loài"
                >
                  <ExternalLink className="w-3.5 h-3.5" />
                </button>
              </button>
            );
          })}
        </div>

        {/* ── Right: Map (45%) ── */}
        <div className="w-[45%] rounded-2xl overflow-hidden border border-slate-800/60 bg-slate-900">
          {mapPoints.length === 0 && !distributions[selected ?? -1] && (
            <div className="h-full flex items-center justify-center text-slate-600 text-sm">
              <Loader2 className="w-5 h-5 animate-spin" />
            </div>
          )}
          {(mapPoints.length > 0 || distributions[selected ?? -1]) && (
            <MapContainer
              center={[20, 0]}
              zoom={2}
              style={{ height: '100%', width: '100%', minHeight: '480px' }}
              zoomControl={false}
            >
              <TileLayer
                url="https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png"
                attribution='&copy; <a href="https://carto.com/">CARTO</a>'
              />
              <MapController points={mapPoints} />
              {mapPoints.map((p, i) => (
                <CircleMarker
                  key={i}
                  center={[p.lat, p.lon]}
                  radius={6}
                  pathOptions={{ color: '#38bdf8', fillColor: '#38bdf8', fillOpacity: 0.7, weight: 1.5 }}
                >
                  <Popup>
                    <div className="text-xs">
                      <p className="font-bold">{p.countryName}</p>
                      <p className="text-slate-500">{p.count.toLocaleString()} ghi nhận</p>
                    </div>
                  </Popup>
                </CircleMarker>
              ))}
            </MapContainer>
          )}
        </div>
      </div>
    </div>
  );
}

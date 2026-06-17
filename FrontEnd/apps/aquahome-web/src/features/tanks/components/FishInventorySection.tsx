import { useEffect, useState, useMemo, useRef } from 'react';
import { useMap, MapContainer, TileLayer, CircleMarker, Popup } from 'react-leaflet';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import {
  getSpeciesSummaries, getSpeciesDistribution,
  getCached, setCached, CacheKeys, SPECIES_DATA_TTL, useTranslation,
} from '@fishlover/shared';
import type { AquariumFishDto, SpeciesSummary, SpeciesDistributionDto } from '@fishlover/shared';
import { Fish, Globe, ExternalLink, Loader2 } from 'lucide-react';

// ── Color palette — 10 distinct colors per fish ──────────────────────────────
const FISH_COLORS = [
  '#38bdf8', // sky
  '#34d399', // emerald
  '#f472b6', // pink
  '#fb923c', // orange
  '#a78bfa', // violet
  '#facc15', // yellow
  '#2dd4bf', // teal
  '#f87171', // red
  '#818cf8', // indigo
  '#86efac', // green
];

function getFishColor(index: number) {
  return FISH_COLORS[index % FISH_COLORS.length];
}

// ── MapController ─────────────────────────────────────────────────────────────
function MapController({ points }: { points: Array<{ lat: number; lon: number }> }) {
  const map = useMap();
  useEffect(() => {
    if (!points.length) {
      map.setView([20, 0], 2);
      return;
    }
    const bounds = L.latLngBounds(points.map(p => [p.lat, p.lon] as [number, number]));
    map.fitBounds(bounds, { padding: [30, 30], maxZoom: 8 });
  }, [points, map]);
  return null;
}

// ── HoverImagePopup ───────────────────────────────────────────────────────────
function HoverImagePopup({ imageUrl, name, x, y }: { imageUrl: string; name: string; x: number; y: number }) {
  return (
    <div
      className="fixed z-50 pointer-events-none"
      style={{ left: x + 16, top: y - 60 }}
    >
      <div className="bg-[#1E293B] border border-slate-700 rounded-xl shadow-2xl overflow-hidden w-40">
        <img src={imageUrl} alt={name} className="w-full h-24 object-cover" />
        <p className="text-xs font-semibold text-white px-2 py-1.5 truncate">{name}</p>
      </div>
    </div>
  );
}

interface Props {
  aquariumId: string;
  fishList: AquariumFishDto[];
  loading: boolean;
  onNavigateFish: (specCode: number) => void;
}

const FIXED_HEIGHT = 480;
const ROW_HEIGHT   = 40; // px — compact row
const VISIBLE_ROWS = 10;

export default function FishInventorySection({ aquariumId: _aquariumId, fishList, loading, onNavigateFish }: Props) {
  const { t } = useTranslation();
  const [summaries, setSummaries]           = useState<Record<number, SpeciesSummary>>({});
  const [distributions, setDistributions]   = useState<Record<number, SpeciesDistributionDto>>({});
  const [selected, setSelected]             = useState<number | null>(null);
  const [summaryLoading, setSummaryLoading] = useState(false);
  const [hover, setHover]                   = useState<{ specCode: number; x: number; y: number } | null>(null);
  const fetchedRef = useRef<string>('');

  // ── Assign stable color index per specCode ────────────────────────────────
  const colorMap = useMemo(() => {
    const map: Record<number, number> = {};
    fishList.forEach((f, i) => { map[f.specCode] = i; });
    return map;
  }, [fishList]);

  // ── Batch fetch ───────────────────────────────────────────────────────────
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

    specCodes.forEach(specCode => {
      const cacheKey = CacheKeys.speciesDistribution(specCode);
      const cached = getCached<SpeciesDistributionDto>(cacheKey);
      if (cached) { setDistributions(prev => ({ ...prev, [specCode]: cached })); return; }
      getSpeciesDistribution(specCode)
        .then(data => {
          setCached(cacheKey, data, SPECIES_DATA_TTL);
          setDistributions(prev => ({ ...prev, [specCode]: data }));
        })
        .catch(() => {});
    });
  }, [loading, fishList]);

  const selectedColor = selected != null ? getFishColor(colorMap[selected] ?? 0) : '#38bdf8';

  const mapPoints = useMemo(() => {
    if (selected === null) return [];
    const dist = distributions[selected];
    if (!dist) return [];
    return dist.countries
      .filter(c => c.occurrences.length > 0)
      .map(c => ({ lat: c.occurrences[0].lat, lon: c.occurrences[0].lon, countryName: c.name, count: c.count }));
  }, [selected, distributions]);

  const hoverSummary = hover ? summaries[hover.specCode] : null;

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

  const listScrollHeight = ROW_HEIGHT * VISIBLE_ROWS;

  return (
    <div>
      <h3 className="text-sm font-bold text-slate-400 uppercase tracking-wider mb-4">
        {t('aquarium.fishListTitle')}
      </h3>

      {/* Fixed-height container */}
      <div className="flex gap-3 rounded-2xl overflow-hidden border border-slate-800/60" style={{ height: FIXED_HEIGHT }}>

        {/* ── Left: Fish list 40% ── */}
        <div className="w-[40%] flex flex-col bg-[#111827]">
          {/* Column headers */}
          <div className="flex items-center gap-2 px-3 py-1.5 border-b border-slate-800/60 shrink-0">
            <span className="w-7 shrink-0" />
            <span className="flex-1 text-[10px] font-bold uppercase tracking-wider text-slate-600">Loài</span>
            <span className="text-[10px] font-bold uppercase tracking-wider text-slate-600 w-8 text-right shrink-0">SL</span>
            <span className="text-[10px] font-bold uppercase tracking-wider text-slate-600 w-8 text-right shrink-0">QG</span>
            <span className="w-5 shrink-0" />
          </div>
          <div
            className="overflow-y-auto flex-1 p-2 space-y-0.5"
            style={{ maxHeight: listScrollHeight }}
          >
            {summaryLoading && fishList.map(f => (
              <div key={f.specCode} className="flex items-center gap-2 px-2 py-1.5 rounded-lg animate-pulse" style={{ height: ROW_HEIGHT }}>
                <div className="w-7 h-7 rounded bg-slate-700/50 shrink-0" />
                <div className="flex-1 space-y-1">
                  <div className="h-2.5 bg-slate-700/50 rounded w-3/4" />
                  <div className="h-2 bg-slate-700/30 rounded w-1/2" />
                </div>
              </div>
            ))}

            {!summaryLoading && fishList.map(f => {
              const summary   = summaries[f.specCode];
              const dist      = distributions[f.specCode];
              const isSelected = selected === f.specCode;
              const color     = getFishColor(colorMap[f.specCode] ?? 0);

              return (
                <button
                  key={f.specCode}
                  onClick={() => setSelected(prev => prev === f.specCode ? null : f.specCode)}
                  onMouseLeave={() => setHover(null)}
                  className={`w-full flex items-center gap-2 px-2 rounded-lg text-left transition-all ${
                    isSelected ? 'bg-white/5' : 'hover:bg-white/5'
                  }`}
                  style={{ height: ROW_HEIGHT }}
                >
                  {/* Thumbnail + Name — hover zone for popup */}
                  <div
                    className="flex items-center gap-2 flex-1 min-w-0"
                    onMouseMove={e => {
                      if (summary?.imageUrl) setHover({ specCode: f.specCode, x: e.clientX, y: e.clientY });
                    }}
                  >
                    <div className="w-7 h-7 rounded overflow-hidden bg-slate-800 shrink-0">
                      {summary?.imageUrl
                        ? <img src={summary.imageUrl} alt="" className="w-full h-full object-cover" />
                        : <div className="w-full h-full flex items-center justify-center"><Fish className="w-3.5 h-3.5 text-slate-600" /></div>
                      }
                    </div>
                    <div className="min-w-0">
                      <p className="text-xs font-semibold text-white truncate leading-tight">
                        {summary?.commonName ?? summary?.speciesName ?? `#${f.specCode}`}
                      </p>
                      {summary?.commonName && (
                        <p className="text-[10px] text-slate-500 italic truncate">{summary.speciesName}</p>
                      )}
                    </div>
                  </div>

                  {/* Qty */}
                  <span className="text-[10px] font-bold px-1.5 py-0.5 rounded-full shrink-0"
                    style={{ background: `${color}22`, color }}>
                    ×{f.quantity}
                  </span>

                  {/* Countries */}
                  <div className="flex items-center gap-0.5 text-[10px] shrink-0 w-8 justify-end" style={{ color }}>
                    <Globe className="w-2.5 h-2.5" />
                    <span>{dist ? dist.countries.length : '—'}</span>
                  </div>

                  {/* Navigate */}
                  <button
                    onClick={e => { e.stopPropagation(); onNavigateFish(f.specCode); }}
                    className="p-1 rounded hover:bg-white/10 text-slate-600 hover:text-sky-400 transition-colors shrink-0"
                    title="Xem hồ sơ loài"
                  >
                    <ExternalLink className="w-3 h-3" />
                  </button>
                </button>
              );
            })}
          </div>
        </div>

        {/* ── Right: Map 60% ── */}
        {/* isolation:isolate keeps Leaflet's internal z-index from escaping this stacking context */}
        <div className="w-[60%] relative" style={{ isolation: 'isolate' }}>
          {/* Loading spinner only before first data arrives */}
          {summaryLoading && (
            <div className="absolute inset-0 flex items-center justify-center text-slate-600 bg-slate-900 z-10">
              <Loader2 className="w-5 h-5 animate-spin" />
            </div>
          )}
          {!summaryLoading && (
            <MapContainer
              center={[20, 0]}
              zoom={2}
              style={{ height: '100%', width: '100%' }}
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
                  radius={5}
                  pathOptions={{ color: selectedColor, fillColor: selectedColor, fillOpacity: 0.75, weight: 1.5 }}
                >
                  <Popup>
                    <div className="text-xs">
                      <p className="font-bold">{p.countryName}</p>
                      <p className="text-slate-500">{p.count.toLocaleString()}</p>
                    </div>
                  </Popup>
                </CircleMarker>
              ))}
            </MapContainer>
          )}
        </div>
      </div>

      {/* Hover image popup */}
      {hover && hoverSummary?.imageUrl && (
        <HoverImagePopup
          imageUrl={hoverSummary.imageUrl}
          name={hoverSummary.commonName ?? hoverSummary.speciesName}
          x={hover.x}
          y={hover.y}
        />
      )}
    </div>
  );
}

import { useEffect, useState, useMemo, useRef } from 'react';
import { useMap, MapContainer, TileLayer, CircleMarker, Popup } from 'react-leaflet';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import {
  getSpeciesSummaries, getSpeciesDistributionsBatch,
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

const ROW_HEIGHT = 40; // px — compact row

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
        // Keep current selection if valid, otherwise show all (null)
        setSelected(prev => (prev != null && specCodes.includes(prev)) ? prev : null);
      })
      .catch(console.error)
      .finally(() => setSummaryLoading(false));

    // Distribution: lấy từ cache trước, chỉ batch 1 request cho các loài chưa cache
    const missing: number[] = [];
    specCodes.forEach(specCode => {
      const cached = getCached<SpeciesDistributionDto>(CacheKeys.speciesDistribution(specCode));
      if (cached) setDistributions(prev => ({ ...prev, [specCode]: cached }));
      else missing.push(specCode);
    });

    if (missing.length > 0) {
      getSpeciesDistributionsBatch(missing)
        .then(batch => {
          Object.entries(batch).forEach(([code, data]) => {
            setCached(CacheKeys.speciesDistribution(Number(code)), data, SPECIES_DATA_TTL);
          });
          setDistributions(prev => ({
            ...prev,
            ...Object.fromEntries(Object.entries(batch).map(([code, data]) => [Number(code), data])),
          }));
        })
        .catch(() => {});
    }
  }, [loading, fishList]);

  const mapPoints = useMemo(() => {
    if (selected !== null) {
      const dist = distributions[selected];
      if (!dist) return [];
      const color = getFishColor(colorMap[selected] ?? 0);
      return dist.countries
        .filter(c => c.occurrences.length > 0)
        .map(c => ({ lat: c.occurrences[0].lat, lon: c.occurrences[0].lon, countryName: c.name, count: c.count, color }));
    }
    // Show all fish — each species gets its own color
    return Object.entries(distributions).flatMap(([specCodeStr, dist]) => {
      const color = getFishColor(colorMap[Number(specCodeStr)] ?? 0);
      return dist.countries
        .filter(c => c.occurrences.length > 0)
        .map(c => ({ lat: c.occurrences[0].lat, lon: c.occurrences[0].lon, countryName: c.name, count: c.count, color }));
    });
  }, [selected, distributions, colorMap]);

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

  return (
    <div>
      <h3 className="text-sm font-bold text-slate-400 uppercase tracking-wider mb-4">
        {t('aquarium.fishListTitle')}
      </h3>

      {/* Responsive container: mobile = col (map top, list bottom), desktop = row */}
      <div className="flex flex-col md:flex-row gap-3 rounded-2xl overflow-hidden border border-slate-800/60 md:h-[480px]">

        {/* ── Map — top on mobile, right on desktop ── */}
        {/* rendered first in DOM so it appears on top on mobile */}
        <div className="order-1 md:order-2 md:flex-1 h-[220px] md:h-full relative" style={{ isolation: 'isolate' }}>
          {summaryLoading && (
            <div className="absolute inset-0 flex items-center justify-center text-slate-600 bg-slate-900 z-10">
              <Loader2 className="w-5 h-5 animate-spin" />
            </div>
          )}
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
                pathOptions={{ color: p.color, fillColor: p.color, fillOpacity: 0.75, weight: 1.5 }}
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
        </div>

        {/* ── Fish list — bottom on mobile, left on desktop ── */}
        <div className="order-2 md:order-1 md:w-1/3 flex flex-col bg-[#111827]">
          {/* Column headers */}
          <div className="flex items-center gap-2 px-3 py-1.5 border-b border-slate-800/60 shrink-0">
            <span className="w-7 shrink-0" />
            <span className="flex-1 text-[10px] font-bold uppercase tracking-wider text-slate-600">{t('tanks_detail.colSpecies')}</span>
            <span className="text-[10px] font-bold uppercase tracking-wider text-slate-600 w-8 text-right shrink-0">{t('tanks_detail.colQty')}</span>
            <span className="text-[10px] font-bold uppercase tracking-wider text-slate-600 w-8 text-right shrink-0">{t('tanks_detail.colOrigin')}</span>
            <span className="w-5 shrink-0" />
          </div>
          {/* Show-all chip — only when a fish is selected */}
          {selected !== null && (
            <button
              onClick={() => setSelected(null)}
              className="mx-2 mt-1.5 flex items-center gap-1 px-2 py-0.5 rounded-full bg-sky-500/15 border border-sky-500/30 text-sky-400 text-[10px] font-semibold hover:bg-sky-500/25 transition-colors self-start shrink-0"
            >
              <span>✕</span> All
            </button>
          )}
          <div
            className="overflow-y-auto flex-1 p-2 space-y-0.5 max-h-[200px] md:max-h-[320px]"
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

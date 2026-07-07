import { useEffect, useState, useMemo } from 'react';
import { useMap, MapContainer, TileLayer, CircleMarker, Popup } from 'react-leaflet';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import { useTranslation } from '@fishlover/shared';
import type { SnapshotFishDto } from '@fishlover/shared';
import { Fish, Globe, ExternalLink } from 'lucide-react';

// ── Color palette — giống FishInventorySection ───────────────────────────────
const FISH_COLORS = [
  '#38bdf8', '#34d399', '#f472b6', '#fb923c', '#a78bfa',
  '#facc15', '#2dd4bf', '#f87171', '#818cf8', '#86efac',
];

function getFishColor(index: number) {
  return FISH_COLORS[index % FISH_COLORS.length];
}

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

interface Props {
  fish: SnapshotFishDto[];
  onNavigateFish: (specCode: number) => void;
}

const ROW_HEIGHT = 40; // px — compact row

/**
 * Fish list + world map cho public snapshot — dữ liệu denorm sẵn trong SnapshotData,
 * KHÔNG gọi API nào (khác FishInventorySection vốn tự fetch summaries/distributions).
 */
export default function SnapshotFishSection({ fish, onNavigateFish }: Props) {
  const { t } = useTranslation();
  const [selected, setSelected] = useState<number | null>(null);

  const colorMap = useMemo(() => {
    const map: Record<number, number> = {};
    fish.forEach((f, i) => { map[f.specCode] = i; });
    return map;
  }, [fish]);

  const mapPoints = useMemo(() => {
    const source = selected !== null ? fish.filter(f => f.specCode === selected) : fish;
    return source.flatMap(f => {
      const color = getFishColor(colorMap[f.specCode] ?? 0);
      return f.distributionPoints.map(p => ({
        lat: p.latitudeDec,
        lon: p.longitudeDec,
        locality: p.locality ?? p.countryCode ?? '',
        color,
      }));
    });
  }, [selected, fish, colorMap]);

  if (fish.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center py-16 text-center rounded-2xl border border-dashed border-slate-800/60 bg-[#1E293B]/30">
        <div className="w-14 h-14 rounded-2xl bg-sky-500/10 border border-sky-500/20 flex items-center justify-center mb-3">
          <Fish className="w-7 h-7 text-sky-500/60" />
        </div>
        <p className="text-slate-500 text-sm">{t('aquarium.empty')}</p>
      </div>
    );
  }

  return (
    <div>
      <h3 className="text-sm font-bold text-slate-400 uppercase tracking-wider mb-4">
        {t('aquarium.fishListTitle')}
      </h3>

      {/* Responsive: mobile = col (map top, list bottom), desktop = row */}
      <div className="flex flex-col md:flex-row gap-3 rounded-2xl overflow-hidden border border-slate-800/60 md:h-[480px]">

        {/* ── Map ── */}
        <div className="order-1 md:order-2 md:flex-1 h-[220px] md:h-full relative" style={{ isolation: 'isolate' }}>
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
                {p.locality && (
                  <Popup>
                    <div className="text-xs"><p className="font-bold">{p.locality}</p></div>
                  </Popup>
                )}
              </CircleMarker>
            ))}
          </MapContainer>
        </div>

        {/* ── Fish list ── */}
        <div className="order-2 md:order-1 md:w-1/3 flex flex-col bg-[#111827]">
          <div className="flex items-center gap-2 px-3 py-1.5 border-b border-slate-800/60 shrink-0">
            <span className="w-7 shrink-0" />
            <span className="flex-1 text-[10px] font-bold uppercase tracking-wider text-slate-600">{t('tanks_detail.colSpecies')}</span>
            <span className="text-[10px] font-bold uppercase tracking-wider text-slate-600 w-8 text-right shrink-0">{t('tanks_detail.colQty')}</span>
            <span className="w-5 shrink-0" />
          </div>
          {selected !== null && (
            <button
              onClick={() => setSelected(null)}
              className="mx-2 mt-1.5 flex items-center gap-1 px-2 py-0.5 rounded-full bg-sky-500/15 border border-sky-500/30 text-sky-400 text-[10px] font-semibold hover:bg-sky-500/25 transition-colors self-start shrink-0"
            >
              <span>✕</span> All
            </button>
          )}
          <div className="overflow-y-auto flex-1 p-2 space-y-0.5 max-h-[200px] md:max-h-[320px]">
            {fish.map(f => {
              const isSelected = selected === f.specCode;
              const color = getFishColor(colorMap[f.specCode] ?? 0);

              return (
                <button
                  key={f.specCode}
                  onClick={() => setSelected(prev => prev === f.specCode ? null : f.specCode)}
                  className={`w-full flex items-center gap-2 px-2 rounded-lg text-left transition-all ${
                    isSelected ? 'bg-white/5' : 'hover:bg-white/5'
                  }`}
                  style={{ height: ROW_HEIGHT }}
                >
                  <div className="w-7 h-7 rounded overflow-hidden bg-slate-800 shrink-0">
                    {f.imageUrl
                      ? <img src={f.imageUrl} alt="" className="w-full h-full object-cover" />
                      : <div className="w-full h-full flex items-center justify-center"><Fish className="w-3.5 h-3.5 text-slate-600" /></div>
                    }
                  </div>
                  <div className="min-w-0 flex-1">
                    <p className="text-xs font-semibold text-white truncate leading-tight">
                      {f.commonName ?? f.speciesName}
                    </p>
                    {f.commonName && (
                      <p className="text-[10px] text-slate-500 italic truncate">{f.speciesName}</p>
                    )}
                  </div>

                  <span className="text-[10px] font-bold px-1.5 py-0.5 rounded-full shrink-0"
                    style={{ background: `${color}22`, color }}>
                    ×{f.quantity}
                  </span>

                  <div className="flex items-center gap-0.5 text-[10px] shrink-0 justify-end" style={{ color }}>
                    <Globe className="w-2.5 h-2.5" />
                    <span>{f.distributionPoints.length}</span>
                  </div>

                  <button
                    onClick={e => { e.stopPropagation(); onNavigateFish(f.specCode); }}
                    className="p-1 rounded hover:bg-white/10 text-slate-600 hover:text-sky-400 transition-colors shrink-0"
                    title={t('publicTanks.viewSpecies')}
                  >
                    <ExternalLink className="w-3 h-3" />
                  </button>
                </button>
              );
            })}
          </div>
        </div>
      </div>
    </div>
  );
}

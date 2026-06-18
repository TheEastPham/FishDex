import { cn, WaterType } from '@fishlover/shared';
import type { AquariumDto } from '@fishlover/shared';
import { Droplets, Fish, FlaskConical, Ruler, Pencil, Trash2 } from 'lucide-react';

const WATER_TYPE_STYLES: Record<number, { bg: string; text: string; border: string; label: string }> = {
  [WaterType.Freshwater]: { bg: 'bg-emerald-500/10', text: 'text-emerald-400', border: 'border-emerald-500/20', label: 'Nước ngọt' },
  [WaterType.Saltwater]:  { bg: 'bg-sky-500/10',     text: 'text-sky-400',     border: 'border-sky-500/20',     label: 'Nước mặn' },
  [WaterType.Brackish]:   { bg: 'bg-teal-500/10',    text: 'text-teal-400',    border: 'border-teal-500/20',    label: 'Lợ'       },
};

const DEFAULT_STYLE = { bg: 'bg-slate-700/20', text: 'text-slate-400', border: 'border-slate-700/30', label: 'Khác' };

export function getTankStyle(waterType: WaterType | null) {
  if (waterType == null) return DEFAULT_STYLE;
  return WATER_TYPE_STYLES[waterType] ?? DEFAULT_STYLE;
}

interface Props {
  tank: AquariumDto;
  onEdit: (tank: AquariumDto) => void;
  onDelete: (id: string) => void;
}

export default function AquariumCard({ tank, onEdit, onDelete }: Props) {
  const style = getTankStyle(tank.waterType);

  return (
    <div className="bg-[#1E293B] border border-slate-800/60 rounded-2xl p-5 hover:bg-[#263348] hover:-translate-y-1 hover:shadow-2xl hover:border-slate-700/50 transition-all duration-300 group flex flex-col">

      {/* Top row */}
      <div className="flex items-start justify-between mb-4">
        <div className={cn('p-2.5 rounded-xl border shrink-0', style.bg, style.border)}>
          <Droplets className={cn('w-5 h-5', style.text)} />
        </div>
        <div className="flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
          <button
            onClick={(e) => { e.stopPropagation(); onEdit(tank); }}
            className="p-1.5 bg-white/5 hover:bg-white/10 rounded-lg transition-colors"
            title="Chỉnh sửa"
          >
            <Pencil className="w-3.5 h-3.5 text-slate-400" />
          </button>
          <button
            onClick={(e) => { e.stopPropagation(); onDelete(tank.id); }}
            className="p-1.5 bg-red-500/10 hover:bg-red-500/20 rounded-lg transition-colors"
            title="Xóa"
          >
            <Trash2 className="w-3.5 h-3.5 text-red-400" />
          </button>
        </div>
      </div>

      {/* Name & Type */}
      <h3 className="font-bold text-white text-lg mb-2 leading-tight">{tank.name}</h3>
      {tank.waterType != null && (
        <span className={cn('text-[11px] font-bold uppercase tracking-wider px-2.5 py-1 rounded-full border w-fit mb-3', style.bg, style.text, style.border)}>
          {style.label}
        </span>
      )}

      {/* Description */}
      {tank.description && (
        <p className="text-slate-500 text-sm leading-relaxed line-clamp-2 mb-3">{tank.description}</p>
      )}

      {/* Stats */}
      <div className="mt-auto space-y-2 pt-3 border-t border-slate-800/50">
        {tank.volumeLiters != null && (
          <div className="flex items-center justify-between text-sm">
            <span className="flex items-center gap-1.5 text-slate-500">
              <FlaskConical className="w-3.5 h-3.5" /> Thể tích
            </span>
            <span className="font-bold text-slate-200">{tank.volumeLiters.toFixed(1)} L</span>
          </div>
        )}
        {(tank.lengthCm || tank.widthCm || tank.heightCm) && (
          <div className="flex items-center justify-between text-sm">
            <span className="flex items-center gap-1.5 text-slate-500">
              <Ruler className="w-3.5 h-3.5" /> Kích thước
            </span>
            <span className="font-mono text-xs text-slate-400">
              {tank.lengthCm ?? '?'} × {tank.widthCm ?? '?'} × {tank.heightCm ?? '?'} cm
            </span>
          </div>
        )}
        <div className="flex items-center justify-between text-sm">
          <span className="flex items-center gap-1.5 text-slate-500">
            <Fish className="w-3.5 h-3.5" /> Cư dân
          </span>
          <span className="font-bold text-slate-200">{tank.fishCount} con</span>
        </div>
      </div>
    </div>
  );
}

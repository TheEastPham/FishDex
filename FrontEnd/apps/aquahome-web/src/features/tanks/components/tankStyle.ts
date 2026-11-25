import { WaterType } from '@fishlover/shared';

/** Màu theo loại nước của bể (chấm màu trên tab ở TanksPage). */
export interface TankStyle {
  bg: string;
  text: string;
  border: string;
}

const WATER_TYPE_STYLES: Record<number, TankStyle> = {
  [WaterType.Freshwater]: { bg: 'bg-emerald-500/10', text: 'text-emerald-400', border: 'border-emerald-500/20' },
  [WaterType.Saltwater]:  { bg: 'bg-sky-500/10',     text: 'text-sky-400',     border: 'border-sky-500/20' },
  [WaterType.Brackish]:   { bg: 'bg-teal-500/10',    text: 'text-teal-400',    border: 'border-teal-500/20' },
};

const DEFAULT_STYLE: TankStyle = { bg: 'bg-slate-700/20', text: 'text-slate-400', border: 'border-slate-700/30' };

export function getTankStyle(waterType: WaterType | null): TankStyle {
  if (waterType == null) return DEFAULT_STYLE;
  return WATER_TYPE_STYLES[waterType] ?? DEFAULT_STYLE;
}

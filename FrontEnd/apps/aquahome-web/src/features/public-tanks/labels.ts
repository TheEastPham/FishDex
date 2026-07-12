import { WaterType, AquariumStyle, PrizeTierLevel } from '@fishlover/shared';
import type { TFunction } from 'i18next';

/**
 * Fallback tên hiển thị khi không có snapshotData (gallery card chỉ có slug).
 * Slug dạng `{tên-bể}-{nickname}` (có thể kèm `-{n}` nếu trùng) — bỏ hậu tố số, dash → space, title-case.
 */
export function displayNameFromSlug(slug: string): string {
  const base = slug.replace(/-\d+$/, '');
  return base.replace(/-/g, ' ').replace(/\b\w/g, c => c.toUpperCase());
}

export function waterTypeLabel(t: TFunction, waterType: WaterType | null): string | null {
  switch (waterType) {
    case WaterType.Freshwater: return t('tanks.wt_freshwater');
    case WaterType.Saltwater:  return t('tanks.wt_saltwater');
    case WaterType.Brackish:   return t('tanks.wt_brackish');
    default: return null;
  }
}

// Style names là tên riêng quốc tế (Iwagumi, Dutch...) — không dịch, giống AquariumDetail
export const STYLE_LABELS: Record<number, string> = {
  [AquariumStyle.Nature]:     'Nature',
  [AquariumStyle.Dutch]:      'Dutch',
  [AquariumStyle.Iwagumi]:    'Iwagumi',
  [AquariumStyle.Biotope]:    'Biotope',
  [AquariumStyle.Reef]:       'Reef',
  [AquariumStyle.Blackwater]: 'Blackwater',
  [AquariumStyle.Community]:  'Community',
  [AquariumStyle.Predator]:   'Predator',
  [AquariumStyle.Paludarium]: 'Paludarium',
};

/** Tên hạng giải do admin tự đặt (vd "Giải Nhất") — hiển thị thẳng, không dịch qua i18n. */
export function awardBadgeStyle(level: PrizeTierLevel | null): string {
  switch (level) {
    case PrizeTierLevel.Gold:          return 'bg-amber-500/90 text-amber-950';
    case PrizeTierLevel.Silver:        return 'bg-slate-300/90 text-slate-900';
    case PrizeTierLevel.Bronze:        return 'bg-orange-700/90 text-orange-50';
    case PrizeTierLevel.Encouragement: return 'bg-sky-500/90 text-sky-950';
    default:                           return 'bg-white/20 text-white';
  }
}

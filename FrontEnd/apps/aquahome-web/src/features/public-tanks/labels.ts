import { WaterType, AquariumStyle, ContestAward } from '@fishlover/shared';
import type { TFunction } from 'i18next';

/** Tên hiển thị lấy từ slug `{aquariumName}-{rand4}` — bỏ suffix random, dash → space */
export function displayNameFromSlug(slug: string): string {
  const base = slug.replace(/-[a-z0-9]{4}$/, '');
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

export function awardLabel(t: TFunction, award: ContestAward | null): string | null {
  switch (award) {
    case ContestAward.Winner:      return t('publicTanks.awardWinner');
    case ContestAward.Top3:        return t('publicTanks.awardTop3');
    case ContestAward.Participant: return t('publicTanks.awardParticipant');
    default: return null;
  }
}

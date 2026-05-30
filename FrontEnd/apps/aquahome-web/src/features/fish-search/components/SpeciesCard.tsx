import type { SpeciesSearchResult } from '@fishlover/shared';
import { cn, useTranslation } from '@fishlover/shared';
import { Fish } from 'lucide-react';

interface Props {
  species: SpeciesSearchResult;
  onAddToAquarium?: (species: SpeciesSearchResult) => void;
  /** AquaHome Service not ready — disabled until Story 5.2 */
  aquariumReady?: boolean;
  index?: number;
}

const GRADIENTS = [
  'from-blue-400 to-indigo-500',
  'from-emerald-400 to-teal-500',
  'from-cyan-400 to-blue-500',
  'from-indigo-400 to-purple-500',
  'from-sky-400 to-indigo-400',
];

export default function SpeciesCard({ species, onAddToAquarium, aquariumReady = false, index = 0 }: Props) {
  const { t } = useTranslation();
  const bgGradient = GRADIENTS[index % GRADIENTS.length];

  return (
    <div className="group relative flex flex-col rounded-2xl bg-white shadow-sm border border-slate-200/60 overflow-hidden hover:shadow-xl hover:-translate-y-1 hover:border-primary/30 transition-all duration-300">

      {/* Image Placeholder */}
      <div className={cn("h-40 w-full bg-gradient-to-br relative overflow-hidden", bgGradient)}>
        <div className="absolute inset-0 bg-white/10 backdrop-blur-sm" />
        {/* Abstract shapes */}
        <div className="absolute -bottom-8 -right-8 w-24 h-24 rounded-full bg-white/20 blur-xl group-hover:scale-150 transition-transform duration-700" />
        <div className="absolute top-4 left-4 w-12 h-12 rounded-full bg-white/10 blur-md" />
        
        <div className="absolute inset-0 flex items-center justify-center opacity-30 group-hover:opacity-50 transition-opacity duration-300">
          <Fish className="w-16 h-16 text-white stroke-[1.5]" />
        </div>
      </div>

      <div className="flex flex-col flex-1 p-5 gap-3">
        {/* Scientific name */}
        <div>
          <p className="text-[11px] font-bold text-slate-400 uppercase tracking-wider mb-1">
            {species.familyName ?? '—'}
          </p>
          <h3 className="font-semibold text-lg text-slate-800 italic leading-snug line-clamp-1">
            {species.speciesName}
          </h3>
        </div>

        {/* Common name badge */}
        <div className="min-h-[24px]">
          {species.preferredCommonName && (
            <span className="inline-flex items-center rounded-md bg-primary/10 px-2.5 py-1 text-xs font-semibold text-primary">
              {species.preferredCommonName}
            </span>
          )}
        </div>

        {/* Genus */}
        <div className="mt-auto pt-2 pb-3 border-t border-slate-50 border-dashed">
          {species.genusName ? (
            <p className="text-xs text-slate-500 flex justify-between">
              <span>{t('fish.genus')}:</span>
              <span className="font-medium text-slate-700">{species.genusName}</span>
            </p>
          ) : (
            <p className="text-xs text-slate-400 italic">No genus data</p>
          )}
        </div>

        {/* Action */}
        <div className="flex gap-2 mt-auto pt-1">
          <button
            className="flex-1 rounded-xl px-4 py-2.5 text-sm font-semibold text-primary bg-primary/10 hover:bg-primary hover:text-white transition-all duration-200 active:scale-[0.98]"
          >
            {t('fish.detail')}
          </button>
          <button
            onClick={() => onAddToAquarium?.(species)}
            disabled={!aquariumReady}
            className={cn(
              'w-10 h-10 flex flex-shrink-0 items-center justify-center rounded-xl transition-all duration-200 active:scale-[0.98]',
              aquariumReady
                ? 'bg-primary text-white hover:bg-primary/90 hover:shadow-md hover:shadow-primary/20'
                : 'bg-slate-100 text-slate-400 cursor-not-allowed'
            )}
            title={aquariumReady ? undefined : t('fish.addToAquariumDisabledTip')}
          >
            <span className="text-xl font-bold leading-none">+</span>
          </button>
        </div>
      </div>
    </div>
  );
}

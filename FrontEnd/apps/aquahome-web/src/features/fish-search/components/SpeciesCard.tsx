import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import type { SpeciesSearchResult } from '@fishlover/shared';
import { cn, useTranslation } from '@fishlover/shared';
import { Fish, Heart, Folder, ExternalLink, Share2, Eye } from 'lucide-react';

interface Props {
  species: SpeciesSearchResult;
  index?: number;
  onFamilyClick?: (familyName: string) => void;
}

const GRADIENTS = [
  'from-slate-700 to-slate-900',
  'from-zinc-700 to-zinc-900',
  'from-stone-700 to-stone-900',
];

export default function SpeciesCard({ species, index = 0, onFamilyClick }: Props) {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [isFavorite, setIsFavorite] = useState(false);
  const bgGradient = GRADIENTS[index % GRADIENTS.length];

  return (
    <div className="group relative flex flex-col rounded-xl bg-[#202226] border border-slate-800/80 overflow-hidden hover:shadow-2xl hover:shadow-black/60 hover:-translate-y-1 transition-all duration-300">

      {/* Image Area */}
      <div className="h-[170px] w-full relative overflow-hidden bg-slate-900">
        {species.imageUrl ? (
          <img
            src={species.imageUrl}
            alt={species.speciesName}
            className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-700"
          />
        ) : (
          <div className={cn("absolute inset-0 bg-gradient-to-br opacity-80", bgGradient)}>
            <div className="absolute inset-0 flex items-center justify-center opacity-30 group-hover:opacity-50 transition-opacity duration-300">
              <Fish className="w-16 h-16 text-white stroke-[1.5]" />
            </div>
          </div>
        )}

        {/* Favorite Heart */}
        <button 
          onClick={(e) => { e.stopPropagation(); setIsFavorite(!isFavorite); }}
          className="absolute top-2 right-2 p-2 rounded-full hover:bg-black/20 transition-colors group/heart"
          title={t('fish.addToFavorites')}
        >
          <Heart className={cn("w-6 h-6 transition-colors drop-shadow-md", isFavorite ? "fill-white text-white" : "text-white/80 group-hover/heart:text-white")} />
        </button>
      </div>

      {/* Content Area */}
      <div className="flex flex-col flex-1 px-4 pt-3 pb-4">
        {/* Header row */}
        <div className="flex items-center justify-between text-slate-300 mb-2.5">
          <button 
            onClick={() => onFamilyClick?.(species.familyName!)}
            className="flex items-center gap-1.5 hover:text-white transition-colors group/fam"
            title={species.familyName ? t('fish.viewFamily', { family: species.familyName }) : undefined}
          >
            <Folder className="w-4 h-4" />
            <span className="text-sm font-medium capitalize truncate max-w-[120px]">{species.familyName || t('fish.unknownFamily')}</span>
            {species.familyName && <ExternalLink className="w-3 h-3 opacity-0 group-hover/fam:opacity-100 transition-opacity" />}
          </button>
          <button className="hover:text-white transition-colors p-1" title={t('fish.share')}>
            <Share2 className="w-4 h-4" />
          </button>
        </div>

        {/* Divider */}
        <div className="w-full h-px bg-gradient-to-r from-[#f9e5b9]/60 via-[#f9e5b9]/20 to-transparent mb-3" />

        {/* Names */}
        <div className="flex flex-col items-center text-center gap-1 mb-4">
          {species.preferredCommonName ? (
            <>
              <h3 className="text-[17px] font-bold text-[#f9e5b9] tracking-wide leading-snug line-clamp-1">
                {species.preferredCommonName}
              </h3>
              <p className="text-[13px] text-slate-300 italic font-light line-clamp-1">
                {species.speciesName}
              </p>
            </>
          ) : (
            <h3 className="text-[17px] font-bold text-[#f9e5b9] italic tracking-wide leading-snug line-clamp-1">
              {species.speciesName}
            </h3>
          )}
        </div>

        {/* Spacer */}
        <div className="flex-1" />

        {/* Action Button Row */}
        <div className="flex mt-auto overflow-hidden rounded-lg border border-[#1a1c20]">
          <button
            onClick={() => navigate(`/fish/${species.specCode}`)}
            className="flex-1 flex items-center justify-center bg-[#2a2d32] hover:bg-[#32363c] text-white py-2 px-3 text-sm font-bold transition-colors"
          >
            {t('fish.viewProfile')}
          </button>
          <div className="w-[1px] bg-[#1a1c20]" />
          <button
            onClick={() => navigate(`/fish/${species.specCode}`)}
            className="flex items-center justify-center bg-[#1a1c20] hover:bg-black py-2 px-3 transition-colors text-white"
            title={t('fish.viewProfileDetails')}
          >
            <Eye className="w-[18px] h-[18px]" />
          </button>
        </div>
      </div>
    </div>
  );
}

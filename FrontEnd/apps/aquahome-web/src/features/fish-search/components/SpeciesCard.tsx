import { useState, useEffect, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import type { SpeciesSearchResult, AquariumDto, FavoriteDto } from '@fishlover/shared';
import {
  cn, useTranslation, useAuthStore,
  addFavorite, removeFavorite, checkFavorite, addFishToAquarium,
  getCached, setCached, CacheKeys, FAVORITE_CHECK_TTL,
} from '@fishlover/shared';
import { Fish, Heart, Folder, ExternalLink, Share2, Plus, Loader2, Droplets, Minus } from 'lucide-react';

interface Props {
  species: SpeciesSearchResult;
  index?: number;
  onFamilyClick?: (familyName: string) => void;
  aquariums?: AquariumDto[];
}

const GRADIENTS = [
  'from-slate-700 to-slate-900',
  'from-zinc-700 to-zinc-900',
  'from-stone-700 to-stone-900',
];

export default function SpeciesCard({ species, index = 0, onFamilyClick, aquariums = [] }: Props) {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const bgGradient = GRADIENTS[index % GRADIENTS.length];
  const [loginHint, setLoginHint] = useState(false);

  // ── Favorites ────────────────────────────────────────────
  const [isFavorite, setIsFavorite] = useState<boolean>(false);
  const [favLoading, setFavLoading] = useState(false);

  useEffect(() => {
    if (!isAuthenticated) return;

    // 1. Individual check cache — highest priority (handles in-session toggles)
    const individual = getCached<boolean>(CacheKeys.favoriteCheck(species.specCode));
    if (individual !== null) { setIsFavorite(individual); return; }

    // 2. Bulk favorites cache — resolve locally, no API call
    const allFavs = getCached<FavoriteDto[]>(CacheKeys.myFavorites());
    if (allFavs !== null) {
      const isFav = allFavs.some(f => f.specCode === species.specCode);
      setCached(CacheKeys.favoriteCheck(species.specCode), isFav, FAVORITE_CHECK_TTL);
      setIsFavorite(isFav);
      return;
    }

    // 3. Fallback — individual API call (only when nothing cached)
    checkFavorite(species.specCode).then((v) => {
      setCached(CacheKeys.favoriteCheck(species.specCode), v, FAVORITE_CHECK_TTL);
      setIsFavorite(v);
    }).catch(() => {});
  }, [isAuthenticated, species.specCode]);

  const handleToggleFavorite = async (e: React.MouseEvent) => {
    e.stopPropagation();
    if (!isAuthenticated) { navigate('/login'); return; }
    const next = !isFavorite;
    setIsFavorite(next);
    setFavLoading(true);
    try {
      if (next) await addFavorite(species.specCode);
      else await removeFavorite(species.specCode);
      setCached(CacheKeys.favoriteCheck(species.specCode), next, FAVORITE_CHECK_TTL);
    } catch {
      setIsFavorite(!next); // rollback
    } finally {
      setFavLoading(false);
    }
  };

  // ── Add to Aquarium ──────────────────────────────────────
  const [showPicker, setShowPicker] = useState(false);
  const [qty, setQty] = useState(1);
  const [addingId, setAddingId] = useState<string | null>(null);
  const pickerRef = useRef<HTMLDivElement>(null);
  const addingRef = useRef(false); // synchronous lock — state update is async, ref is not

  useEffect(() => {
    if (!showPicker) { setQty(1); return; }
    function onClickOutside(e: MouseEvent) {
      if (pickerRef.current && !pickerRef.current.contains(e.target as Node)) {
        setShowPicker(false);
      }
    }
    document.addEventListener('mousedown', onClickOutside);
    return () => document.removeEventListener('mousedown', onClickOutside);
  }, [showPicker]);

  const handleAddToAquarium = async (e: React.MouseEvent, aquariumId: string) => {
    e.stopPropagation();
    if (addingRef.current) return;
    addingRef.current = true; // lock immediately — before any await or setState
    setAddingId(aquariumId);
    setShowPicker(false);     // close picker right away to prevent double-select
    try {
      await addFishToAquarium(aquariumId, species.specCode, qty);
    } catch {
      // silently fail
    } finally {
      addingRef.current = false;
      setAddingId(null);
    }
  };

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
          <div className={cn('absolute inset-0 bg-gradient-to-br opacity-80', bgGradient)}>
            <div className="absolute inset-0 flex items-center justify-center opacity-30 group-hover:opacity-50 transition-opacity duration-300">
              <Fish className="w-16 h-16 text-white stroke-[1.5]" />
            </div>
          </div>
        )}

        {/* Favorite Heart */}
        <button
          onClick={handleToggleFavorite}
          disabled={favLoading}
          className="absolute top-2 right-2 p-2 rounded-full hover:bg-black/20 transition-colors group/heart"
          title={t('fish.addToFavorites')}
        >
          {favLoading
            ? <Loader2 className="w-5 h-5 text-white animate-spin drop-shadow-md" />
            : <Heart className={cn('w-6 h-6 transition-colors drop-shadow-md', isFavorite ? 'fill-rose-400 text-rose-400' : 'text-white/80 group-hover/heart:text-white')} />
          }
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

        <div className="flex-1" />

        {/* Action Row */}
        <div className="flex mt-auto gap-2">
          {/* View Profile */}
          <button
            onClick={() => {
              if (!isAuthenticated) { setLoginHint(true); return; }
              navigate(`/fish/${species.specCode}`);
            }}
            className="flex-1 flex items-center justify-center bg-[#2a2d32] hover:bg-[#32363c] text-white py-2 px-3 text-sm font-bold rounded-lg transition-colors"
          >
            {t('fish.viewProfile')}
          </button>

          {/* Add to Aquarium */}
          {isAuthenticated && (
            <div className="relative" ref={pickerRef}>
              <button
                onClick={(e) => { e.stopPropagation(); setShowPicker((v) => !v); }}
                className="flex items-center justify-center bg-[#2a2d32] hover:bg-primary/20 hover:border-primary/40 border border-transparent text-slate-300 hover:text-primary py-2 px-3 rounded-lg transition-colors"
                title={t('fish.addToAquarium')}
              >
                <Plus className="w-4 h-4" />
              </button>

              {/* Aquarium Picker */}
              {showPicker && (
                <div className="absolute bottom-full right-0 mb-2 w-56 bg-[#1E293B] border border-slate-700 rounded-xl shadow-2xl z-50 overflow-hidden animate-in fade-in slide-in-from-bottom-2">
                  <p className="text-[11px] font-bold text-slate-400 uppercase tracking-wider px-3 py-2 border-b border-slate-800">
                    {t('fish.addToAquarium')}
                  </p>

                  {/* Quantity picker */}
                  <div className="flex items-center justify-between px-3 py-2 border-b border-slate-800/60">
                    <span className="text-xs text-slate-400">Số lượng</span>
                    <div className="flex items-center gap-1">
                      <button
                        onClick={(e) => { e.stopPropagation(); setQty(q => Math.max(1, q - 1)); }}
                        className="w-6 h-6 flex items-center justify-center rounded bg-slate-700 hover:bg-slate-600 text-slate-300 transition-colors"
                      >
                        <Minus className="w-3 h-3" />
                      </button>
                      <span className="w-7 text-center text-sm font-bold text-white tabular-nums">{qty}</span>
                      <button
                        onClick={(e) => { e.stopPropagation(); setQty(q => Math.min(99, q + 1)); }}
                        className="w-6 h-6 flex items-center justify-center rounded bg-slate-700 hover:bg-slate-600 text-slate-300 transition-colors"
                      >
                        <Plus className="w-3 h-3" />
                      </button>
                    </div>
                  </div>

                  {aquariums.length === 0 ? (
                    <p className="text-xs text-slate-500 px-3 py-3 text-center">Chưa có hồ cá nào</p>
                  ) : (
                    <div className="max-h-44 overflow-y-auto">
                      {aquariums.map((aq) => {
                        const isLoading = addingId === aq.id;
                        return (
                          <button
                            key={aq.id}
                            onClick={(e) => handleAddToAquarium(e, aq.id)}
                            disabled={!!addingId}
                            className="w-full flex items-center gap-2.5 px-3 py-2.5 text-left text-sm text-slate-300 hover:bg-white/5 hover:text-white transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                          >
                            {isLoading
                              ? <Loader2 className="w-3.5 h-3.5 shrink-0 animate-spin" />
                              : <Droplets className="w-3.5 h-3.5 shrink-0 text-slate-500" />
                            }
                            <span className="truncate flex-1">{aq.name}</span>
                          </button>
                        );
                      })}
                    </div>
                  )}
                </div>
              )}
            </div>
          )}
        </div>

        {/* Chưa login → nhắc đăng nhập thay vì vào trang chi tiết (BE chưa có endpoint public cho detail) */}
        {loginHint && (
          <div className="mt-3 flex items-center justify-between gap-2 rounded-lg border border-amber-500/30 bg-amber-500/10 px-3 py-2 text-xs text-amber-300">
            <span>{t('fish.loginToViewDetail')}</span>
            <button
              onClick={() => navigate('/login')}
              className="shrink-0 px-2.5 py-1 rounded-md bg-amber-500/20 hover:bg-amber-500/30 border border-amber-500/40 text-amber-200 font-semibold transition-colors"
            >
              {t('login.button')}
            </button>
          </div>
        )}
      </div>
    </div>
  );
}

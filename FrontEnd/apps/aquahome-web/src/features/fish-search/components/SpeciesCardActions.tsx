import { useState, useEffect, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import type { AquariumDto, FavoriteDto } from '@fishlover/shared';
import {
  cn, useTranslation, useAuthStore,
  addFavorite, removeFavorite, checkFavorite, addFishToAquarium,
  getCached, setCached, CacheKeys, FAVORITE_CHECK_TTL,
} from '@fishlover/shared';
import { Heart, Plus, Loader2, Droplets, Minus } from 'lucide-react';

/**
 * Hai action dùng chung cho mọi thẻ loài — thẻ tra cứu khoa học và thẻ market.
 *
 * Tách ra khỏi SpeciesCard vì logic favorite có ba tầng cache và add-to-aquarium có khoá
 * chống double-submit; copy sang thẻ thứ hai là nhân đôi thứ rất dễ lệch.
 *
 * Hai component KHÔNG tự quyết vị trí — nhận `className` để nơi dùng đặt đâu tuỳ ý.
 */

interface FavoriteButtonProps {
  specCode: number;
  className?: string;
}

/**
 * Ba tầng đọc trạng thái yêu thích, cố ý theo thứ tự này để tránh N cuộc gọi API khi render lưới:
 *   1. Cache riêng của loài — ưu tiên cao nhất, phản ánh thao tác vừa bấm trong phiên
 *   2. Cache danh sách yêu thích đã prefetch — giải quyết cục bộ, không gọi mạng
 *   3. Gọi API cho từng loài — chỉ khi hai tầng trên đều rỗng
 */
export function FavoriteButton({ specCode, className }: FavoriteButtonProps) {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);

  const [isFavorite, setIsFavorite] = useState(false);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (!isAuthenticated) return;

    const individual = getCached<boolean>(CacheKeys.favoriteCheck(specCode));
    if (individual !== null) { setIsFavorite(individual); return; }

    const allFavs = getCached<FavoriteDto[]>(CacheKeys.myFavorites());
    if (allFavs !== null) {
      const isFav = allFavs.some((f) => f.specCode === specCode);
      setCached(CacheKeys.favoriteCheck(specCode), isFav, FAVORITE_CHECK_TTL);
      setIsFavorite(isFav);
      return;
    }

    checkFavorite(specCode).then((v) => {
      setCached(CacheKeys.favoriteCheck(specCode), v, FAVORITE_CHECK_TTL);
      setIsFavorite(v);
    }).catch(() => {});
  }, [isAuthenticated, specCode]);

  const handleToggle = async (e: React.MouseEvent) => {
    e.stopPropagation();
    if (!isAuthenticated) { navigate('/login'); return; }

    const next = !isFavorite;
    setIsFavorite(next); // optimistic — rollback nếu API lỗi
    setLoading(true);
    try {
      if (next) await addFavorite(specCode);
      else await removeFavorite(specCode);
      setCached(CacheKeys.favoriteCheck(specCode), next, FAVORITE_CHECK_TTL);
    } catch {
      setIsFavorite(!next);
    } finally {
      setLoading(false);
    }
  };

  return (
    <button
      onClick={handleToggle}
      disabled={loading}
      className={cn('p-2 rounded-full hover:bg-black/20 transition-colors group/heart', className)}
      title={t('fish.addToFavorites')}
    >
      {loading
        ? <Loader2 className="w-5 h-5 text-white animate-spin drop-shadow-md" />
        : <Heart className={cn('w-6 h-6 transition-colors drop-shadow-md', isFavorite ? 'fill-rose-400 text-rose-400' : 'text-white/80 group-hover/heart:text-white')} />
      }
    </button>
  );
}

interface AddToAquariumButtonProps {
  specCode: number;
  aquariums: AquariumDto[];
  className?: string;
}

/** Chỉ render khi đã đăng nhập — nơi dùng không phải tự kiểm tra. */
export function AddToAquariumButton({ specCode, aquariums, className }: AddToAquariumButtonProps) {
  const { t } = useTranslation();
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);

  const [showPicker, setShowPicker] = useState(false);
  const [qty, setQty] = useState(1);
  const [addingId, setAddingId] = useState<string | null>(null);
  const pickerRef = useRef<HTMLDivElement>(null);
  // Khoá đồng bộ — setState là bất đồng bộ nên không chặn được cú bấm thứ hai, ref thì chặn được.
  const addingRef = useRef(false);

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

  if (!isAuthenticated) return null;

  const handleAdd = async (e: React.MouseEvent, aquariumId: string) => {
    e.stopPropagation();
    if (addingRef.current) return;
    addingRef.current = true;
    setAddingId(aquariumId);
    setShowPicker(false); // đóng ngay để không chọn trúng hai lần
    try {
      await addFishToAquarium(aquariumId, specCode, qty);
    } catch {
      // im lặng — thêm cá vào hồ là thao tác phụ, không chặn luồng xem loài
    } finally {
      addingRef.current = false;
      setAddingId(null);
    }
  };

  return (
    <div className={cn('relative', className)} ref={pickerRef}>
      <button
        onClick={(e) => { e.stopPropagation(); setShowPicker((v) => !v); }}
        className="flex items-center justify-center bg-[#2a2d32] hover:bg-primary/20 hover:border-primary/40 border border-transparent text-slate-300 hover:text-primary py-2 px-3 rounded-lg transition-colors"
        title={t('fish.addToAquarium')}
      >
        <Plus className="w-4 h-4" />
      </button>

      {showPicker && (
        <div className="absolute bottom-full right-0 mb-2 w-56 bg-[#1E293B] border border-slate-700 rounded-xl shadow-2xl z-50 overflow-hidden animate-in fade-in slide-in-from-bottom-2">
          <p className="text-[11px] font-bold text-slate-400 uppercase tracking-wider px-3 py-2 border-b border-slate-800">
            {t('fish.addToAquarium')}
          </p>

          <div className="flex items-center justify-between px-3 py-2 border-b border-slate-800/60">
            <span className="text-xs text-slate-400">{t('fish.quantity')}</span>
            <div className="flex items-center gap-1">
              <button
                onClick={(e) => { e.stopPropagation(); setQty((q) => Math.max(1, q - 1)); }}
                className="w-6 h-6 flex items-center justify-center rounded bg-slate-700 hover:bg-slate-600 text-slate-300 transition-colors"
              >
                <Minus className="w-3 h-3" />
              </button>
              <span className="w-7 text-center text-sm font-bold text-white tabular-nums">{qty}</span>
              <button
                onClick={(e) => { e.stopPropagation(); setQty((q) => Math.min(99, q + 1)); }}
                className="w-6 h-6 flex items-center justify-center rounded bg-slate-700 hover:bg-slate-600 text-slate-300 transition-colors"
              >
                <Plus className="w-3 h-3" />
              </button>
            </div>
          </div>

          {aquariums.length === 0 ? (
            <p className="text-xs text-slate-500 px-3 py-3 text-center">{t('fish.noAquariums')}</p>
          ) : (
            <div className="max-h-44 overflow-y-auto">
              {aquariums.map((aq) => (
                <button
                  key={aq.id}
                  onClick={(e) => handleAdd(e, aq.id)}
                  disabled={!!addingId}
                  className="w-full flex items-center gap-2.5 px-3 py-2.5 text-left text-sm text-slate-300 hover:bg-white/5 hover:text-white transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  {addingId === aq.id
                    ? <Loader2 className="w-3.5 h-3.5 shrink-0 animate-spin" />
                    : <Droplets className="w-3.5 h-3.5 shrink-0 text-slate-500" />
                  }
                  <span className="truncate flex-1">{aq.name}</span>
                </button>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  );
}

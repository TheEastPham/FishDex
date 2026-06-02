import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { getMyFavorites, removeFavorite, getSpeciesDetail, cn } from '@fishlover/shared';
import type { SpeciesDetail } from '@fishlover/shared';
import { Heart, Fish, Loader, ArrowRight, Trash2 } from 'lucide-react';

interface FavCard {
  specCode: number;
  detail: SpeciesDetail | null;
  loading: boolean;
}

export default function FavoritesPage() {
  const navigate = useNavigate();
  const [cards, setCards] = useState<FavCard[]>([]);
  const [loading, setLoading] = useState(true);
  const [removingId, setRemovingId] = useState<number | null>(null);

  useEffect(() => {
    const fetchFavorites = async () => {
      setLoading(true);
      try {
        const favs = await getMyFavorites();
        // Initialize cards with loading state
        const initial: FavCard[] = favs.map(f => ({ specCode: f.specCode, detail: null, loading: true }));
        setCards(initial);
        setLoading(false);

        // Fetch details one by one (TODO: replace with batch API when BE is ready)
        for (const fav of favs) {
          try {
            const detail = await getSpeciesDetail(fav.specCode);
            setCards(prev =>
              prev.map(c => c.specCode === fav.specCode ? { ...c, detail, loading: false } : c)
            );
          } catch {
            setCards(prev =>
              prev.map(c => c.specCode === fav.specCode ? { ...c, loading: false } : c)
            );
          }
        }
      } catch (err) {
        console.error(err);
        setLoading(false);
      }
    };
    fetchFavorites();
  }, []);

  const handleRemove = async (specCode: number) => {
    setRemovingId(specCode);
    try {
      await removeFavorite(specCode);
      setCards(prev => prev.filter(c => c.specCode !== specCode));
    } catch (err) {
      console.error(err);
    } finally {
      setRemovingId(null);
    }
  };

  if (loading) {
    return (
      <div className="min-h-screen bg-[#141518] flex items-center justify-center">
        <div className="flex flex-col items-center gap-4">
          <Heart className="w-14 h-14 text-rose-500/50 animate-pulse" />
          <p className="text-slate-400 font-medium">Đang tải danh sách yêu thích...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-[#141518] p-6 pb-20 font-sans">
      <div className="max-w-6xl mx-auto">

        {/* Header */}
        <div className="mb-8">
          <h1 className="text-3xl font-black text-white flex items-center gap-3">
            <Heart className="w-8 h-8 text-rose-400 fill-rose-400" />
            Cá yêu thích
          </h1>
          <p className="text-slate-400 mt-1">{cards.length} loài trong danh sách</p>
        </div>

        {/* Empty state */}
        {cards.length === 0 && (
          <div className="flex flex-col items-center justify-center py-24 text-center">
            <div className="w-20 h-20 rounded-2xl bg-rose-500/10 border border-rose-500/20 flex items-center justify-center mb-4">
              <Heart className="w-10 h-10 text-rose-400" />
            </div>
            <h3 className="text-xl font-bold text-white mb-2">Chưa có cá yêu thích</h3>
            <p className="text-slate-500 mb-6">Hãy thêm cá yêu thích từ trang chi tiết loài cá!</p>
            <button
              onClick={() => navigate('/fish')}
              className="flex items-center gap-2 bg-rose-500 hover:bg-rose-400 text-white font-bold px-5 py-2.5 rounded-xl transition-colors"
            >
              <Fish className="w-4 h-4" /> Khám phá cá
            </button>
          </div>
        )}

        {/* Grid */}
        {cards.length > 0 && (
          <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 gap-4">
            {cards.map(card => (
              <div
                key={card.specCode}
                className="bg-[#1e2024] border border-slate-800/60 rounded-2xl overflow-hidden hover:border-slate-700/60 hover:-translate-y-1 hover:shadow-xl transition-all duration-300 group"
              >
                {/* Image */}
                <div
                  className="aspect-square cursor-pointer relative overflow-hidden bg-[#141518]"
                  onClick={() => navigate(`/fish/${card.specCode}`)}
                >
                  {card.loading ? (
                    <div className="w-full h-full flex items-center justify-center">
                      <Loader className="w-6 h-6 text-slate-600 animate-spin" />
                    </div>
                  ) : card.detail?.preferredImageUrl ? (
                    <img
                      src={card.detail.preferredImageUrl}
                      alt={card.detail.preferredCommonName || card.detail.speciesName}
                      className="w-full h-full object-cover group-hover:scale-110 transition-transform duration-500"
                    />
                  ) : (
                    <div className="w-full h-full flex items-center justify-center">
                      <Fish className="w-10 h-10 text-slate-700" />
                    </div>
                  )}
                  {/* Arrow overlay */}
                  <div className="absolute top-2 right-2 opacity-0 group-hover:opacity-100 transition-opacity">
                    <div className="bg-black/50 rounded-lg p-1">
                      <ArrowRight className="w-3.5 h-3.5 text-white" />
                    </div>
                  </div>
                </div>

                {/* Info */}
                <div className="p-3">
                  {card.loading ? (
                    <div className="space-y-1.5">
                      <div className="h-3 bg-slate-800 rounded animate-pulse w-3/4" />
                      <div className="h-2.5 bg-slate-800 rounded animate-pulse w-1/2" />
                    </div>
                  ) : (
                    <>
                      <p className="font-bold text-white text-xs leading-snug line-clamp-2 mb-0.5">
                        {card.detail?.preferredCommonName || card.detail?.speciesName || `Species #${card.specCode}`}
                      </p>
                      {card.detail?.preferredCommonName && (
                        <p className="text-slate-500 text-[11px] italic truncate">{card.detail.speciesName}</p>
                      )}
                    </>
                  )}

                  {/* Remove button */}
                  <button
                    onClick={() => handleRemove(card.specCode)}
                    disabled={removingId === card.specCode}
                    className={cn(
                      "mt-2 w-full flex items-center justify-center gap-1.5 py-1.5 rounded-lg text-xs font-semibold transition-colors",
                      removingId === card.specCode
                        ? "bg-rose-500/10 text-rose-400 opacity-50 cursor-not-allowed"
                        : "bg-rose-500/10 hover:bg-rose-500/20 text-rose-400"
                    )}
                  >
                    {removingId === card.specCode
                      ? <Loader className="w-3 h-3 animate-spin" />
                      : <Trash2 className="w-3 h-3" />
                    }
                    Bỏ yêu thích
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

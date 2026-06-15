import { useNavigate } from 'react-router-dom';
import { useMyAquariums, useMyFavorites, useSpeciesSummaries, cn, useTranslation } from '@fishlover/shared';
import type { AquariumDto, SpeciesSummary } from '@fishlover/shared';
import {
  Droplets, Plus, Heart, Fish, ArrowRight, Layers,
  FlaskConical, Ruler, Sparkles
} from 'lucide-react';


const TANK_TYPE_STYLES: Record<string, { bg: string; text: string; border: string; label: string }> = {
  freshwater: { bg: 'bg-emerald-500/10', text: 'text-emerald-400', border: 'border-emerald-500/20', label: 'Nước ngọt' },
  saltwater:  { bg: 'bg-sky-500/10',     text: 'text-sky-400',     border: 'border-sky-500/20',     label: 'Nước mặn' },
  brackish:   { bg: 'bg-teal-500/10',    text: 'text-teal-400',    border: 'border-teal-500/20',    label: 'Lợ' },
  planted:    { bg: 'bg-lime-500/10',    text: 'text-lime-400',    border: 'border-lime-500/20',    label: 'Thủy sinh' },
};

function getTankStyle(type: string | null) {
  if (!type) return { bg: 'bg-slate-700/20', text: 'text-slate-400', border: 'border-slate-700/30', label: 'Khác' };
  const key = Object.keys(TANK_TYPE_STYLES).find(k => type.toLowerCase().includes(k));
  return key ? TANK_TYPE_STYLES[key] : { bg: 'bg-slate-700/20', text: 'text-slate-400', border: 'border-slate-700/30', label: type };
}

function StatCard({ icon, label, value, sub, color, loading }: {
  icon: React.ReactNode; label: string; value: string | number; sub?: string; color: string; loading?: boolean;
}) {
  return (
    <div className="bg-[#1E293B] rounded-2xl p-5 border border-slate-800/60 flex items-center gap-4 hover:bg-[#263348] transition-colors group">
      <div className={cn('p-3 rounded-xl shrink-0 transition-transform group-hover:scale-110', color)}>
        {icon}
      </div>
      <div>
        <p className="text-xs text-slate-500 font-semibold uppercase tracking-widest mb-0.5">{label}</p>
        {loading
          ? <div className="h-7 w-16 bg-slate-800 rounded-lg animate-pulse mt-1" />
          : <p className="text-2xl font-black text-white">{value}</p>
        }
        {sub && <p className="text-xs text-slate-500 mt-0.5">{sub}</p>}
      </div>
    </div>
  );
}

function AquariumPreviewCard({ tank, onClick }: { tank: AquariumDto; onClick: () => void }) {
  const style = getTankStyle(tank.type);
  return (
    <div
      onClick={onClick}
      className="bg-[#1E293B] border border-slate-800/60 rounded-2xl p-5 cursor-pointer hover:bg-[#263348] hover:-translate-y-1 hover:shadow-xl hover:border-slate-700/60 transition-all duration-300 group"
    >
      <div className="flex items-start justify-between mb-4">
        <div className={cn('p-2 rounded-xl border', style.bg, style.border)}>
          <Droplets className={cn('w-5 h-5', style.text)} />
        </div>
        {tank.type && (
          <span className={cn('text-[11px] font-bold uppercase tracking-wider px-2.5 py-1 rounded-full border', style.bg, style.text, style.border)}>
            {style.label}
          </span>
        )}
      </div>
      <h3 className="font-bold text-white text-base mb-1 truncate">{tank.name}</h3>
      <div className="flex items-center gap-3 mt-3 text-sm text-slate-400">
        {tank.volumeLiters != null && (
          <span className="flex items-center gap-1">
            <FlaskConical className="w-3.5 h-3.5" />
            {tank.volumeLiters.toFixed(0)}L
          </span>
        )}
        {tank.lengthCm != null && (
          <span className="flex items-center gap-1">
            <Ruler className="w-3.5 h-3.5" />
            {tank.lengthCm}×{tank.widthCm}×{tank.heightCm}
          </span>
        )}
      </div>
      <div className="mt-4 pt-3 border-t border-slate-800/50 flex items-center justify-between">
        <span className="text-xs text-slate-500 flex items-center gap-1.5">
          <Fish className="w-3.5 h-3.5" /> {tank.fishCount} cư dân
        </span>
        <ArrowRight className="w-4 h-4 text-slate-600 group-hover:text-slate-400 group-hover:translate-x-1 transition-all" />
      </div>
    </div>
  );
}

function FavoritePreview({ summary, onClick }: { summary: SpeciesSummary; onClick: () => void }) {
  return (
    <div
      onClick={onClick}
      className="relative rounded-xl overflow-hidden aspect-square cursor-pointer group border border-slate-800/50 bg-[#172033]"
    >
      {summary.imageUrl ? (
        <img src={summary.imageUrl} alt={summary.commonName || summary.speciesName} className="w-full h-full object-cover group-hover:scale-110 transition-transform duration-500" />
      ) : (
        <div className="w-full h-full flex items-center justify-center">
          <Fish className="w-8 h-8 text-slate-700" />
        </div>
      )}
      <div className="absolute inset-0 bg-gradient-to-t from-black/80 via-transparent to-transparent opacity-0 group-hover:opacity-100 transition-opacity flex items-end p-2">
        <p className="text-white text-xs font-bold truncate">{summary.commonName || summary.speciesName}</p>
      </div>
    </div>
  );
}

export default function DashboardPage() {
  const navigate = useNavigate();
  const { i18n } = useTranslation();

  const { aquariums, loading: tanksLoading } = useMyAquariums();
  const { favorites, loading: favsLoading } = useMyFavorites();

  const previewCodes = favorites.slice(0, 6).map((f) => f.specCode);
  const { summaries, loading: summariesLoading } = useSpeciesSummaries(previewCodes, i18n.language);

  const totalVolume = aquariums.reduce((sum, t) => sum + (t.volumeLiters ?? 0), 0);

  return (
    <div className="min-h-screen bg-[#0F172A] p-6 pb-20 font-sans">
      <div className="max-w-6xl mx-auto space-y-8">

        {/* ── Header ── */}
        <div className="flex items-start justify-between">
          <div>
            <div className="flex items-center gap-2 mb-1">
              <Sparkles className="w-5 h-5 text-amber-400" />
              <span className="text-amber-400 text-sm font-bold uppercase tracking-widest">AquaHome</span>
            </div>
            <h1 className="text-3xl font-black text-white">Bộ sưu tập của bạn</h1>
            <p className="text-slate-400 mt-1">Quản lý hồ cá và danh sách yêu thích</p>
          </div>
          <button
            onClick={() => navigate('/tanks')}
            className="flex items-center gap-2 bg-sky-500 hover:bg-sky-400 text-white font-bold px-4 py-2.5 rounded-xl transition-colors shadow-lg shadow-sky-500/20 text-sm"
          >
            <Plus className="w-4 h-4" />
            Thêm hồ mới
          </button>
        </div>

        {/* ── Stats ── */}
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
          <StatCard icon={<Layers className="w-5 h-5 text-sky-400" />}     label="Tổng số hồ"    value={aquariums.length}            sub="hồ cá"        color="bg-sky-500/10"     loading={tanksLoading} />
          <StatCard icon={<FlaskConical className="w-5 h-5 text-emerald-400" />} label="Tổng thể tích" value={`${totalVolume.toFixed(0)}L`} sub="đang quản lý" color="bg-emerald-500/10" loading={tanksLoading} />
          <StatCard icon={<Heart className="w-5 h-5 text-rose-400" />}     label="Cá yêu thích"  value={favorites.length}            sub="loài"         color="bg-rose-500/10"    loading={favsLoading} />
        </div>

        {/* ── My Aquariums preview ── */}
        <div>
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-lg font-bold text-white flex items-center gap-2">
              <Droplets className="w-5 h-5 text-sky-400" /> Hồ cá của tôi
            </h2>
            <button onClick={() => navigate('/tanks')} className="text-sm text-sky-400 hover:text-sky-300 font-semibold flex items-center gap-1 transition-colors">
              Xem tất cả <ArrowRight className="w-4 h-4" />
            </button>
          </div>

          {tanksLoading ? (
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
              {[1, 2, 3].map(i => <div key={i} className="bg-[#1E293B] border border-slate-800/60 rounded-2xl h-44 animate-pulse" />)}
            </div>
          ) : aquariums.length === 0 ? (
            <div className="bg-[#1E293B] border border-dashed border-slate-700/60 rounded-2xl p-10 text-center">
              <Droplets className="w-12 h-12 text-slate-700 mx-auto mb-3" />
              <p className="text-slate-400 font-medium mb-4">Bạn chưa có hồ cá nào</p>
              <button onClick={() => navigate('/tanks')} className="inline-flex items-center gap-2 bg-sky-500 hover:bg-sky-400 text-white font-bold px-4 py-2 rounded-xl transition-colors text-sm">
                <Plus className="w-4 h-4" /> Tạo hồ đầu tiên
              </button>
            </div>
          ) : (
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
              {aquariums.slice(0, 3).map(tank => (
                <AquariumPreviewCard key={tank.id} tank={tank} onClick={() => navigate('/tanks')} />
              ))}
            </div>
          )}
        </div>

        {/* ── Favorites preview ── */}
        <div>
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-lg font-bold text-white flex items-center gap-2">
              <Heart className="w-5 h-5 text-rose-400" /> Cá yêu thích
            </h2>
            <button onClick={() => navigate('/favorites')} className="text-sm text-rose-400 hover:text-rose-300 font-semibold flex items-center gap-1 transition-colors">
              Xem tất cả <ArrowRight className="w-4 h-4" />
            </button>
          </div>

          {favsLoading || summariesLoading ? (
            <div className="grid grid-cols-3 sm:grid-cols-6 gap-3">
              {[1, 2, 3, 4, 5, 6].map(i => <div key={i} className="aspect-square rounded-xl bg-[#1E293B] animate-pulse" />)}
            </div>
          ) : Object.keys(summaries).length > 0 ? (
            <div className="grid grid-cols-3 sm:grid-cols-6 gap-3">
              {Object.values(summaries).map(s => (
                <FavoritePreview key={s.specCode} summary={s} onClick={() => navigate(`/fish/${s.specCode}`)} />
              ))}
            </div>
          ) : (
            <div className="bg-[#1E293B] border border-dashed border-slate-700/60 rounded-2xl p-8 text-center">
              <Heart className="w-10 h-10 text-slate-700 mx-auto mb-3" />
              <p className="text-slate-500 text-sm">Chưa có cá yêu thích — thêm từ trang chi tiết loài cá!</p>
            </div>
          )}
        </div>

      </div>
    </div>
  );
}

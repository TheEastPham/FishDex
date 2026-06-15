import { useEffect, useState } from 'react';
import { createAquarium, updateAquarium, deleteAquarium, getMyAquariums } from '@fishlover/shared';
import type { AquariumDto, CreateAquariumRequest } from '@fishlover/shared';
import { Plus, Droplets, Fish } from 'lucide-react';
import AquariumForm from './components/AquariumForm';
import AquariumDetail from './components/AquariumDetail';
import { getTankStyle } from './components/AquariumCard';

export default function TanksPage() {
  const [tanks, setTanks]       = useState<AquariumDto[]>([]);
  const [loading, setLoading]   = useState(true);
  const [activeId, setActiveId] = useState<string | null>(null);
  const [formOpen, setFormOpen] = useState(false);
  const [editing, setEditing]   = useState<AquariumDto | null>(null);
  const [deleteId, setDeleteId] = useState<string | null>(null);

  const fetchTanks = async () => {
    setLoading(true);
    try {
      const data = await getMyAquariums();
      setTanks(data);
      setActiveId(prev => {
        if (prev && data.some(t => t.id === prev)) return prev;
        return data[0]?.id ?? null;
      });
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { fetchTanks(); }, []);

  const handleSave = async (req: CreateAquariumRequest) => {
    if (editing) {
      await updateAquarium(editing.id, req);
    } else {
      const created = await createAquarium(req);
      setActiveId(created.id);
    }
    setEditing(null);
    await fetchTanks();
  };

  const handleDelete = (id: string) => setDeleteId(id);

  const confirmDelete = async () => {
    if (!deleteId) return;
    try {
      await deleteAquarium(deleteId);
      await fetchTanks();
    } catch (err) {
      console.error(err);
    } finally {
      setDeleteId(null);
    }
  };

  const activeTank = tanks.find(t => t.id === activeId) ?? null;

  return (
    <div className="min-h-screen bg-[#0F172A] pb-20 font-sans">

      {/* Header */}
      <div className="flex items-center justify-between px-6 pt-6 pb-4">
        <div>
          <h1 className="text-2xl font-black text-white flex items-center gap-2.5">
            <Droplets className="w-6 h-6 text-sky-400" />
            Hồ cá của tôi
          </h1>
          <p className="text-slate-500 text-sm mt-0.5">{tanks.length} hồ đang quản lý</p>
        </div>
        <button
          onClick={() => { setEditing(null); setFormOpen(true); }}
          className="flex items-center gap-2 bg-sky-500 hover:bg-sky-400 text-white font-bold px-4 py-2 rounded-xl transition-colors shadow-lg shadow-sky-500/20 text-sm"
        >
          <Plus className="w-4 h-4" />
          Thêm hồ
        </button>
      </div>

      {/* Loading */}
      {loading && (
        <div className="flex items-center justify-center py-32">
          <Fish className="w-10 h-10 text-slate-700 animate-bounce" />
        </div>
      )}

      {/* Empty */}
      {!loading && tanks.length === 0 && (
        <div className="flex flex-col items-center justify-center py-28 text-center px-6">
          <div className="w-20 h-20 rounded-2xl bg-sky-500/10 border border-sky-500/20 flex items-center justify-center mb-4">
            <Droplets className="w-10 h-10 text-sky-400" />
          </div>
          <h3 className="text-xl font-bold text-white mb-2">Chưa có hồ cá nào</h3>
          <p className="text-slate-500 mb-6">Hãy tạo hồ cá đầu tiên của bạn!</p>
          <button
            onClick={() => { setEditing(null); setFormOpen(true); }}
            className="flex items-center gap-2 bg-sky-500 hover:bg-sky-400 text-white font-bold px-5 py-2.5 rounded-xl transition-colors"
          >
            <Plus className="w-4 h-4" /> Tạo hồ đầu tiên
          </button>
        </div>
      )}

      {/* Tab view */}
      {!loading && tanks.length > 0 && (
        <>
          {/* Tab bar */}
          <div className="flex overflow-x-auto border-b border-slate-800/60 px-6 gap-1 scrollbar-none">
            {tanks.map(tank => {
              const style = getTankStyle(tank.type);
              const isActive = tank.id === activeId;
              return (
                <button
                  key={tank.id}
                  onClick={() => setActiveId(tank.id)}
                  className={`flex items-center gap-2 px-4 py-3 text-sm font-semibold whitespace-nowrap border-b-2 transition-all ${
                    isActive
                      ? 'border-sky-500 text-white'
                      : 'border-transparent text-slate-400 hover:text-slate-200 hover:border-slate-600'
                  }`}
                >
                  <Droplets className={`w-3.5 h-3.5 shrink-0 ${isActive ? 'text-sky-400' : style.text}`} />
                  <span className="max-w-[140px] truncate">{tank.name}</span>
                  {tank.fishCount > 0 && (
                    <span className={`text-[10px] font-bold px-1.5 py-0.5 rounded-full ${isActive ? 'bg-sky-500/20 text-sky-300' : 'bg-slate-800 text-slate-500'}`}>
                      {tank.fishCount}
                    </span>
                  )}
                </button>
              );
            })}
          </div>

          {/* Active tab content */}
          {activeTank && (
            <AquariumDetail
              key={activeTank.id}
              tank={activeTank}
              onEdit={t => { setEditing(t); setFormOpen(true); }}
              onDelete={handleDelete}
            />
          )}
        </>
      )}

      {/* Form drawer */}
      <AquariumForm
        isOpen={formOpen}
        onClose={() => { setFormOpen(false); setEditing(null); }}
        onSave={handleSave}
        editing={editing}
      />

      {/* Delete confirm */}
      {deleteId && (
        <>
          <div className="fixed inset-0 bg-black/60 z-50" onClick={() => setDeleteId(null)} />
          <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
            <div className="bg-[#1E293B] border border-slate-700 rounded-2xl p-6 max-w-sm w-full shadow-2xl">
              <h3 className="text-white font-bold text-lg mb-2">Xoá hồ cá?</h3>
              <p className="text-slate-400 text-sm mb-6">Hành động này không thể hoàn tác.</p>
              <div className="flex gap-3">
                <button onClick={() => setDeleteId(null)} className="flex-1 py-2.5 rounded-xl bg-white/5 hover:bg-white/10 text-slate-300 font-bold transition-colors">Huỷ</button>
                <button onClick={confirmDelete} className="flex-1 py-2.5 rounded-xl bg-red-500 hover:bg-red-400 text-white font-bold transition-colors">Xoá</button>
              </div>
            </div>
          </div>
        </>
      )}
    </div>
  );
}

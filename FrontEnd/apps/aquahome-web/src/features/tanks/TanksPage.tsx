import { useEffect, useState } from 'react';
import { getMyAquariums, createAquarium, updateAquarium, deleteAquarium } from '@fishlover/shared';
import type { AquariumDto, CreateAquariumRequest } from '@fishlover/shared';
import { Plus, Droplets, Fish, Search } from 'lucide-react';
import AquariumCard from './components/AquariumCard';
import AquariumForm from './components/AquariumForm';

export default function TanksPage() {
  const [tanks, setTanks] = useState<AquariumDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [formOpen, setFormOpen] = useState(false);
  const [editing, setEditing] = useState<AquariumDto | null>(null);
  const [search, setSearch] = useState('');
  const [deleteId, setDeleteId] = useState<string | null>(null);

  const fetchTanks = async () => {
    setLoading(true);
    try {
      const data = await getMyAquariums();
      setTanks(data);
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
      await createAquarium(req);
    }
    setEditing(null);
    await fetchTanks();
  };

  const handleDelete = async (id: string) => {
    setDeleteId(id);
  };

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

  const filtered = tanks.filter(t => t.name.toLowerCase().includes(search.toLowerCase()));

  return (
    <div className="min-h-screen bg-[#141518] p-6 pb-20 font-sans">
      <div className="max-w-6xl mx-auto">

        {/* Header */}
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-8">
          <div>
            <h1 className="text-3xl font-black text-white flex items-center gap-3">
              <Droplets className="w-8 h-8 text-sky-400" />
              Hồ cá của tôi
            </h1>
            <p className="text-slate-400 mt-1">{tanks.length} hồ cá đang quản lý</p>
          </div>
          <button
            onClick={() => { setEditing(null); setFormOpen(true); }}
            className="flex items-center gap-2 bg-sky-500 hover:bg-sky-400 text-white font-bold px-5 py-2.5 rounded-xl transition-colors shadow-lg shadow-sky-500/20"
          >
            <Plus className="w-4 h-4" />
            Thêm hồ cá
          </button>
        </div>

        {/* Search */}
        {tanks.length > 0 && (
          <div className="relative mb-6">
            <Search className="absolute left-4 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-500" />
            <input
              value={search}
              onChange={e => setSearch(e.target.value)}
              placeholder="Tìm kiếm hồ cá..."
              className="w-full bg-[#1e2024] border border-slate-800/60 rounded-xl pl-10 pr-4 py-3 text-white placeholder-slate-600 focus:outline-none focus:border-sky-500/40 transition-all"
            />
          </div>
        )}

        {/* Content */}
        {loading ? (
          <div className="flex items-center justify-center py-24">
            <Fish className="w-12 h-12 text-slate-700 animate-bounce" />
          </div>
        ) : tanks.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-24 text-center">
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
        ) : filtered.length === 0 ? (
          <div className="text-center py-16 text-slate-500">
            Không tìm thấy hồ cá nào với từ khoá "<span className="text-slate-300">{search}</span>"
          </div>
        ) : (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-5">
            {filtered.map(tank => (
              <AquariumCard
                key={tank.id}
                tank={tank}
                onEdit={t => { setEditing(t); setFormOpen(true); }}
                onDelete={handleDelete}
              />
            ))}
          </div>
        )}
      </div>

      {/* Form Drawer */}
      <AquariumForm
        isOpen={formOpen}
        onClose={() => { setFormOpen(false); setEditing(null); }}
        onSave={handleSave}
        editing={editing}
      />

      {/* Delete Confirm Modal */}
      {deleteId && (
        <>
          <div className="fixed inset-0 bg-black/60 z-50" onClick={() => setDeleteId(null)} />
          <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
            <div className="bg-[#1e2024] border border-slate-700 rounded-2xl p-6 max-w-sm w-full shadow-2xl">
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

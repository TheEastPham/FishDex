import { useEffect, useState } from 'react';
import { createAquarium, updateAquarium, deleteAquarium, getMyAquariums, useTranslation } from '@fishlover/shared';
import type { AquariumDto, CreateAquariumRequest } from '@fishlover/shared';
import { PlusCircle, Fish } from 'lucide-react';
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

  const { t } = useTranslation();
  const activeTank = tanks.find(t => t.id === activeId) ?? null;

  return (
    <div className="min-h-screen bg-[#0F172A] pb-20 font-sans">

      {/* ── Header ── */}
      <div className="px-4 sm:px-6 pt-5 pb-3 flex items-center justify-between">
        <h1 className="text-xl sm:text-2xl font-black text-white">
          {t('tanks.pageTitle')}
          {!loading && tanks.length > 0 && (
            <span className="ml-2 text-sm font-semibold text-slate-500">
              · {t('tanks.tankCount', { count: tanks.length })}
            </span>
          )}
        </h1>
        <button
          onClick={() => { setEditing(null); setFormOpen(true); }}
          className="flex items-center gap-1.5 px-3 py-1.5 rounded-xl bg-sky-500/15 hover:bg-sky-500/30 border border-sky-500/30 hover:border-sky-400/60 text-sky-400 text-sm font-semibold transition-colors"
          title={t('tanks.addBtn')}
        >
          <PlusCircle className="w-4 h-4" />
          <span className="hidden sm:inline">{t('tanks.addBtn')}</span>
        </button>
      </div>

      {/* ── Loading ── */}
      {loading && (
        <div className="flex items-center justify-center py-32">
          <Fish className="w-10 h-10 text-slate-700 animate-bounce" />
        </div>
      )}

      {/* ── Empty ── */}
      {!loading && tanks.length === 0 && (
        <div className="flex flex-col items-center justify-center py-28 text-center px-6">
          <div className="w-20 h-20 rounded-2xl bg-sky-500/10 border border-sky-500/20 flex items-center justify-center mb-4">
            <Fish className="w-10 h-10 text-sky-400" />
          </div>
          <h3 className="text-xl font-bold text-white mb-2">{t('tanks.emptyTitle')}</h3>
          <p className="text-slate-500 mb-6">{t('tanks.emptyHint')}</p>
          <button
            onClick={() => { setEditing(null); setFormOpen(true); }}
            className="flex items-center gap-2 bg-sky-500 hover:bg-sky-400 text-white font-bold px-5 py-2.5 rounded-xl transition-colors"
          >
            <PlusCircle className="w-4 h-4" /> {t('tanks.createFirstBtn')}
          </button>
        </div>
      )}

      {/* ── Tab bar + content ── */}
      {!loading && tanks.length > 0 && (
        <>
          {/* Tab bar: tank tabs + "+" at end */}
          <div className="flex items-end overflow-x-auto border-b border-slate-800/60 px-4 sm:px-6 gap-0.5 scrollbar-none">
            {tanks.map(tank => {
              const style = getTankStyle(tank.waterType);
              const isActive = tank.id === activeId;
              return (
                <button
                  key={tank.id}
                  onClick={() => setActiveId(tank.id)}
                  className={`flex items-center gap-1.5 px-3 py-2.5 text-sm font-semibold whitespace-nowrap border-b-2 transition-all ${
                    isActive
                      ? 'border-sky-500 text-white'
                      : 'border-transparent text-slate-400 hover:text-slate-200 hover:border-slate-600'
                  }`}
                >
                  <span
                    className={`w-2 h-2 rounded-full shrink-0 ${isActive ? 'bg-sky-400' : style.text.replace('text-', 'bg-') + ' opacity-60'}`}
                  />
                  <span className="max-w-[120px] truncate">{tank.name}</span>
                  {tank.fishCount > 0 && (
                    <span className={`text-[10px] font-bold px-1.5 py-0.5 rounded-full ${isActive ? 'bg-sky-500/20 text-sky-300' : 'bg-slate-800 text-slate-500'}`}>
                      {tank.fishCount}
                    </span>
                  )}
                </button>
              );
            })}

          </div>

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

      {/* ── Form drawer ── */}
      <AquariumForm
        isOpen={formOpen}
        onClose={() => { setFormOpen(false); setEditing(null); }}
        onSave={handleSave}
        editing={editing}
      />

      {/* ── Delete confirm ── */}
      {deleteId && (
        <>
          <div className="fixed inset-0 bg-black/60 z-50" onClick={() => setDeleteId(null)} />
          <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
            <div className="bg-[#1E293B] border border-slate-700 rounded-2xl p-6 max-w-sm w-full shadow-2xl">
              <h3 className="text-white font-bold text-lg mb-2">{t('tanks.deleteConfirmTitle')}</h3>
              <p className="text-slate-400 text-sm mb-6">{t('tanks.deleteConfirmBody')}</p>
              <div className="flex gap-3">
                <button onClick={() => setDeleteId(null)} className="flex-1 py-2.5 rounded-xl bg-white/5 hover:bg-white/10 text-slate-300 font-bold transition-colors">
                  {t('tanks.cancel')}
                </button>
                <button onClick={confirmDelete} className="flex-1 py-2.5 rounded-xl bg-red-500 hover:bg-red-400 text-white font-bold transition-colors">
                  {t('tanks.confirmDelete')}
                </button>
              </div>
            </div>
          </div>
        </>
      )}
    </div>
  );
}

import { useEffect, useState } from 'react';
import { Bell, Droplets, Filter, CheckCircle, Trash2, Plus, Clock, AlertCircle } from 'lucide-react';
import {
  AquaTaskType,
  getReminders,
  createReminder,
  completeReminder,
  deleteReminder,
  useTranslation,
  cn,
} from '@fishlover/shared';
import type { ReminderDto, CreateReminderRequest } from '@fishlover/shared';
import ReminderFormModal from './ReminderFormModal';
import ScheduleNextModal from './ScheduleNextModal';

function getReminderStatus(r: ReminderDto): 'done' | 'overdue' | 'upcoming' {
  if (r.isCompleted) return 'done';
  if (new Date(r.dueAt) < new Date()) return 'overdue';
  return 'upcoming';
}

function formatDueAt(iso: string, locale: string): string {
  return new Date(iso).toLocaleString(locale, {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

interface Props {
  aquariumId: string;
}

export default function RemindersSection({ aquariumId }: Props) {
  const { t } = useTranslation();
  const [reminders, setReminders] = useState<ReminderDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [deleteId, setDeleteId] = useState<string | null>(null);
  const [scheduleNext, setScheduleNext] = useState<{ completedId: string; suggestedNextDueAt: string; intervalDays: number | null } | null>(null);

  useEffect(() => {
    setLoading(true);
    getReminders(aquariumId)
      .then(setReminders)
      .catch(console.error)
      .finally(() => setLoading(false));
  }, [aquariumId]);

  async function handleCreate(req: CreateReminderRequest) {
    const created = await createReminder(aquariumId, req);
    setReminders(prev => [created, ...prev]);
    setShowForm(false);
  }

  async function handleComplete(reminder: ReminderDto) {
    const res = await completeReminder(aquariumId, reminder.id);
    setReminders(prev => prev.map(r => r.id === res.completedId ? { ...r, isCompleted: true, completedAt: new Date().toISOString() } : r));
    if (res.suggestedNextDueAt) {
      setScheduleNext({ completedId: res.completedId, suggestedNextDueAt: res.suggestedNextDueAt, intervalDays: reminder.intervalDays });
    }
  }

  async function handleDelete(id: string) {
    await deleteReminder(aquariumId, id);
    setReminders(prev => prev.filter(r => r.id !== id));
    setDeleteId(null);
  }

  async function handleScheduleNext(dueAt: string) {
    if (!scheduleNext) return;
    const existing = reminders.find(r => r.id === scheduleNext.completedId);
    if (existing) {
      const created = await createReminder(aquariumId, {
        aquaTaskType: existing.aquaTaskType,
        dueAt,
        intervalDays: scheduleNext.intervalDays,
      });
      setReminders(prev => [created, ...prev]);
    }
    setScheduleNext(null);
  }

  const locale = t('aquarium.dateLocale');

  return (
    <div className="mt-8">
      {/* Section header */}
      <div className="flex items-center justify-between mb-3">
        <div className="flex items-center gap-2">
          <Bell className="w-4 h-4 text-slate-400" />
          <h3 className="text-white font-bold text-sm">{t('reminders.title')}</h3>
          {reminders.length > 0 && (
            <span className="text-xs bg-slate-700 text-slate-300 rounded-full px-2 py-0.5">{reminders.length}</span>
          )}
        </div>
        <button
          onClick={() => setShowForm(true)}
          className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-sky-500/20 border border-sky-500/40 text-sky-300 hover:bg-sky-500/30 transition-colors text-xs font-semibold min-h-[36px]"
        >
          <Plus className="w-3.5 h-3.5" />
          {t('reminders.addBtn')}
        </button>
      </div>

      {/* List */}
      {loading ? (
        <div className="space-y-2">
          {[1, 2].map(i => (
            <div key={i} className="h-16 bg-slate-800/40 rounded-xl animate-pulse" />
          ))}
        </div>
      ) : reminders.length === 0 ? (
        <div className="rounded-xl border border-slate-800/60 bg-slate-800/20 px-4 py-6 text-center">
          <p className="text-slate-400 text-sm">{t('reminders.empty')}</p>
          <p className="text-slate-600 text-xs mt-1">{t('reminders.emptyHint')}</p>
        </div>
      ) : (
        <div className="space-y-2">
          {reminders.map(r => {
            const status = getReminderStatus(r);
            const isWater = r.aquaTaskType === AquaTaskType.WaterChange;
            return (
              <div
                key={r.id}
                className={cn(
                  'flex items-center gap-3 rounded-xl border px-3 py-2.5 transition-colors',
                  status === 'done'    && 'bg-slate-800/20 border-slate-800/40 opacity-60',
                  status === 'overdue' && 'bg-red-500/5 border-red-500/30',
                  status === 'upcoming'&& 'bg-slate-800/30 border-slate-800/60',
                )}
              >
                {/* Icon */}
                <div className={cn(
                  'w-8 h-8 rounded-lg flex items-center justify-center flex-shrink-0',
                  isWater ? 'bg-sky-500/20' : 'bg-amber-500/20',
                )}>
                  {isWater
                    ? <Droplets className="w-4 h-4 text-sky-400" />
                    : <Filter className="w-4 h-4 text-amber-400" />
                  }
                </div>

                {/* Info */}
                <div className="flex-1 min-w-0">
                  <p className="text-white text-xs font-semibold">
                    {isWater ? t('reminders.waterChange') : t('reminders.filterCleaning')}
                  </p>
                  <div className="flex items-center gap-1 mt-0.5">
                    {status === 'overdue'
                      ? <AlertCircle className="w-3 h-3 text-red-400 flex-shrink-0" />
                      : <Clock className="w-3 h-3 text-slate-500 flex-shrink-0" />
                    }
                    <p className={cn('text-xs truncate', status === 'overdue' ? 'text-red-400' : 'text-slate-500')}>
                      {formatDueAt(r.dueAt, locale)}
                    </p>
                  </div>
                  {r.intervalDays && (
                    <p className="text-slate-600 text-[10px] mt-0.5">
                      {t('reminders.intervalLabel')} {t('reminders.intervalDays', { count: r.intervalDays })}
                    </p>
                  )}
                </div>

                {/* Status badge */}
                {status === 'done' && (
                  <span className="text-[10px] text-emerald-400 font-medium flex-shrink-0">{t('reminders.done')}</span>
                )}
                {status === 'overdue' && (
                  <span className="text-[10px] text-red-400 font-medium flex-shrink-0">{t('reminders.overdue')}</span>
                )}

                {/* Actions */}
                <div className="flex items-center gap-1 flex-shrink-0">
                  {!r.isCompleted && (
                    <button
                      onClick={() => handleComplete(r)}
                      className="p-1.5 rounded-lg hover:bg-emerald-500/20 transition-colors min-h-[36px] min-w-[36px] flex items-center justify-center"
                      title={t('reminders.markDone')}
                    >
                      <CheckCircle className="w-4 h-4 text-emerald-400" />
                    </button>
                  )}
                  <button
                    onClick={() => setDeleteId(r.id)}
                    className="p-1.5 rounded-lg hover:bg-red-500/20 transition-colors min-h-[36px] min-w-[36px] flex items-center justify-center"
                    title={t('reminders.deleteBtn')}
                  >
                    <Trash2 className="w-4 h-4 text-slate-500 hover:text-red-400" />
                  </button>
                </div>
              </div>
            );
          })}
        </div>
      )}

      {/* Add form modal */}
      {showForm && (
        <ReminderFormModal onClose={() => setShowForm(false)} onSubmit={handleCreate} />
      )}

      {/* Schedule next modal */}
      {scheduleNext && (
        <ScheduleNextModal
          suggestedNextDueAt={scheduleNext.suggestedNextDueAt}
          onSchedule={handleScheduleNext}
          onSkip={() => setScheduleNext(null)}
        />
      )}

      {/* Delete confirmation */}
      {deleteId && (
        <>
          <div className="fixed inset-0 bg-black/60 z-50" onClick={() => setDeleteId(null)} />
          <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
            <div className="bg-[#1E293B] border border-slate-700 rounded-2xl p-6 w-full max-w-sm shadow-2xl">
              <h3 className="text-white font-bold text-base mb-2">{t('reminders.deleteConfirmTitle')}</h3>
              <p className="text-slate-400 text-sm mb-5">{t('reminders.deleteConfirmBody')}</p>
              <div className="flex gap-2">
                <button
                  onClick={() => setDeleteId(null)}
                  className="flex-1 py-2.5 rounded-xl border border-slate-700 text-slate-400 hover:bg-slate-700/50 font-semibold text-sm min-h-[44px]"
                >
                  {t('reminders.cancel')}
                </button>
                <button
                  onClick={() => handleDelete(deleteId)}
                  className="flex-1 py-2.5 rounded-xl bg-red-500 hover:bg-red-600 text-white font-bold text-sm min-h-[44px]"
                >
                  {t('reminders.confirm')}
                </button>
              </div>
            </div>
          </div>
        </>
      )}
    </div>
  );
}

import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  getAllReminders,
  completeReminder,
  createReminder,
  AquaTaskType,
  useTranslation,
  cn,
} from '@fishlover/shared';
import type { UserReminderDto, CreateReminderRequest } from '@fishlover/shared';
import { Bell, Droplets, Filter, CheckCircle, Clock, AlertCircle, Fish } from 'lucide-react';
import ScheduleNextModal from '@/features/tanks/components/ScheduleNextModal';

function getReminderStatus(r: UserReminderDto): 'done' | 'overdue' | 'upcoming' {
  if (r.isCompleted) return 'done';
  if (new Date(r.dueAt) < new Date()) return 'overdue';
  return 'upcoming';
}

function formatDueAt(iso: string, locale: string): string {
  return new Date(iso).toLocaleString(locale, {
    day: '2-digit', month: '2-digit', year: 'numeric',
    hour: '2-digit', minute: '2-digit',
  });
}

interface ScheduleNextState {
  completedId: string;
  aquariumId: string;
  aquaTaskType: AquaTaskType;
  suggestedNextDueAt: string;
  intervalDays: number | null;
}

export default function TasksPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const locale = t('aquarium.dateLocale');

  const [reminders, setReminders] = useState<UserReminderDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [scheduleNext, setScheduleNext] = useState<ScheduleNextState | null>(null);

  useEffect(() => {
    getAllReminders()
      .then(setReminders)
      .catch(console.error)
      .finally(() => setLoading(false));
  }, []);

  async function handleComplete(r: UserReminderDto) {
    const res = await completeReminder(r.aquariumId, r.id);
    setReminders(prev =>
      prev.map(x => x.id === res.completedId ? { ...x, isCompleted: true, completedAt: new Date().toISOString() } : x),
    );
    if (res.suggestedNextDueAt) {
      setScheduleNext({
        completedId: res.completedId,
        aquariumId: r.aquariumId,
        aquaTaskType: r.aquaTaskType,
        suggestedNextDueAt: res.suggestedNextDueAt,
        intervalDays: r.intervalDays,
      });
    }
  }

  async function handleScheduleNext(dueAt: string) {
    if (!scheduleNext) return;
    const req: CreateReminderRequest = {
      aquaTaskType: scheduleNext.aquaTaskType,
      dueAt,
      intervalDays: scheduleNext.intervalDays,
    };
    const created = await createReminder(scheduleNext.aquariumId, req);
    const existing = reminders.find(r => r.id === scheduleNext.completedId);
    const newReminder: UserReminderDto = {
      ...created,
      aquariumName: existing?.aquariumName ?? '',
    };
    setReminders(prev => [newReminder, ...prev]);
    setScheduleNext(null);
  }

  // Group by aquariumId, preserve order (upcoming first)
  const grouped = reminders.reduce<Record<string, { name: string; items: UserReminderDto[] }>>((acc, r) => {
    if (!acc[r.aquariumId]) acc[r.aquariumId] = { name: r.aquariumName, items: [] };
    acc[r.aquariumId].items.push(r);
    return acc;
  }, {});

  const hasAny = reminders.length > 0;

  return (
    <div className="min-h-screen bg-[#0F172A]">
      {/* Header */}
      <div className="px-4 pt-6 pb-4 sm:px-6">
        <div className="flex items-center gap-3 mb-1">
          <div className="w-8 h-8 rounded-xl bg-sky-500/20 flex items-center justify-center">
            <Bell className="w-4 h-4 text-sky-400" />
          </div>
          <h1 className="text-white font-black text-xl">{t('tasks.title')}</h1>
        </div>
        {hasAny && (
          <p className="text-slate-500 text-sm ml-11">{t('tasks.subtitle')}</p>
        )}
      </div>

      <div className="px-4 pb-24 sm:px-6 space-y-6">
        {loading ? (
          <div className="space-y-4">
            {[1, 2].map(i => (
              <div key={i}>
                <div className="h-4 w-32 bg-slate-800 rounded animate-pulse mb-2" />
                <div className="space-y-2">
                  <div className="h-16 bg-slate-800/40 rounded-xl animate-pulse" />
                  <div className="h-16 bg-slate-800/40 rounded-xl animate-pulse" />
                </div>
              </div>
            ))}
          </div>
        ) : !hasAny ? (
          <div className="flex flex-col items-center justify-center py-20 text-center">
            <div className="w-16 h-16 rounded-2xl bg-slate-800/50 flex items-center justify-center mb-4">
              <Bell className="w-8 h-8 text-slate-600" />
            </div>
            <p className="text-slate-400 font-semibold mb-1">{t('tasks.empty')}</p>
            <p className="text-slate-600 text-sm mb-6 max-w-xs">{t('tasks.emptyHint')}</p>
            <button
              onClick={() => navigate('/tanks')}
              className="flex items-center gap-2 px-4 py-2.5 rounded-xl bg-sky-500/20 border border-sky-500/40 text-sky-300 hover:bg-sky-500/30 transition-colors font-semibold text-sm min-h-[44px]"
            >
              <Fish className="w-4 h-4" />
              {t('tasks.goToTanks')}
            </button>
          </div>
        ) : (
          Object.entries(grouped).map(([aquariumId, group]) => (
            <div key={aquariumId}>
              {/* Tank header */}
              <div className="flex items-center gap-2 mb-2">
                <Fish className="w-3.5 h-3.5 text-slate-500" />
                <h2 className="text-slate-300 font-bold text-sm">{group.name}</h2>
                <span className="text-slate-600 text-xs">({group.items.length})</span>
              </div>

              {/* Reminder list */}
              <div className="space-y-2">
                {group.items.map(r => {
                  const status = getReminderStatus(r);
                  const isWater = r.aquaTaskType === AquaTaskType.WaterChange;
                  return (
                    <div
                      key={r.id}
                      className={cn(
                        'flex items-center gap-3 rounded-xl border px-3 py-2.5',
                        status === 'done'     && 'bg-slate-800/20 border-slate-800/40 opacity-60',
                        status === 'overdue'  && 'bg-red-500/5 border-red-500/30',
                        status === 'upcoming' && 'bg-slate-800/30 border-slate-800/60',
                      )}
                    >
                      {/* Icon */}
                      <div className={cn(
                        'w-9 h-9 rounded-lg flex items-center justify-center flex-shrink-0',
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
                        <span className="text-[10px] text-emerald-400 font-medium flex-shrink-0">{t('tasks.done')}</span>
                      )}
                      {status === 'overdue' && (
                        <span className="text-[10px] text-red-400 font-medium flex-shrink-0">{t('tasks.overdue')}</span>
                      )}

                      {/* Complete button */}
                      {!r.isCompleted && (
                        <button
                          onClick={() => handleComplete(r)}
                          className="p-1.5 rounded-lg hover:bg-emerald-500/20 transition-colors min-h-[36px] min-w-[36px] flex items-center justify-center flex-shrink-0"
                          title={t('reminders.markDone')}
                        >
                          <CheckCircle className="w-4 h-4 text-emerald-400" />
                        </button>
                      )}
                    </div>
                  );
                })}
              </div>
            </div>
          ))
        )}
      </div>

      {scheduleNext && (
        <ScheduleNextModal
          suggestedNextDueAt={scheduleNext.suggestedNextDueAt}
          onSchedule={handleScheduleNext}
          onSkip={() => setScheduleNext(null)}
        />
      )}
    </div>
  );
}

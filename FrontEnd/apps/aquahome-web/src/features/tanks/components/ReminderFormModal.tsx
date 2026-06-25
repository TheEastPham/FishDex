import { useState } from 'react';
import { X, Droplets, Filter } from 'lucide-react';
import { AquaTaskType, useTranslation, cn } from '@fishlover/shared';
import type { CreateReminderRequest } from '@fishlover/shared';

const INTERVAL_OPTIONS = [null, 7, 14, 30, 60, 90] as const;

function toLocalDatetimeValue(date: Date): string {
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

interface Props {
  onClose: () => void;
  onSubmit: (req: CreateReminderRequest) => Promise<void>;
}

export default function ReminderFormModal({ onClose, onSubmit }: Props) {
  const { t } = useTranslation();
  const [taskType, setTaskType] = useState<AquaTaskType>(AquaTaskType.WaterChange);
  const [dueAt, setDueAt] = useState(() => toLocalDatetimeValue(new Date()));
  const [intervalDays, setIntervalDays] = useState<number | null>(7);
  const [saving, setSaving] = useState(false);

  async function handleSubmit() {
    setSaving(true);
    try {
      await onSubmit({
        aquaTaskType: taskType,
        dueAt: new Date(dueAt).toISOString(),
        intervalDays,
      });
    } finally {
      setSaving(false);
    }
  }

  return (
    <>
      <div className="fixed inset-0 bg-black/60 z-50" onClick={onClose} />
      <div className="fixed bottom-0 left-0 right-0 z-50 bg-[#172033] rounded-t-2xl shadow-2xl">
        {/* Handle */}
        <div className="flex justify-center pt-3 pb-1">
          <div className="w-10 h-1 rounded-full bg-slate-600" />
        </div>

        {/* Header */}
        <div className="flex items-center justify-between px-5 py-3 border-b border-slate-800/60">
          <h3 className="text-white font-bold text-base">{t('reminders.addTitle')}</h3>
          <button onClick={onClose} className="p-2 rounded-lg hover:bg-slate-700/50 transition-colors min-h-[44px] min-w-[44px] flex items-center justify-center">
            <X className="w-4 h-4 text-slate-400" />
          </button>
        </div>

        <div className="px-5 py-4 space-y-5 pb-8">
          {/* Task type */}
          <div>
            <p className="text-slate-400 text-xs font-medium mb-2">{t('reminders.typeLabel')}</p>
            <div className="grid grid-cols-2 gap-2">
              <button
                onClick={() => setTaskType(AquaTaskType.WaterChange)}
                className={cn(
                  'flex items-center justify-center gap-2 py-3 rounded-xl border font-semibold text-sm transition-colors min-h-[52px]',
                  taskType === AquaTaskType.WaterChange
                    ? 'bg-sky-500/20 border-sky-500/60 text-sky-300'
                    : 'bg-slate-800/50 border-slate-700 text-slate-400 hover:border-slate-600',
                )}
              >
                <Droplets className="w-4 h-4" />
                {t('reminders.waterChange')}
              </button>
              <button
                onClick={() => setTaskType(AquaTaskType.FilterCleaning)}
                className={cn(
                  'flex items-center justify-center gap-2 py-3 rounded-xl border font-semibold text-sm transition-colors min-h-[52px]',
                  taskType === AquaTaskType.FilterCleaning
                    ? 'bg-amber-500/20 border-amber-500/60 text-amber-300'
                    : 'bg-slate-800/50 border-slate-700 text-slate-400 hover:border-slate-600',
                )}
              >
                <Filter className="w-4 h-4" />
                {t('reminders.filterCleaning')}
              </button>
            </div>
          </div>

          {/* Due date */}
          <div>
            <label className="block text-slate-400 text-xs font-medium mb-2">{t('reminders.dueAt')}</label>
            <input
              type="datetime-local"
              value={dueAt}
              onChange={e => setDueAt(e.target.value)}
              className="w-full bg-slate-800/50 border border-slate-700 rounded-xl px-3 py-2.5 text-white text-base focus:outline-none focus:border-sky-500 min-h-[44px]"
            />
          </div>

          {/* Interval */}
          <div>
            <p className="text-slate-400 text-xs font-medium mb-2">{t('reminders.intervalLabel')}</p>
            <div className="flex flex-wrap gap-2">
              {INTERVAL_OPTIONS.map(opt => (
                <button
                  key={String(opt)}
                  onClick={() => setIntervalDays(opt)}
                  className={cn(
                    'px-3 py-1.5 rounded-lg border text-sm font-medium transition-colors min-h-[36px]',
                    intervalDays === opt
                      ? 'bg-sky-500/20 border-sky-500/60 text-sky-300'
                      : 'bg-slate-800/50 border-slate-700 text-slate-400 hover:border-slate-600',
                  )}
                >
                  {opt === null ? t('reminders.intervalNone') : t('reminders.intervalDays', { count: opt })}
                </button>
              ))}
            </div>
          </div>

          {/* Submit */}
          <button
            onClick={handleSubmit}
            disabled={saving}
            className="w-full py-3 rounded-xl bg-sky-500 hover:bg-sky-600 disabled:opacity-50 text-white font-bold text-sm transition-colors min-h-[48px]"
          >
            {saving ? t('reminders.saving') : t('reminders.confirm')}
          </button>
        </div>
      </div>
    </>
  );
}

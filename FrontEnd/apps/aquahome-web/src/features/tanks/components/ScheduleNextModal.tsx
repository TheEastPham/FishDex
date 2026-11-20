import { useState } from 'react';
import { CalendarCheck } from 'lucide-react';
import { useTranslation } from '@fishlover/shared';

function toLocalDatetimeValue(iso: string): string {
  const d = new Date(iso);
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

interface Props {
  suggestedNextDueAt: string;
  onSchedule: (dueAt: string) => Promise<void>;
  onSkip: () => void;
}

export default function ScheduleNextModal({ suggestedNextDueAt, onSchedule, onSkip }: Props) {
  const { t } = useTranslation();
  const [dueAt, setDueAt] = useState(() => toLocalDatetimeValue(suggestedNextDueAt));
  const [saving, setSaving] = useState(false);

  async function handleSchedule() {
    setSaving(true);
    try {
      await onSchedule(new Date(dueAt).toISOString());
    } finally {
      setSaving(false);
    }
  }

  return (
    <>
      <div className="fixed inset-0 bg-black/60 z-50" onClick={onSkip} />
      <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
        <div className="bg-[#1E293B] border border-slate-700 rounded-2xl p-6 w-full max-w-sm shadow-2xl">
          <div className="flex items-center gap-3 mb-4">
            <div className="w-10 h-10 rounded-xl bg-emerald-500/20 flex items-center justify-center">
              <CalendarCheck className="w-5 h-5 text-emerald-400" />
            </div>
            <h3 className="text-white font-bold text-base">{t('reminders.scheduleNextTitle')}</h3>
          </div>

          <label className="block text-slate-400 text-xs font-medium mb-2">{t('reminders.scheduleNextHint')}</label>
          <input
            type="datetime-local"
            value={dueAt}
            onChange={e => setDueAt(e.target.value)}
            className="w-full bg-slate-800/50 border border-slate-700 rounded-xl px-3 py-2.5 text-white text-base focus:outline-none focus:border-emerald-500 mb-4 min-h-[44px]"
          />

          <div className="flex gap-2">
            <button
              onClick={onSkip}
              className="flex-1 py-2.5 rounded-xl border border-slate-700 text-slate-400 hover:bg-slate-700/50 font-semibold text-sm transition-colors min-h-[44px]"
            >
              {t('reminders.skipNext')}
            </button>
            <button
              onClick={handleSchedule}
              disabled={saving}
              className="flex-1 py-2.5 rounded-xl bg-emerald-500 hover:bg-emerald-600 disabled:opacity-50 text-white font-bold text-sm transition-colors min-h-[44px]"
            >
              {saving ? t('reminders.saving') : t('reminders.scheduleNext')}
            </button>
          </div>
        </div>
      </div>
    </>
  );
}

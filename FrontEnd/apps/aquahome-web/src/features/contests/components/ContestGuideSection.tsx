import { useState } from 'react';
import { useTranslation, cn } from '@fishlover/shared';
import { Info, Video, Music, Trophy, Users, ChevronDown } from 'lucide-react';

interface StepProps { icon: React.ReactNode; title: string; body: string }
function Step({ icon, title, body }: StepProps) {
  return (
    <div className="flex gap-3">
      <div className="w-9 h-9 rounded-xl bg-sky-500/10 border border-sky-500/20 flex items-center justify-center shrink-0 text-sky-400">
        {icon}
      </div>
      <div className="min-w-0">
        <p className="text-sm font-semibold text-white">{title}</p>
        <p className="text-xs text-slate-500 mt-0.5 leading-relaxed">{body}</p>
      </div>
    </div>
  );
}

/**
 * Thể lệ/hướng dẫn chung — nội dung TĨNH, giống nhau cho mọi contest (không phụ thuộc dữ liệu contest cụ thể).
 * Giúp user hiểu luật chơi chung trước khi xem chi tiết từng contest bên dưới.
 */
export default function ContestGuideSection() {
  const { t } = useTranslation();
  const [open, setOpen] = useState(true);

  return (
    <div className="rounded-2xl border border-slate-800/60 bg-[#1E293B] mb-6 overflow-hidden">
      <button
        onClick={() => setOpen(v => !v)}
        className="w-full flex items-center justify-between gap-2 px-4 py-3.5 min-h-[44px]"
      >
        <span className="flex items-center gap-2 text-sm font-bold text-white">
          <Info className="w-4 h-4 text-sky-400" /> {t('contests.guideTitle')}
        </span>
        <ChevronDown className={cn('w-4 h-4 text-slate-500 transition-transform', open && 'rotate-180')} />
      </button>

      {open && (
        <div className="px-4 pb-5 pt-1 space-y-4 border-t border-slate-800/60">
          <Step
            icon={<Video className="w-4 h-4" />}
            title={t('contests.guideVideoTitle')}
            body={t('contests.guideVideoBody')}
          />
          <Step
            icon={<Music className="w-4 h-4" />}
            title={t('contests.guideCopyrightTitle')}
            body={t('contests.guideCopyrightBody')}
          />
          <Step
            icon={<Trophy className="w-4 h-4" />}
            title={t('contests.guidePrizeTitle')}
            body={t('contests.guidePrizeBody')}
          />
          <Step
            icon={<Users className="w-4 h-4" />}
            title={t('contests.guideProcessTitle')}
            body={t('contests.guideProcessBody')}
          />
        </div>
      )}
    </div>
  );
}

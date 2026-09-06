import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation, getSpeciesSummaries } from '@fishlover/shared';
import type { SpeciesSummary } from '@fishlover/shared';
import { Fish, Lock, Clock } from 'lucide-react';

interface Props {
  specCode: number;
  limit: number;
  resetsInSeconds: number;
}

function formatReset(seconds: number, hourLabel: string, minuteLabel: string): string {
  const h = Math.floor(seconds / 3600);
  const m = Math.floor((seconds % 3600) / 60);
  return h > 0 ? `${h}${hourLabel} ${m}${minuteLabel}` : `${m}${minuteLabel}`;
}

/**
 * Màn hết lượt xem trong ngày.
 *
 * Cố ý KHÔNG phải một cái khoá trắng: vẫn hiện tên loài và ảnh đại diện — mấy field này vốn đã
 * công khai qua endpoint summaries (không trừ lượt), nên giấu đi chẳng được gì mà link chia sẻ
 * gửi cho bạn bè mở ra lại thành một trang chết. Và nói rõ bao giờ hạn mức reset: người dùng phải
 * biết mình vẫn quay lại được mà không cần đăng ký.
 */
export default function QuotaWall({ specCode, limit, resetsInSeconds }: Props) {
  const { t, i18n } = useTranslation();
  const navigate = useNavigate();
  const [summary, setSummary] = useState<SpeciesSummary | null>(null);

  useEffect(() => {
    let cancelled = false;
    getSpeciesSummaries([specCode], i18n.language)
      .then((list) => { if (!cancelled) setSummary(list[0] ?? null); })
      .catch(() => { if (!cancelled) setSummary(null); });
    return () => { cancelled = true; };
  }, [specCode, i18n.language]);

  return (
    <div className="min-h-screen bg-[#141518] flex items-center justify-center px-6 py-10">
      <div className="w-full max-w-sm rounded-2xl border border-slate-700/60 bg-[#1a1c20] overflow-hidden shadow-2xl">

        <div className="relative h-44 bg-[#0e0f11] flex items-center justify-center">
          {summary?.imageUrl ? (
            <img src={summary.imageUrl} alt={summary.speciesName} className="w-full h-full object-cover opacity-60" />
          ) : (
            <Fish className="w-14 h-14 text-slate-700" />
          )}
          <div className="absolute inset-0 bg-gradient-to-t from-[#1a1c20] via-[#1a1c20]/40 to-transparent" />
        </div>

        <div className="px-6 pb-6 -mt-8 relative text-center">
          <h1 className="text-xl font-bold text-white italic">{summary?.speciesName ?? `#${specCode}`}</h1>
          {summary?.commonName && (
            <p className="text-sm text-slate-400 mt-1">{summary.commonName}</p>
          )}

          <div className="mt-6 rounded-xl border border-amber-500/30 bg-amber-500/10 p-4">
            <Lock className="w-8 h-8 text-amber-400 mx-auto mb-3" />
            <p className="text-amber-200 font-semibold">{t('quota.title', { limit })}</p>
            <p className="text-sm text-amber-300/80 mt-2">{t('quota.body')}</p>
            <p className="text-xs text-amber-300/70 mt-3 inline-flex items-center gap-1.5">
              <Clock className="w-3.5 h-3.5" />
              {t('quota.resetsIn', {
                time: formatReset(resetsInSeconds, t('quota.hourShort'), t('quota.minuteShort')),
              })}
            </p>
          </div>

          <button
            onClick={() => navigate('/login')}
            className="mt-5 w-full min-h-[44px] rounded-lg bg-emerald-500/20 hover:bg-emerald-500/30 border border-emerald-500/40 text-emerald-100 font-semibold transition-colors"
          >
            {t('quota.loginCta')}
          </button>
          <button
            onClick={() => navigate('/register')}
            className="mt-2 w-full min-h-[44px] rounded-lg border border-slate-700 text-slate-300 hover:bg-slate-800 transition-colors"
          >
            {t('quota.registerCta')}
          </button>
          <button
            onClick={() => navigate(-1)}
            className="mt-3 text-sm text-slate-400 underline underline-offset-2 hover:text-slate-200"
          >
            {t('common.back')}
          </button>
        </div>
      </div>
    </div>
  );
}

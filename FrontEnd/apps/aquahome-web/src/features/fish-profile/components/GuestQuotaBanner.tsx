import { Link } from 'react-router-dom';
import { useTranslation, useAnonQuotaStore, useAuthStore } from '@fishlover/shared';
import { Eye } from 'lucide-react';

/**
 * Đếm ngược số loài khách còn xem được hôm nay.
 *
 * Hiện ngay từ lượt đầu chứ không đợi gần hết: hạn mức 20 loài/ngày là mức người dùng thật sẽ
 * chạm, mà một cái tường bật lên bất ngờ thì bị đọc là lỗi chứ không phải là chính sách.
 */
export default function GuestQuotaBanner() {
  const { t } = useTranslation();
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const { known, limit, remaining } = useAnonQuotaStore();

  if (isAuthenticated || !known || limit <= 0) return null;

  const low = remaining <= 5;

  return (
    <div className="max-w-7xl mx-auto w-full px-4 pt-3">
      <div
        className={
          'flex flex-wrap items-center justify-center gap-x-2 gap-y-1 rounded-xl border px-4 py-2.5 text-xs ' +
          (low
            ? 'border-amber-500/30 bg-amber-500/10 text-amber-200'
            : 'border-slate-700/60 bg-[#1a1c20] text-slate-400')
        }
      >
        <Eye className="w-3.5 h-3.5 shrink-0" />
        <span>{t('quota.remaining', { remaining, limit })}</span>
        <Link to="/login" className="font-semibold underline underline-offset-2 hover:text-white">
          {t('quota.bannerCta')}
        </Link>
      </div>
    </div>
  );
}

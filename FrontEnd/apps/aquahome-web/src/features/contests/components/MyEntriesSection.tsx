import { useState } from 'react';
import { confirmEntryUpload, ContestEntryStatus, cn } from '@fishlover/shared';
import type { ContestEntryDto, useTranslation } from '@fishlover/shared';
import { Loader2, RotateCw, Youtube } from 'lucide-react';

/** Badge trạng thái bài dự thi — cho người nộp biết bài đang ở bước nào. */
export function EntryStatusBadge({ status, t }: { status: ContestEntryStatus; t: ReturnType<typeof useTranslation>['t'] }) {
  const [label, cls] = {
    [ContestEntryStatus.Pending]:       [t('contests.stPending'),   'bg-slate-500/15 text-slate-400 border-slate-500/30'],
    [ContestEntryStatus.Queued]:        [t('contests.stQueued'),    'bg-sky-500/15 text-sky-400 border-sky-500/30'],
    [ContestEntryStatus.UploadedDraft]: [t('contests.stReview'),    'bg-amber-500/15 text-amber-300 border-amber-500/30'],
    [ContestEntryStatus.Published]:     [t('contests.stPublished'), 'bg-emerald-500/15 text-emerald-400 border-emerald-500/30'],
    [ContestEntryStatus.Rejected]:      [t('contests.stRejected'),  'bg-red-500/10 text-red-400 border-red-500/30'],
    [ContestEntryStatus.Failed]:        [t('contests.stFailed'),    'bg-orange-500/10 text-orange-300 border-orange-500/30'],
  }[status] ?? [t('contests.stPending'), 'bg-slate-500/15 text-slate-400 border-slate-500/30'];

  return <span className={cn('shrink-0 px-2 py-0.5 rounded-full border text-[10px] font-bold', cls)}>{label}</span>;
}

/** Bài dự thi của chính user — trước đây nộp xong không có chỗ nào theo dõi. */
export default function MyEntriesSection({ contestId, entries, t, onChanged }: {
  contestId: string;
  entries: ContestEntryDto[];
  t: ReturnType<typeof useTranslation>['t'];
  /** Load lại danh sách sau khi xác nhận lại — bài có thể đã đổi trạng thái kể cả khi request lỗi. */
  onChanged: () => void;
}) {
  const [retryingId, setRetryingId] = useState<string | null>(null);
  const [retryError, setRetryError] = useState<{ id: string; message: string } | null>(null);

  if (entries.length === 0) return null;

  // Bài Pending = đã nộp nhưng bước xác nhận chưa xong (mất mạng, R2 trục trặc). Hết lượt YouTube thì bài ở hàng chờ, không cần bấm lại.
  const handleRetry = async (entryId: string) => {
    setRetryingId(entryId);
    setRetryError(null);
    try {
      await confirmEntryUpload(contestId, entryId);
    } catch (err: unknown) {
      const res = (err as { response?: { status?: number; data?: { message?: string } } })?.response;
      setRetryError({
        id: entryId,
        // 422 luôn kèm message cụ thể từ BE (vd "hôm nay đã hết lượt đăng video") — hiện thẳng cho user
        message: res?.status === 422 ? (res.data?.message ?? t('contests.errSubmit')) : t('contests.errSubmit'),
      });
    } finally {
      setRetryingId(null);
      onChanged();
    }
  };

  return (
    <div className="px-3 pb-3">
      <p className="text-[10px] font-bold uppercase tracking-wider text-slate-600 px-3 pb-2">
        {t('contests.myEntries')} ({entries.length})
      </p>
      <div className="space-y-2">
        {entries.map(e => (
          <div key={e.id} className="rounded-xl bg-[#0F172A] border border-slate-800 p-3">
            <div className="flex items-center gap-2">
              <p className="text-sm font-semibold text-white truncate flex-1 min-w-0">
                {e.title || e.aquariumName || t('contests.untitledEntry')}
              </p>
              <EntryStatusBadge status={e.status} t={t} />
            </div>
            {e.aquariumName && e.title && (
              <p className="text-xs text-slate-600 mt-0.5 truncate">{e.aquariumName}</p>
            )}
            {(e.status === ContestEntryStatus.Rejected || e.status === ContestEntryStatus.Failed) && e.rejectionReason && (
              <p className="text-xs text-red-400/90 mt-1.5 leading-relaxed">{e.rejectionReason}</p>
            )}
            {e.status === ContestEntryStatus.Queued && (
              <p className="text-xs text-slate-500 mt-1.5 leading-relaxed">{t('contests.queuedHint')}</p>
            )}
            {e.status === ContestEntryStatus.Pending && (
              <div className="mt-2">
                <p className="text-xs text-slate-500 leading-relaxed">{t('contests.pendingHint')}</p>
                <button
                  onClick={() => handleRetry(e.id)}
                  disabled={retryingId !== null}
                  className="mt-2 inline-flex items-center gap-1.5 px-3 py-2 rounded-lg bg-sky-500/15 border border-sky-500/30 text-sky-300 text-xs font-bold hover:bg-sky-500/25 transition-colors min-h-[44px] disabled:opacity-50"
                >
                  {retryingId === e.id
                    ? <Loader2 className="w-3.5 h-3.5 animate-spin" />
                    : <RotateCw className="w-3.5 h-3.5" />}
                  {t('contests.retryConfirm')}
                </button>
                {retryError?.id === e.id && (
                  <p className="text-xs text-red-400/90 mt-1.5 leading-relaxed">{retryError.message}</p>
                )}
              </div>
            )}
            {e.youTubeVideoId && e.status === ContestEntryStatus.Published && (
              <a
                href={`https://www.youtube.com/watch?v=${e.youTubeVideoId}`}
                target="_blank"
                rel="noopener noreferrer"
                className="inline-flex items-center gap-1.5 text-xs text-red-400 hover:underline mt-1.5"
              >
                <Youtube className="w-3.5 h-3.5" /> {t('contests.watchVideo')}
              </a>
            )}
          </div>
        ))}
      </div>
    </div>
  );
}

import { useEffect, useRef, useState } from 'react';
import {
  getMySnapshots,
  submitContestEntry, confirmEntryUpload, uploadToR2,
  useTranslation, cn,
} from '@fishlover/shared';
import type { ContestDto, MySnapshotDto } from '@fishlover/shared';
import { Loader2, Upload, Video, Check, X, AlertTriangle } from 'lucide-react';

// Video contest: 2–5 phút, landscape, tối đa 500MB — khớp validate BE
const MIN_DURATION_S = 120;
const MAX_DURATION_S = 300;
const MAX_SIZE_BYTES = 500 * 1024 * 1024;
const ALLOWED_TYPES = ['video/mp4', 'video/quicktime', 'video/webm'];
// Khớp giới hạn validate ở BE (ContestService)
const MAX_TITLE_LEN = 100;
const MAX_DESC_LEN = 100;

interface VideoMeta {
  durationSeconds: number;
  width: number;
  height: number;
}

function readVideoMetadata(file: File): Promise<VideoMeta> {
  return new Promise((resolve, reject) => {
    const url = URL.createObjectURL(file);
    const video = document.createElement('video');
    video.preload = 'metadata';
    video.onloadedmetadata = () => {
      URL.revokeObjectURL(url);
      resolve({
        durationSeconds: video.duration,
        width: video.videoWidth,
        height: video.videoHeight,
      });
    };
    video.onerror = () => {
      URL.revokeObjectURL(url);
      reject(new Error('Cannot read video metadata'));
    };
    video.src = url;
  });
}

interface Props {
  contest: ContestDto;
  onClose: () => void;
  onSubmitted: () => void;
}

type Step = 'form' | 'uploading' | 'done';

export default function ContestEntryFormModal({ contest, onClose, onSubmitted }: Props) {
  const { t } = useTranslation();
  const fileInputRef = useRef<HTMLInputElement>(null);

  const [snapshots, setSnapshots] = useState<MySnapshotDto[]>([]);
  const [snapshotsLoading, setSnapshotsLoading] = useState(true);
  const [selectedSnapshotId, setSelectedSnapshotId] = useState('');

  const [file, setFile] = useState<File | null>(null);
  const [videoMeta, setVideoMeta] = useState<VideoMeta | null>(null);
  const [videoError, setVideoError] = useState<string | null>(null);
  const [copyrightChecked, setCopyrightChecked] = useState(false);

  // Tên video mặc định = tên bể đang chọn; chỉ ngừng tự điền khi user đã tự gõ.
  const [title, setTitle] = useState('');
  const [titleTouched, setTitleTouched] = useState(false);
  const [description, setDescription] = useState('');

  const [step, setStep] = useState<Step>('form');
  const [progress, setProgress] = useState(0);
  const [submitError, setSubmitError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    getMySnapshots()
      .then(list => { if (!cancelled) setSnapshots(list); })
      .catch(() => { if (!cancelled) setSnapshots([]); })
      .finally(() => { if (!cancelled) setSnapshotsLoading(false); });
    return () => { cancelled = true; };
  }, []);

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const selected = e.target.files?.[0];
    if (!selected) return;
    setVideoError(null);
    setVideoMeta(null);
    setFile(null);

    if (!ALLOWED_TYPES.includes(selected.type)) {
      setVideoError(t('contests.errFormat'));
      return;
    }
    if (selected.size > MAX_SIZE_BYTES) {
      setVideoError(t('contests.errTooLarge'));
      return;
    }

    try {
      const meta = await readVideoMetadata(selected);

      // Landscape only: width > height
      if (meta.width <= meta.height) {
        setVideoError(t('contests.errPortrait'));
        return;
      }
      if (meta.durationSeconds < MIN_DURATION_S) {
        setVideoError(t('contests.errTooShort'));
        return;
      }
      if (meta.durationSeconds > MAX_DURATION_S) {
        setVideoError(t('contests.errTooLong'));
        return;
      }

      setFile(selected);
      setVideoMeta(meta);
    } catch {
      setVideoError(t('contests.errUnreadable'));
    }
  };

  const handleSnapshotChange = (snapshotId: string) => {
    setSelectedSnapshotId(snapshotId);
    if (titleTouched) return; // user đã tự đặt tên thì không ghi đè
    setTitle(snapshots.find(s => s.id === snapshotId)?.aquariumName ?? '');
  };

  const canSubmit = !!file && !!videoMeta && !!selectedSnapshotId && copyrightChecked && !videoError;

  const handleSubmit = async () => {
    if (!canSubmit || !file || !videoMeta) return;
    setStep('uploading');
    setSubmitError(null);
    setProgress(0);

    try {
      const { entryId, uploadUrl } = await submitContestEntry(contest.id, {
        aquariumSnapshotId: selectedSnapshotId,
        fileName: file.name,
        contentType: file.type,
        fileSizeBytes: file.size,
        videoDurationSeconds: Math.round(videoMeta.durationSeconds),
        title: title.trim() || null,
        description: description.trim() || null,
      });

      await uploadToR2(uploadUrl, file, file.type, setProgress);
      await confirmEntryUpload(contest.id, entryId);

      setStep('done');
      onSubmitted();
    } catch (err: unknown) {
      const res = (err as { response?: { status?: number; data?: { message?: string } } })?.response;
      setSubmitError(
        res?.status === 503 ? t('contests.errOverloaded')
        // 422 luôn kèm message cụ thể từ BE (vd "đã dự thi bằng bể này rồi") — hiện thẳng cho user
        : res?.status === 422 ? (res.data?.message ?? t('contests.errValidation'))
        : t('contests.errSubmit'),
      );
      setStep('form');
    }
  };

  const durationLabel = videoMeta
    ? `${Math.floor(videoMeta.durationSeconds / 60)}:${String(Math.round(videoMeta.durationSeconds % 60)).padStart(2, '0')}`
    : null;

  return (
    <>
      <div className="fixed inset-0 bg-black/60 z-50" onClick={step === 'uploading' ? undefined : onClose} />
      <div className="fixed inset-0 z-50 flex items-center justify-center p-4 pointer-events-none">
        <div className="w-full max-w-md max-h-[90vh] overflow-y-auto bg-[#172033] border border-slate-700 rounded-2xl pointer-events-auto">

          {/* Header */}
          <div className="flex items-center justify-between px-5 py-4 border-b border-slate-800">
            <div>
              <h2 className="text-base font-bold text-white">{t('contests.entryTitle')}</h2>
              <p className="text-xs text-slate-500 mt-0.5 truncate">{contest.title}</p>
            </div>
            {step !== 'uploading' && (
              <button onClick={onClose} className="p-2 rounded-lg hover:bg-white/10 text-slate-500 min-w-[44px] min-h-[44px] flex items-center justify-center">
                <X className="w-4 h-4" />
              </button>
            )}
          </div>

          <div className="p-5">
            {/* ── Done ── */}
            {step === 'done' && (
              <div className="text-center py-6">
                <div className="w-14 h-14 rounded-2xl bg-emerald-500/10 border border-emerald-500/30 flex items-center justify-center mx-auto mb-4">
                  <Check className="w-7 h-7 text-emerald-400" />
                </div>
                <p className="text-white font-bold mb-1">{t('contests.submitSuccess')}</p>
                <p className="text-xs text-slate-500">{t('contests.submitSuccessHint')}</p>
                <button
                  onClick={onClose}
                  className="mt-5 px-6 py-3 rounded-xl bg-sky-500 text-white text-sm font-bold hover:bg-sky-400 transition-colors min-h-[44px]"
                >
                  {t('contests.close')}
                </button>
              </div>
            )}

            {/* ── Uploading ── */}
            {step === 'uploading' && (
              <div className="py-8 text-center">
                <Loader2 className="w-8 h-8 animate-spin text-sky-400 mx-auto mb-4" />
                <p className="text-sm text-white font-semibold mb-3">{t('contests.uploading')}</p>
                <div className="h-2 rounded-full bg-slate-800 overflow-hidden">
                  <div
                    className="h-full bg-sky-500 rounded-full transition-all duration-300"
                    style={{ width: `${progress}%` }}
                  />
                </div>
                <p className="text-xs text-slate-500 mt-2">{progress}%</p>
              </div>
            )}

            {/* ── Form ── */}
            {step === 'form' && (
              <div className="space-y-5">
                {submitError && (
                  <div className="rounded-xl border border-red-500/30 bg-red-500/10 p-3 text-sm text-red-400">
                    {submitError}
                  </div>
                )}

                {/* Snapshot select */}
                <div>
                  <label className="block text-xs font-bold text-slate-500 uppercase tracking-wider mb-2">
                    {t('contests.snapshotLabel')}
                  </label>
                  {snapshotsLoading ? (
                    <div className="h-11 rounded-xl bg-slate-800/50 animate-pulse" />
                  ) : snapshots.length === 0 ? (
                    <div className="flex items-center gap-2 rounded-xl border border-amber-500/30 bg-amber-500/10 p-3 text-sm text-amber-400">
                      <AlertTriangle className="w-4 h-4 shrink-0" />
                      {t('contests.noSnapshots')}
                    </div>
                  ) : (
                    <select
                      value={selectedSnapshotId}
                      onChange={e => handleSnapshotChange(e.target.value)}
                      className="w-full bg-[#0F172A] border border-slate-700 rounded-xl px-3 py-3 text-base text-white min-h-[44px]"
                    >
                      <option value="">{t('contests.snapshotPlaceholder')}</option>
                      {snapshots.map(s => (
                        <option key={s.id} value={s.id}>{s.aquariumName}</option>
                      ))}
                    </select>
                  )}
                </div>

                {/* Tên video — mặc định lấy tên bể, cho user sửa */}
                <div>
                  <label className="block text-xs font-bold text-slate-500 uppercase tracking-wider mb-2">
                    {t('contests.videoTitleLabel')}
                  </label>
                  <input
                    value={title}
                    onChange={e => { setTitle(e.target.value.slice(0, MAX_TITLE_LEN)); setTitleTouched(true); }}
                    placeholder={t('contests.videoTitlePlaceholder')}
                    className="w-full bg-[#0F172A] border border-slate-700 rounded-xl px-3 py-3 text-base text-white placeholder:text-slate-600 min-h-[44px]"
                  />
                  <p className="text-xs text-slate-600 mt-1.5">{title.length}/{MAX_TITLE_LEN}</p>
                </div>

                {/* Mô tả — tối đa 100 ký tự */}
                <div>
                  <label className="block text-xs font-bold text-slate-500 uppercase tracking-wider mb-2">
                    {t('contests.videoDescLabel')}
                  </label>
                  <textarea
                    value={description}
                    onChange={e => setDescription(e.target.value.slice(0, MAX_DESC_LEN))}
                    rows={2}
                    placeholder={t('contests.videoDescPlaceholder')}
                    className="w-full bg-[#0F172A] border border-slate-700 rounded-xl px-3 py-3 text-base text-white placeholder:text-slate-600 resize-none"
                  />
                  <p className="text-xs text-slate-600 mt-1.5">{description.length}/{MAX_DESC_LEN}</p>
                </div>

                {/* Video picker */}
                <div>
                  <label className="block text-xs font-bold text-slate-500 uppercase tracking-wider mb-2">
                    {t('contests.videoLabel')}
                  </label>
                  <input
                    ref={fileInputRef}
                    type="file"
                    accept="video/mp4,video/quicktime,video/webm"
                    onChange={handleFileChange}
                    className="hidden"
                  />
                  <button
                    onClick={() => fileInputRef.current?.click()}
                    className={cn(
                      'w-full flex items-center gap-3 rounded-xl border-2 border-dashed p-4 transition-colors text-left min-h-[44px]',
                      file ? 'border-emerald-500/40 bg-emerald-500/5' : 'border-slate-700 hover:border-sky-500/40',
                    )}
                  >
                    {file && videoMeta ? (
                      <>
                        <Video className="w-5 h-5 text-emerald-400 shrink-0" />
                        <div className="min-w-0">
                          <p className="text-sm text-white font-semibold truncate">{file.name}</p>
                          <p className="text-xs text-slate-500">
                            {durationLabel} · {videoMeta.width}×{videoMeta.height} · {(file.size / 1024 / 1024).toFixed(1)} MB
                          </p>
                        </div>
                      </>
                    ) : (
                      <>
                        <Upload className="w-5 h-5 text-slate-500 shrink-0" />
                        <div>
                          <p className="text-sm text-slate-400 font-semibold">{t('contests.videoPick')}</p>
                          <p className="text-xs text-slate-600">{t('contests.videoRequirements')}</p>
                        </div>
                      </>
                    )}
                  </button>
                  {videoError && (
                    <p className="text-xs text-red-400 mt-2 flex items-center gap-1.5">
                      <AlertTriangle className="w-3.5 h-3.5 shrink-0" /> {videoError}
                    </p>
                  )}
                </div>

                {/* Copyright checkbox */}
                <label className="flex items-start gap-3 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={copyrightChecked}
                    onChange={e => setCopyrightChecked(e.target.checked)}
                    className="mt-0.5 w-5 h-5 rounded border-slate-600 bg-[#0F172A] accent-sky-500 shrink-0"
                  />
                  <span className="text-xs text-slate-400 leading-relaxed">
                    {t('contests.copyrightConfirm')}
                  </span>
                </label>

                {/* Submit */}
                <button
                  onClick={handleSubmit}
                  disabled={!canSubmit}
                  className="w-full py-3 rounded-xl bg-sky-500 text-white text-sm font-bold hover:bg-sky-400 transition-colors disabled:opacity-40 disabled:cursor-not-allowed min-h-[44px]"
                >
                  {t('contests.submit')}
                </button>
              </div>
            )}
          </div>
        </div>
      </div>
    </>
  );
}

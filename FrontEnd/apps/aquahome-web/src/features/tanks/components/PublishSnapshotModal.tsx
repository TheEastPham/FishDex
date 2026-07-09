import { useEffect, useState } from 'react';
import {
  previewSnapshot, publishSnapshot, getAquariumMedia, getMySnapshots,
  useTranslation, cn,
} from '@fishlover/shared';
import type { SnapshotPreviewDto, AquariumMediaDto, MySnapshotDto } from '@fishlover/shared';
import { Loader2, Globe, Fish, Check, Copy, X, ImageIcon, RefreshCw, Sparkles } from 'lucide-react';
import SnapshotFishSection from '../../public-tanks/components/SnapshotFishSection';

interface Props {
  aquariumId: string;
  aquariumName: string;
  onClose: () => void;
}

export default function PublishSnapshotModal({ aquariumId, aquariumName, onClose }: Props) {
  const { t } = useTranslation();

  const [preview, setPreview] = useState<SnapshotPreviewDto | null>(null);
  const [media, setMedia] = useState<AquariumMediaDto[]>([]);
  const [existing, setExisting] = useState<MySnapshotDto[]>([]);
  const [coverMediaId, setCoverMediaId] = useState<string | null>(null);
  // null = publish mới (link mới); có giá trị = ghi đè snapshot đã chọn (giữ nguyên link/lượt thích)
  const [targetSnapshotId, setTargetSnapshotId] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [publishing, setPublishing] = useState(false);
  const [error, setError] = useState(false);
  const [publishedSlug, setPublishedSlug] = useState<string | null>(null);
  const [copied, setCopied] = useState(false);

  useEffect(() => {
    setLoading(true);
    Promise.all([
      previewSnapshot(aquariumId),
      getAquariumMedia(aquariumId).catch(() => [] as AquariumMediaDto[]),
      getMySnapshots().catch(() => [] as MySnapshotDto[]),
    ])
      .then(([previewData, mediaList, mySnapshots]) => {
        setPreview(previewData);
        setMedia(mediaList.filter(m => m.url));
        setExisting(mySnapshots.filter(s => s.aquariumId === aquariumId));
      })
      .catch(() => setError(true))
      .finally(() => setLoading(false));
  }, [aquariumId]);

  const handlePublish = async () => {
    setPublishing(true);
    setError(false);
    try {
      const snapshot = await publishSnapshot(aquariumId, { coverMediaId, targetSnapshotId });
      setPublishedSlug(snapshot.slug);
    } catch {
      setError(true);
    } finally {
      setPublishing(false);
    }
  };

  const publicUrl = publishedSlug ? `${window.location.origin}/public/tanks/${publishedSlug}` : null;

  const copyUrl = async () => {
    if (!publicUrl) return;
    await navigator.clipboard.writeText(publicUrl);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  return (
    <>
      <div className="fixed inset-0 bg-black/60 z-50" onClick={onClose} />
      <div className="fixed inset-0 z-50 flex items-center justify-center p-4 pointer-events-none">
        <div className="w-full max-w-2xl max-h-[90vh] overflow-y-auto bg-[#172033] border border-slate-700 rounded-2xl pointer-events-auto">

          {/* Header */}
          <div className="sticky top-0 bg-[#172033] flex items-center justify-between px-5 py-4 border-b border-slate-800 z-10">
            <div>
              <h2 className="text-base font-bold text-white flex items-center gap-2">
                <Globe className="w-4 h-4 text-sky-400" />
                {publishedSlug ? t('publish.successTitle') : t('publish.title')}
              </h2>
              <p className="text-xs text-slate-500 mt-0.5">{aquariumName}</p>
            </div>
            <button onClick={onClose} className="p-2 rounded-lg hover:bg-white/10 text-slate-500 min-w-[44px] min-h-[44px] flex items-center justify-center">
              <X className="w-4 h-4" />
            </button>
          </div>

          <div className="p-5">
            {loading && (
              <div className="flex items-center justify-center py-16 text-slate-600">
                <Loader2 className="w-6 h-6 animate-spin" />
              </div>
            )}

            {!loading && error && !publishedSlug && (
              <div className="rounded-xl border border-red-500/30 bg-red-500/10 p-4 text-sm text-red-400">
                {t('publish.error')}
              </div>
            )}

            {/* ── Success state ── */}
            {publishedSlug && publicUrl && (
              <div className="text-center py-6">
                <div className="w-14 h-14 rounded-2xl bg-emerald-500/10 border border-emerald-500/30 flex items-center justify-center mx-auto mb-4">
                  <Check className="w-7 h-7 text-emerald-400" />
                </div>
                <p className="text-white font-bold mb-1">{t('publish.successMessage')}</p>
                <p className="text-xs text-slate-500 mb-5">{t('publish.successHint')}</p>

                <div className="flex items-center gap-2 bg-[#0F172A] border border-slate-700 rounded-xl p-2 pl-4">
                  <span className="flex-1 text-sm text-sky-400 truncate text-left">{publicUrl}</span>
                  <button
                    onClick={copyUrl}
                    className={cn(
                      'flex items-center gap-1.5 px-3 py-2 rounded-lg text-xs font-bold transition-colors shrink-0 min-h-[44px]',
                      copied
                        ? 'bg-emerald-500/20 text-emerald-400'
                        : 'bg-sky-500 text-white hover:bg-sky-400',
                    )}
                  >
                    {copied ? <Check className="w-3.5 h-3.5" /> : <Copy className="w-3.5 h-3.5" />}
                    {copied ? t('publish.copied') : t('publish.copy')}
                  </button>
                </div>
              </div>
            )}

            {/* ── Preview state ── */}
            {!loading && !publishedSlug && preview && (
              <>
                <p className="text-sm text-slate-400 mb-4">{t('publish.previewHint')}</p>

                {/* Publish mới vs ghi đè bể đã public */}
                {existing.length > 0 && (
                  <div className="mb-5 space-y-2">
                    <p className="text-xs font-bold text-slate-500 uppercase tracking-wider mb-2">{t('publish.modeLabel')}</p>

                    <button
                      onClick={() => setTargetSnapshotId(null)}
                      className={cn(
                        'w-full flex items-center gap-3 p-3 rounded-xl border-2 text-left transition-colors',
                        targetSnapshotId === null ? 'border-sky-500 bg-sky-500/10' : 'border-slate-700 hover:border-slate-500',
                      )}
                    >
                      <Sparkles className="w-4 h-4 text-sky-400 shrink-0" />
                      <div>
                        <p className="text-sm font-semibold text-white">{t('publish.modeNew')}</p>
                        <p className="text-xs text-slate-500">{t('publish.modeNewHint')}</p>
                      </div>
                    </button>

                    {existing.map(s => (
                      <button
                        key={s.id}
                        onClick={() => setTargetSnapshotId(s.id)}
                        className={cn(
                          'w-full flex items-center gap-3 p-3 rounded-xl border-2 text-left transition-colors',
                          targetSnapshotId === s.id ? 'border-sky-500 bg-sky-500/10' : 'border-slate-700 hover:border-slate-500',
                        )}
                      >
                        <RefreshCw className="w-4 h-4 text-amber-400 shrink-0" />
                        <div className="min-w-0 flex-1">
                          <p className="text-sm font-semibold text-white truncate">{t('publish.modeOverwrite')} - /{s.slug}</p>
                          <p className="text-xs text-slate-500">{t('publish.modeOverwriteHint', { count: s.likeCount })}</p>
                        </div>
                      </button>
                    ))}
                  </div>
                )}

                {/* Cover picker */}
                {media.length > 0 && (
                  <div className="mb-5">
                    <p className="text-xs font-bold text-slate-500 uppercase tracking-wider mb-2">{t('publish.coverLabel')}</p>
                    <div className="flex gap-2 overflow-x-auto pb-1">
                      <button
                        onClick={() => setCoverMediaId(null)}
                        className={cn(
                          'w-16 h-16 rounded-lg border-2 shrink-0 flex items-center justify-center bg-[#0F172A] transition-colors',
                          coverMediaId === null ? 'border-sky-500' : 'border-slate-700 hover:border-slate-500',
                        )}
                        title={t('publish.noCover')}
                      >
                        <ImageIcon className="w-5 h-5 text-slate-600" />
                      </button>
                      {media.map(m => (
                        <button
                          key={m.id}
                          onClick={() => setCoverMediaId(m.id)}
                          className={cn(
                            'w-16 h-16 rounded-lg border-2 shrink-0 overflow-hidden transition-colors',
                            coverMediaId === m.id ? 'border-sky-500' : 'border-slate-700 hover:border-slate-500',
                          )}
                        >
                          {/* Thumbnail hiển thị bằng presigned URL hiện tại — chỉ để chọn, publish sẽ copy bằng m.id */}
                          <img src={m.url!} alt="" className="w-full h-full object-cover" />
                        </button>
                      ))}
                    </div>
                  </div>
                )}

                {/* Fish + map preview — cùng component với public page */}
                <div className="mb-5">
                  {preview.snapshotData.fish.length > 0 ? (
                    <SnapshotFishSection fish={preview.snapshotData.fish} onNavigateFish={() => {}} />
                  ) : (
                    <div className="flex items-center gap-2 rounded-xl border border-amber-500/30 bg-amber-500/10 p-3 text-sm text-amber-400">
                      <Fish className="w-4 h-4 shrink-0" />
                      {t('publish.noFishWarning')}
                    </div>
                  )}
                </div>

                {/* Actions */}
                <div className="flex gap-3">
                  <button
                    onClick={onClose}
                    className="flex-1 py-3 rounded-xl border border-slate-700 text-slate-400 text-sm font-bold hover:bg-white/5 transition-colors min-h-[44px]"
                  >
                    {t('publish.cancel')}
                  </button>
                  <button
                    onClick={handlePublish}
                    disabled={publishing}
                    className="flex-1 py-3 rounded-xl bg-sky-500 text-white text-sm font-bold hover:bg-sky-400 transition-colors disabled:opacity-50 flex items-center justify-center gap-2 min-h-[44px]"
                  >
                    {publishing && <Loader2 className="w-4 h-4 animate-spin" />}
                    {t('publish.confirm')}
                  </button>
                </div>
              </>
            )}
          </div>
        </div>
      </div>
    </>
  );
}

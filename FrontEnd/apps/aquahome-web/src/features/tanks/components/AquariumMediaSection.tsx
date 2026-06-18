import { useEffect, useRef, useState } from 'react';
import {
  getAquariumMedia, requestMediaUpload, uploadToR2, confirmMediaUpload, deleteAquariumMedia,
} from '@fishlover/shared';
import type { AquariumMediaDto } from '@fishlover/shared';
import { ImagePlus, Trash2, Loader2, X, ZoomIn } from 'lucide-react';

const MAX_PHOTOS    = 10;
const MAX_BYTES     = 1 * 1024 * 1024; // 1 MB
const ALLOWED_TYPES = ['image/jpeg', 'image/png', 'image/webp'];

// ── Client-side compression ───────────────────────────────────────────────────
async function compressImage(file: File): Promise<Blob> {
  const bitmap = await createImageBitmap(file);
  const { width, height } = bitmap;

  // Scale down if wider than 1920px
  const scale    = width > 1920 ? 1920 / width : 1;
  const canvasW  = Math.round(width * scale);
  const canvasH  = Math.round(height * scale);

  const canvas   = document.createElement('canvas');
  canvas.width   = canvasW;
  canvas.height  = canvasH;
  const ctx      = canvas.getContext('2d')!;
  ctx.drawImage(bitmap, 0, 0, canvasW, canvasH);

  // Progressive quality reduction until ≤ 1 MB
  const mimeType = file.type === 'image/png' ? 'image/png' : file.type === 'image/webp' ? 'image/webp' : 'image/jpeg';
  if (mimeType === 'image/png') {
    // PNG is lossless — only resize helps; return as-is if already small
    return await new Promise<Blob>((res, rej) =>
      canvas.toBlob(b => b ? res(b) : rej(new Error('canvas.toBlob failed')), 'image/png'),
    );
  }

  for (const quality of [0.9, 0.8, 0.7, 0.6, 0.5, 0.4]) {
    const blob = await new Promise<Blob>((res, rej) =>
      canvas.toBlob(b => b ? res(b) : rej(new Error('canvas.toBlob failed')), mimeType, quality),
    );
    if (blob.size <= MAX_BYTES) return blob;
  }

  // Last resort: halve dimensions and retry at quality 0.6
  canvas.width  = Math.round(canvasW / 2);
  canvas.height = Math.round(canvasH / 2);
  ctx.drawImage(bitmap, 0, 0, canvas.width, canvas.height);
  return await new Promise<Blob>((res, rej) =>
    canvas.toBlob(b => b ? res(b) : rej(new Error('canvas.toBlob failed')), mimeType, 0.6),
  );
}

// ── Lightbox ──────────────────────────────────────────────────────────────────
function Lightbox({ url, onClose }: { url: string; onClose: () => void }) {
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => e.key === 'Escape' && onClose();
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [onClose]);

  return (
    <div
      className="fixed inset-0 z-50 bg-black/90 flex items-center justify-center"
      onClick={onClose}
    >
      <button
        className="absolute top-4 right-4 p-2 rounded-full bg-white/10 hover:bg-white/20 text-white"
        onClick={onClose}
      >
        <X className="w-5 h-5" />
      </button>
      <img
        src={url}
        alt=""
        className="max-w-[90vw] max-h-[90vh] object-contain rounded-xl"
        onClick={e => e.stopPropagation()}
      />
    </div>
  );
}

// ── Main component ────────────────────────────────────────────────────────────
interface Props { aquariumId: string }

interface UploadState {
  file: File;
  preview: string;
  progress: number;   // 0–100
  error: string | null;
}

export default function AquariumMediaSection({ aquariumId }: Props) {
  const [photos, setPhotos]         = useState<AquariumMediaDto[]>([]);
  const [loading, setLoading]       = useState(true);
  const [uploads, setUploads]       = useState<UploadState[]>([]);
  const [lightbox, setLightbox]     = useState<string | null>(null);
  const [deleting, setDeleting]     = useState<string | null>(null);
  const inputRef                    = useRef<HTMLInputElement>(null);

  useEffect(() => {
    getAquariumMedia(aquariumId)
      .then(setPhotos)
      .catch(console.error)
      .finally(() => setLoading(false));
  }, [aquariumId]);

  const canUpload = photos.length + uploads.length < MAX_PHOTOS;

  async function handleFiles(files: FileList) {
    const accepted = Array.from(files)
      .filter(f => ALLOWED_TYPES.includes(f.type))
      .slice(0, MAX_PHOTOS - photos.length - uploads.length);

    if (!accepted.length) return;

    const newUploads: UploadState[] = accepted.map(f => ({
      file: f,
      preview: URL.createObjectURL(f),
      progress: 0,
      error: null,
    }));
    setUploads(prev => [...prev, ...newUploads]);

    for (const upload of newUploads) {
      try {
        // 1. Compress
        const compressed = await compressImage(upload.file);
        setProgress(upload, 10);

        // 2. Request presigned PUT URL from BE
        const { uploadUrl, mediaId } = await requestMediaUpload(
          aquariumId, upload.file.name, upload.file.type,
        );
        setProgress(upload, 20);

        // 3. Upload compressed blob directly to R2
        await uploadToR2(uploadUrl, compressed, upload.file.type, pct => {
          setProgress(upload, 20 + Math.round(pct * 0.7)); // 20–90
        });

        // 4. Confirm with BE
        const dto = await confirmMediaUpload(aquariumId, mediaId);
        setProgress(upload, 100);

        setPhotos(prev => [...prev, dto]);
        setUploads(prev => prev.filter(u => u !== upload));
        URL.revokeObjectURL(upload.preview);
      } catch (err) {
        setUploads(prev =>
          prev.map(u => u === upload ? { ...u, error: (err as Error).message } : u),
        );
      }
    }
  }

  function setProgress(upload: UploadState, pct: number) {
    setUploads(prev => prev.map(u => u === upload ? { ...u, progress: pct } : u));
  }

  async function handleDelete(id: string) {
    setDeleting(id);
    try {
      await deleteAquariumMedia(aquariumId, id);
      setPhotos(prev => prev.filter(p => p.id !== id));
    } catch (err) {
      console.error('Delete failed', err);
    } finally {
      setDeleting(null);
    }
  }

  if (loading) {
    return (
      <div className="flex items-center justify-center py-10 text-slate-600">
        <Loader2 className="w-5 h-5 animate-spin" />
      </div>
    );
  }

  return (
    <div>
      <div className="flex items-center justify-between mb-4">
        <h3 className="text-sm font-bold text-slate-400 uppercase tracking-wider">
          Ảnh bể ({photos.length}/{MAX_PHOTOS})
        </h3>
        {canUpload && (
          <button
            onClick={() => inputRef.current?.click()}
            className="flex items-center gap-1.5 text-xs font-semibold px-3 py-1.5 rounded-lg bg-sky-500/10 hover:bg-sky-500/20 text-sky-400 transition-colors"
          >
            <ImagePlus className="w-3.5 h-3.5" />
            Thêm ảnh
          </button>
        )}
        <input
          ref={inputRef}
          type="file"
          accept={ALLOWED_TYPES.join(',')}
          multiple
          className="hidden"
          onChange={e => e.target.files && handleFiles(e.target.files)}
        />
      </div>

      {photos.length === 0 && uploads.length === 0 ? (
        <div
          className="flex flex-col items-center justify-center py-12 rounded-2xl border border-dashed border-slate-800/60 bg-[#1E293B]/30 cursor-pointer hover:bg-[#1E293B]/50 transition-colors"
          onClick={() => canUpload && inputRef.current?.click()}
        >
          <ImagePlus className="w-8 h-8 text-slate-600 mb-2" />
          <p className="text-slate-500 text-sm">Chưa có ảnh</p>
          <p className="text-slate-600 text-xs mt-1">JPG, PNG, WebP · tối đa 1 MB sau khi nén</p>
        </div>
      ) : (
        <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-5 gap-2">
          {/* Confirmed photos */}
          {photos.map(p => (
            <div key={p.id} className="relative group aspect-square rounded-xl overflow-hidden bg-slate-800">
              {p.url
                ? <img src={p.url} alt="" className="w-full h-full object-cover" />
                : <div className="w-full h-full bg-slate-700" />
              }
              <div className="absolute inset-0 bg-black/0 group-hover:bg-black/40 transition-colors flex items-center justify-center gap-2 opacity-0 group-hover:opacity-100">
                {p.url && (
                  <button
                    onClick={() => setLightbox(p.url!)}
                    className="p-1.5 rounded-full bg-white/20 hover:bg-white/30 text-white"
                  >
                    <ZoomIn className="w-3.5 h-3.5" />
                  </button>
                )}
                <button
                  onClick={() => handleDelete(p.id)}
                  disabled={deleting === p.id}
                  className="p-1.5 rounded-full bg-red-500/30 hover:bg-red-500/50 text-red-300 disabled:opacity-50"
                >
                  {deleting === p.id
                    ? <Loader2 className="w-3.5 h-3.5 animate-spin" />
                    : <Trash2 className="w-3.5 h-3.5" />
                  }
                </button>
              </div>
            </div>
          ))}

          {/* In-progress uploads */}
          {uploads.map((u, i) => (
            <div key={i} className="relative aspect-square rounded-xl overflow-hidden bg-slate-800">
              <img src={u.preview} alt="" className="w-full h-full object-cover opacity-40" />
              <div className="absolute inset-0 flex flex-col items-center justify-center gap-2 p-2">
                {u.error ? (
                  <>
                    <X className="w-5 h-5 text-red-400" />
                    <p className="text-[10px] text-red-400 text-center leading-tight">{u.error}</p>
                  </>
                ) : (
                  <>
                    <Loader2 className="w-5 h-5 text-sky-400 animate-spin" />
                    <div className="w-full bg-slate-700 rounded-full h-1">
                      <div
                        className="bg-sky-400 h-1 rounded-full transition-all duration-300"
                        style={{ width: `${u.progress}%` }}
                      />
                    </div>
                    <p className="text-[10px] text-sky-400">{u.progress}%</p>
                  </>
                )}
              </div>
            </div>
          ))}
        </div>
      )}

      {lightbox && <Lightbox url={lightbox} onClose={() => setLightbox(null)} />}
    </div>
  );
}

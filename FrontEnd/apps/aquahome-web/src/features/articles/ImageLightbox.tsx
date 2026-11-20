import { useCallback, useEffect, useRef, useState } from 'react';
import { X, ChevronLeft, ChevronRight, Play, Pause } from 'lucide-react';
import { useTranslation } from '@fishlover/shared';

export interface LightboxImage {
  url: string;
  caption?: string;
  alt?: string;
}

interface Props {
  images: LightboxImage[];
  /** Mở tại ảnh thứ mấy. null = đóng. */
  startIndex: number | null;
  onClose: () => void;
  /** Mở ra là chạy luôn — dùng cho nút "Xem slideshow". */
  autoPlayOnOpen?: boolean;
}

const SLIDE_MS = 4000;

/**
 * Xem ảnh toàn màn: phím ←/→ chuyển ảnh, Esc đóng, vuốt ngang trên điện thoại,
 * và nút tự chạy cho chế độ slideshow.
 *
 * Khóa cuộn trang nền khi mở — không thì vuốt trên mobile sẽ cuộn bài phía sau.
 */
export default function ImageLightbox({ images, startIndex, onClose, autoPlayOnOpen }: Props) {
  const { t } = useTranslation();
  const [index, setIndex] = useState(startIndex ?? 0);
  const [playing, setPlaying] = useState(false);
  const touchX = useRef<number | null>(null);

  const open = startIndex !== null && images.length > 0;

  useEffect(() => {
    if (startIndex === null) return;
    setIndex(startIndex);
    setPlaying(Boolean(autoPlayOnOpen));
  }, [startIndex, autoPlayOnOpen]);

  const go = useCallback(
    (delta: number) => setIndex((i) => (i + delta + images.length) % images.length),
    [images.length],
  );

  // Bàn phím: chỉ gắn khi đang mở, gỡ ngay khi đóng để không nuốt phím của trang bên dưới.
  useEffect(() => {
    if (!open) return;

    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose();
      else if (e.key === 'ArrowRight') { setPlaying(false); go(1); }
      else if (e.key === 'ArrowLeft') { setPlaying(false); go(-1); }
      else if (e.key === ' ') { e.preventDefault(); setPlaying((p) => !p); }
    };

    document.addEventListener('keydown', onKey);
    const prevOverflow = document.body.style.overflow;
    document.body.style.overflow = 'hidden';

    return () => {
      document.removeEventListener('keydown', onKey);
      document.body.style.overflow = prevOverflow;
    };
  }, [open, go, onClose]);

  useEffect(() => {
    if (!open || !playing || images.length < 2) return;
    const timer = setInterval(() => go(1), SLIDE_MS);
    return () => clearInterval(timer);
  }, [open, playing, go, images.length]);

  if (!open) return null;

  const current = images[index];

  return (
    <div
      className="fixed inset-0 z-[60] flex flex-col bg-black/95 backdrop-blur-sm"
      onTouchStart={(e) => { touchX.current = e.touches[0].clientX; }}
      onTouchEnd={(e) => {
        if (touchX.current === null) return;
        const dx = e.changedTouches[0].clientX - touchX.current;
        // 60px để không nhầm với chạm nhẹ khi bấm nút
        if (Math.abs(dx) > 60) { setPlaying(false); go(dx < 0 ? 1 : -1); }
        touchX.current = null;
      }}
    >
      {/* Thanh trên: đếm ảnh, tự chạy, đóng */}
      <div className="flex items-center gap-2 px-4 py-3 text-white/80">
        <span className="text-sm tabular-nums">{index + 1} / {images.length}</span>

        {images.length > 1 && (
          <button
            onClick={() => setPlaying((p) => !p)}
            className="ml-2 flex min-h-[40px] items-center gap-1.5 rounded-lg px-2.5 text-xs hover:bg-white/10"
            aria-label={playing ? t('articles.slideshowPause') : t('articles.slideshowPlay')}
          >
            {playing ? <Pause className="h-4 w-4" /> : <Play className="h-4 w-4" />}
            <span className="hidden sm:inline">
              {playing ? t('articles.slideshowPause') : t('articles.slideshowPlay')}
            </span>
          </button>
        )}

        <button
          onClick={onClose}
          className="ml-auto flex min-h-[44px] min-w-[44px] items-center justify-center rounded-lg hover:bg-white/10"
          aria-label={t('common.close')}
        >
          <X className="h-5 w-5" />
        </button>
      </div>

      {/* Ảnh */}
      <div className="relative flex min-h-0 flex-1 items-center justify-center px-2 sm:px-14">
        <img
          src={current.url}
          alt={current.alt ?? current.caption ?? ''}
          className="max-h-full max-w-full object-contain"
        />

        {images.length > 1 && (
          <>
            <button
              onClick={() => { setPlaying(false); go(-1); }}
              className="absolute left-1 flex h-12 w-12 items-center justify-center rounded-full bg-black/40 text-white hover:bg-black/70 sm:left-3"
              aria-label={t('articles.slidePrev')}
            >
              <ChevronLeft className="h-6 w-6" />
            </button>
            <button
              onClick={() => { setPlaying(false); go(1); }}
              className="absolute right-1 flex h-12 w-12 items-center justify-center rounded-full bg-black/40 text-white hover:bg-black/70 sm:right-3"
              aria-label={t('articles.slideNext')}
            >
              <ChevronRight className="h-6 w-6" />
            </button>
          </>
        )}
      </div>

      {/* Chú thích + dải thumbnail */}
      <div className="px-4 pb-4 pt-3">
        {current.caption && (
          <p className="mx-auto mb-3 max-w-2xl text-center text-sm italic text-white/70">{current.caption}</p>
        )}

        {images.length > 1 && (
          <div className="mx-auto flex max-w-full gap-2 overflow-x-auto pb-1">
            {images.map((img, i) => (
              <button
                key={i}
                onClick={() => { setPlaying(false); setIndex(i); }}
                className={`h-12 w-16 shrink-0 overflow-hidden rounded-md border-2 transition-opacity ${
                  i === index ? 'border-sky-400' : 'border-transparent opacity-50 hover:opacity-100'
                }`}
              >
                <img src={img.url} alt="" className="h-full w-full object-cover" />
              </button>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

import { useCallback, useEffect, useRef, useState } from 'react';
import { ChevronLeft, ChevronRight, Maximize2 } from 'lucide-react';
import { cn, useTranslation } from '@fishlover/shared';
import type { LightboxImage } from './ImageLightbox';

/**
 * Băng ảnh cho kiểu "Ảnh là chính", đặt đúng chỗ loạt ảnh trong thân bài.
 *
 * Kiểu này quyết định sẵn cách xem thay vì bày ra lựa chọn: xem từng tấm một, chú thích ngay dưới,
 * và toàn bộ ảnh của bài chỉ xuất hiện đúng một lần — ở đây.
 *
 * Khung cỡ bằng khu xem video (16:9, trong cột nội dung) chứ KHÔNG tràn hết màn. Tràn màn thì trên
 * màn hình rộng, mở bài ra là đập vào mắt một bức tường ảnh cao cả nghìn pixel, chưa kịp đọc chữ
 * nào. Muốn xem lớn thì bấm phóng to — lúc đó mới toàn màn hình, và đó là lúc người đọc chủ động.
 */
interface Props {
  images: LightboxImage[];
  /** Mở lightbox tại ảnh đang xem. */
  onExpand: (index: number) => void;
}

export default function ArticleCarousel({ images, onExpand }: Props) {
  const { t } = useTranslation();
  const [index, setIndex] = useState(0);
  const touchX = useRef<number | null>(null);

  const go = useCallback(
    (delta: number) => setIndex((i) => (i + delta + images.length) % images.length),
    [images.length],
  );

  // Phím mũi tên chỉ có tác dụng khi lightbox chưa mở — lightbox tự bắt phím của nó.
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      const typing = (e.target as HTMLElement)?.tagName;
      if (typing === 'INPUT' || typing === 'TEXTAREA') return;
      if (e.key === 'ArrowRight') go(1);
      else if (e.key === 'ArrowLeft') go(-1);
    };
    document.addEventListener('keydown', onKey);
    return () => document.removeEventListener('keydown', onKey);
  }, [go]);

  if (images.length === 0) return null;

  const current = images[index];

  return (
    <section className="mx-auto w-full max-w-4xl">
      <div
        className="relative aspect-[4/3] w-full overflow-hidden rounded-2xl border border-slate-800/80 bg-black sm:aspect-video"
        onTouchStart={(e) => { touchX.current = e.touches[0].clientX; }}
        onTouchEnd={(e) => {
          if (touchX.current === null) return;
          const dx = e.changedTouches[0].clientX - touchX.current;
          if (Math.abs(dx) > 60) go(dx < 0 ? 1 : -1);
          touchX.current = null;
        }}
      >
        <button
          type="button"
          onClick={() => onExpand(index)}
          className="group h-full w-full cursor-zoom-in"
          aria-label={t('articles.slideExpand')}
        >
          <img
            src={current.url}
            alt={current.alt ?? current.caption ?? ''}
            className="h-full w-full object-cover"
          />
          <span className="absolute right-3 top-3 rounded-lg bg-black/50 p-2 text-white opacity-0 transition-opacity group-hover:opacity-100">
            <Maximize2 className="h-4 w-4" />
          </span>
        </button>

        {images.length > 1 && (
          <>
            <button
              onClick={() => go(-1)}
              className="absolute left-2 top-1/2 flex h-11 w-11 -translate-y-1/2 items-center justify-center rounded-full bg-black/40 text-white backdrop-blur transition-colors hover:bg-black/70 sm:left-4"
              aria-label={t('articles.slidePrev')}
            >
              <ChevronLeft className="h-6 w-6" />
            </button>
            <button
              onClick={() => go(1)}
              className="absolute right-2 top-1/2 flex h-11 w-11 -translate-y-1/2 items-center justify-center rounded-full bg-black/40 text-white backdrop-blur transition-colors hover:bg-black/70 sm:right-4"
              aria-label={t('articles.slideNext')}
            >
              <ChevronRight className="h-6 w-6" />
            </button>

            {/* Chấm chỉ vị trí: dưới 10 ảnh thì chấm đọc nhanh hơn số */}
            <div className="absolute inset-x-0 bottom-3 flex items-center justify-center gap-1.5">
              {images.map((_, i) => (
                <button
                  key={i}
                  onClick={() => setIndex(i)}
                  aria-label={`${i + 1}`}
                  className={cn(
                    'h-1.5 rounded-full transition-all',
                    i === index ? 'w-6 bg-white' : 'w-1.5 bg-white/40 hover:bg-white/70',
                  )}
                />
              ))}
            </div>
          </>
        )}
      </div>

      <div className="mt-3 flex items-start gap-3">
        <span className="shrink-0 rounded-md bg-slate-800 px-2 py-0.5 text-xs tabular-nums text-slate-400">
          {index + 1}/{images.length}
        </span>
        {current.caption && (
          <p className="text-[15px] leading-relaxed text-slate-300">{current.caption}</p>
        )}
      </div>
    </section>
  );
}

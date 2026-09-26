import { useState } from 'react';
import { Play } from 'lucide-react';
import { cn, useTranslation } from '@fishlover/shared';

/**
 * Nhúng video YouTube kiểu "bấm mới tải".
 *
 * Lúc đầu chỉ là ảnh thumbnail + nút play — một thẻ img vài chục KB. Chỉ khi người đọc bấm mới
 * dựng iframe. Nhúng thẳng iframe từ đầu thì mỗi video kéo theo vài trăm KB script và cookie
 * tracking của YouTube, ba video trong một bài là trang nặng gấp đôi mà phần lớn người đọc
 * không bấm cái nào.
 *
 * Dùng domain youtube-nocookie: YouTube không đặt cookie theo dõi cho tới khi video thật sự chạy.
 */
interface Props {
  /** Id 11 ký tự — BE đã bóc sẵn từ link admin dán vào. */
  youtubeId: string;
  caption?: string;
  /** Tràn rộng hơn cột chữ (kiểu Tạp chí, Ảnh là chính). */
  wide?: boolean;
}

export default function YoutubeEmbed({ youtubeId, caption, wide }: Props) {
  const { t } = useTranslation();
  const [playing, setPlaying] = useState(false);

  return (
    <figure className={cn('my-6', wide ? 'mx-auto w-full max-w-4xl' : 'w-full')}>
      <div className="relative aspect-video w-full overflow-hidden rounded-2xl border border-slate-800/80 bg-black">
        {playing ? (
          <iframe
            src={`https://www.youtube-nocookie.com/embed/${youtubeId}?autoplay=1&rel=0`}
            title={caption ?? 'YouTube'}
            allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
            allowFullScreen
            className="absolute inset-0 h-full w-full"
          />
        ) : (
          <button
            type="button"
            onClick={() => setPlaying(true)}
            className="group absolute inset-0 h-full w-full"
            aria-label={t('articles.videoPlay')}
          >
            {/* hqdefault có ở mọi video; maxres thì nhiều video cũ không có, sẽ ra ảnh vỡ */}
            <img
              src={`https://i.ytimg.com/vi/${youtubeId}/hqdefault.jpg`}
              alt=""
              loading="lazy"
              className="h-full w-full object-cover transition-transform duration-300 group-hover:scale-105"
            />
            <span className="absolute inset-0 bg-black/25 transition-colors group-hover:bg-black/10" />
            <span className="absolute left-1/2 top-1/2 flex h-16 w-16 -translate-x-1/2 -translate-y-1/2 items-center justify-center rounded-full bg-red-600 shadow-lg transition-transform group-hover:scale-110">
              <Play className="ml-1 h-7 w-7 fill-white text-white" />
            </span>
          </button>
        )}
      </div>

      {caption && (
        <figcaption className="mx-auto mt-3 text-center text-[13px] italic text-slate-500">
          {caption}
        </figcaption>
      )}
    </figure>
  );
}

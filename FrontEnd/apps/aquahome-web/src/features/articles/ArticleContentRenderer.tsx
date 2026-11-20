import { useEffect, useState } from 'react';
import { Lightbulb, ImageOff, Quote as QuoteIcon, Maximize2, Check } from 'lucide-react';
import { cn, useTranslation } from '@fishlover/shared';
import type { ArticleAssetDto, ArticleBlock, ArticleContent } from '@fishlover/shared';
import { templateSpec, headingAnchor, PROSE_COLUMN, type ArticleTemplateSpec } from './templates';
import YoutubeEmbed from './YoutubeEmbed';

/**
 * Render content.json thành bài đọc.
 *
 * Cột chữ dựng bằng grid 3 cột `1fr [cột chữ] 1fr`: mọi block nằm ở cột giữa, ảnh hoặc pull-quote
 * cần rộng hơn thì `col-span-3` phá ra hai bên. Nhờ vậy chỉ có MỘT chiều rộng cột chữ cho cả bài —
 * KHÔNG dùng `max-w-[66ch]` trên từng element, vì `ch` tính theo cỡ chữ của chính element đó nên
 * h2 26px sẽ ra cột rộng hơn đoạn văn 18px và chữ lệch cột.
 *
 * Cỡ chữ đi qua biến `--prose-size` để nút A−/A+ của người đọc nhân vào được.
 *
 * Mọi text đều đi qua text node của React, KHÔNG dùng dangerouslySetInnerHTML — đó chính là
 * lý do nội dung lưu dạng block thay vì HTML: không có đường nào cho script chui vào bài.
 */
interface Props {
  content: ArticleContent;
  assets: ArticleAssetDto[];
  /** Ghi đè template của bài — trình soạn dùng để xem thử kiểu khác trước khi lưu. */
  templateKey?: string;
  /** Bấm vào ảnh thứ n trong bài (thứ tự tính theo toàn bài) — mở lightbox. */
  onImageClick?: (galleryIndex: number) => void;
  /** Khóa lưu trạng thái tick checklist. Thiếu thì checklist không nhớ được, chỉ tick tạm. */
  storageKey?: string;
  /** Băng ảnh cho kiểu Ảnh là chính — vẽ vào đúng chỗ block ảnh đầu tiên, thay cho cả loạt. */
  gallery?: React.ReactNode;
}

const BODY = 'text-[length:calc(var(--prose-size)*var(--prose-scale))] leading-[1.75] text-slate-200';
const COL = 'col-start-2';

/** Tick của checklist nhớ theo bài: người đọc chạy bể mất 2–4 tuần, đóng tab là chuyện thường. */
function useChecklist(storageKey?: string) {
  const [checked, setChecked] = useState<Set<string>>(new Set());

  useEffect(() => {
    if (!storageKey) return;
    try {
      const raw = localStorage.getItem(storageKey);
      if (raw) setChecked(new Set(JSON.parse(raw) as string[]));
    } catch {
      // localStorage bị chặn (chế độ riêng tư) — tick vẫn dùng được, chỉ không nhớ qua phiên
    }
  }, [storageKey]);

  const toggle = (id: string) => {
    setChecked((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id); else next.add(id);
      if (storageKey) {
        try { localStorage.setItem(storageKey, JSON.stringify([...next])); } catch { /* bỏ qua */ }
      }
      return next;
    });
  };

  const reset = () => {
    setChecked(new Set());
    if (storageKey) {
      try { localStorage.removeItem(storageKey); } catch { /* bỏ qua */ }
    }
  };

  return { checked, toggle, reset };
}

function Block({
  block, index, section, assetUrl, spec, headingNumber, galleryIndex, onImageClick, checklist, gallery,
}: {
  block: ArticleBlock;
  index: number;
  section: string;
  assetUrl: (id?: string) => string | null;
  spec: ArticleTemplateSpec;
  headingNumber?: number;
  galleryIndex?: number;
  onImageClick?: (i: number) => void;
  checklist: ReturnType<typeof useChecklist>;
  gallery?: React.ReactNode;
}) {
  switch (block.type) {
    case 'paragraph':
      return <p className={cn(COL, BODY)}>{block.text}</p>;

    case 'heading': {
      if (block.level === 3) {
        return (
          <h3 className={cn(COL, 'mt-6 text-[19px] font-bold leading-snug text-white sm:text-xl')}>
            {block.text}
          </h3>
        );
      }
      return (
        <h2
          id={headingAnchor(index)}
          className={cn(COL, 'mt-10 scroll-mt-24 text-[22px] font-bold leading-snug text-white sm:text-[26px]')}
        >
          {spec.numberedHeadings && headingNumber !== undefined && (
            <span className="mr-2.5 text-sky-400">{headingNumber}.</span>
          )}
          {block.text}
        </h2>
      );
    }

    case 'image': {
      // Kiểu Ảnh là chính: cả loạt ảnh gom vào một băng ảnh, đặt ngay chỗ tấm đầu tiên.
      // Các block ảnh còn lại không vẽ nữa — vẽ tiếp là lặp lại đúng những tấm đã có trong băng.
      if (spec.gallery) {
        return galleryIndex === 0 ? <div className="col-span-3 my-6">{gallery}</div> : null;
      }

      const url = assetUrl(block.assetId);

      // Ảnh bị admin xóa nhưng block còn sót: hiện ô trống thay vì làm vỡ cả bài.
      if (!url) {
        return (
          <div className={cn(COL, 'flex items-center gap-2 rounded-xl border border-slate-800 bg-slate-900/40 px-4 py-6 text-xs text-slate-600')}>
            <ImageOff className="h-4 w-4" /> {block.caption ?? block.alt ?? ''}
          </div>
        );
      }

      const wide = spec.imageWidth === 'wide';
      const clickable = onImageClick && galleryIndex !== undefined;

      return (
        <figure className={cn(wide ? 'col-span-3' : COL, 'my-6')}>
          <button
            type="button"
            onClick={() => clickable && onImageClick!(galleryIndex!)}
            className={cn('group relative block w-full', clickable ? 'cursor-zoom-in' : 'cursor-default')}
          >
            <img
              src={url}
              alt={block.alt ?? block.caption ?? ''}
              loading="lazy"
              // Chặn chiều cao: ảnh dọc để nguyên tỉ lệ sẽ cao hơn cả màn hình, đẩy chữ ra khỏi tầm nhìn.
              className={cn(
                'mx-auto max-h-[70vh] w-full rounded-2xl border border-slate-800/80 object-contain',
                wide && 'max-w-4xl',
              )}
            />
            {clickable && (
              <span className="absolute right-3 top-3 rounded-lg bg-black/50 p-1.5 text-white opacity-0 transition-opacity group-hover:opacity-100">
                <Maximize2 className="h-4 w-4" />
              </span>
            )}
          </button>
          {block.caption && (
            <figcaption className={cn(
              'mx-auto mt-3 text-center italic text-slate-500',
              wide ? 'max-w-2xl text-sm' : 'text-[13px]',
            )}>
              {block.caption}
            </figcaption>
          )}
        </figure>
      );
    }

    case 'video': {
      if (!block.youtubeId) return null;
      const wide = spec.imageWidth === 'wide';
      return (
        <div className={cn(wide ? 'col-span-3' : COL)}>
          <YoutubeEmbed youtubeId={block.youtubeId} caption={block.caption} wide={wide} />
        </div>
      );
    }

    case 'list': {
      const items = block.items ?? [];

      // Bài hướng dẫn: list đánh số là các bước phải làm → cho tick, và nhớ lại lần sau.
      if (spec.checklist && block.ordered) {
        return (
          <ul className={cn(COL, BODY, 'space-y-2')}>
            {items.map((it, i) => {
              const id = `${section}-${index}-${i}`;
              const done = checklist.checked.has(id);
              return (
                <li key={i}>
                  <button
                    type="button"
                    onClick={() => checklist.toggle(id)}
                    className="flex w-full items-start gap-3 rounded-xl border border-slate-800 bg-slate-900/40 p-3 text-left transition-colors hover:border-slate-700"
                  >
                    <span className={cn(
                      'mt-0.5 flex h-5 w-5 shrink-0 items-center justify-center rounded-md border',
                      done ? 'border-emerald-500 bg-emerald-500 text-slate-900' : 'border-slate-600',
                    )}>
                      {done && <Check className="h-3.5 w-3.5" strokeWidth={3} />}
                    </span>
                    <span className={cn('leading-[1.6]', done && 'text-slate-500 line-through')}>{it}</span>
                  </button>
                </li>
              );
            })}
          </ul>
        );
      }

      const cls = cn(COL, BODY, 'space-y-2.5 pl-6');
      return block.ordered
        ? (
          <ol className={cn(cls, 'list-decimal marker:font-semibold marker:text-sky-400')}>
            {items.map((it, i) => <li key={i} className="pl-1">{it}</li>)}
          </ol>
        )
        : (
          <ul className={cn(cls, 'list-disc marker:text-sky-500/70')}>
            {items.map((it, i) => <li key={i} className="pl-1">{it}</li>)}
          </ul>
        );
    }

    case 'quote':
      // Tạp chí: quote tràn rộng thành pull-quote, chữ lớn, canh giữa như báo giấy
      if (spec.pullQuote) {
        return (
          <blockquote className="col-span-3 my-10 px-4 text-center">
            <QuoteIcon className="mx-auto mb-3 h-7 w-7 text-sky-500/50" />
            <p className="mx-auto max-w-3xl text-2xl font-light leading-[1.4] text-white sm:text-[32px]">
              {block.text}
            </p>
            {block.cite && (
              <cite className="mt-4 block text-sm not-italic text-slate-500">— {block.cite}</cite>
            )}
          </blockquote>
        );
      }
      return (
        <blockquote className={cn(COL, 'my-6 border-l-[3px] border-sky-500/60 pl-5')}>
          <QuoteIcon className="mb-2 h-5 w-5 text-sky-500/50" />
          <p className="text-xl font-medium leading-[1.6] text-slate-100 sm:text-[22px]">{block.text}</p>
          {block.cite && (
            <cite className="mt-3 block text-sm not-italic text-slate-500">— {block.cite}</cite>
          )}
        </blockquote>
      );

    case 'tip':
      return (
        <div className={cn(COL, 'my-4 flex gap-3.5 rounded-2xl border border-amber-500/25 bg-amber-500/[0.07] p-5')}>
          <Lightbulb className="mt-0.5 h-5 w-5 shrink-0 text-amber-400" />
          <p className="text-[16px] leading-[1.7] text-amber-50/90">{block.text}</p>
        </div>
      );

    default:
      // Block lạ (bài cũ, schema mới hơn FE) — bỏ qua chứ không dựng gì.
      return null;
  }
}

/** Gom ảnh của cả bài theo đúng thứ tự xuất hiện — lightbox và dải thumbnail dùng chung danh sách này. */
export function collectImages(content: ArticleContent, assets: ArticleAssetDto[]) {
  const urlById = new Map(assets.map((a) => [a.id, a.url]));
  const out: { url: string; caption?: string; alt?: string; assetId: string }[] = [];

  for (const section of [content.intro, content.body, content.conclusion]) {
    for (const block of section ?? []) {
      if (block.type !== 'image' || !block.assetId) continue;
      const url = urlById.get(block.assetId);
      if (url) out.push({ url, caption: block.caption, alt: block.alt, assetId: block.assetId });
    }
  }
  return out;
}

export default function ArticleContentRenderer({
  content, assets, templateKey, onImageClick, storageKey, gallery,
}: Props) {
  const spec = templateSpec(templateKey ?? content.template);
  const urlById = new Map(assets.map((a) => [a.id, a.url]));
  const assetUrl = (id?: string) => (id ? urlById.get(id) ?? null : null);
  const checklist = useChecklist(storageKey);
  const { t } = useTranslation();

  const intro = content.intro ?? [];
  const body = content.body ?? [];
  const conclusion = content.conclusion ?? [];

  // Đoạn dẫn: chỉ đoạn văn ĐẦU TIÊN của mở bài được phóng to. Phóng cả mở bài thì mất tác dụng
  // nhấn, mà bỏ hẳn thì vào bài không có nhịp mở.
  const leadBlock = spec.lead && intro[0]?.type === 'paragraph' ? intro[0] : null;
  const restIntro = leadBlock ? intro.slice(1) : intro;

  let headingCount = 0;
  let imageCount = 0;

  const renderBlocks = (blocks: ArticleBlock[], section: string, numbered: boolean) =>
    blocks.map((block, i) => {
      const isH2 = block.type === 'heading' && block.level !== 3;
      if (isH2 && numbered) headingCount += 1;

      const isImage = block.type === 'image' && Boolean(assetUrl(block.assetId));
      const galleryIndex = isImage ? imageCount++ : undefined;

      return (
        <Block
          key={`${section}-${i}`}
          block={block}
          index={i}
          section={section}
          assetUrl={assetUrl}
          spec={spec}
          headingNumber={isH2 && numbered ? headingCount : undefined}
          galleryIndex={galleryIndex}
          onImageClick={onImageClick}
          checklist={checklist}
          gallery={gallery}
        />
      );
    });

  // Bài hướng dẫn: đếm tổng số bước để hiện tiến độ. Tick từng ô mà không biết còn bao nhiêu
  // thì người đọc không ước lượng được mình đang ở đâu trong quy trình vài tuần.
  const checklistIds = spec.checklist
    ? [
        ...body.flatMap((b, i) => (b.type === 'list' && b.ordered ? (b.items ?? []).map((_, j) => `body-${i}-${j}`) : [])),
        ...intro.flatMap((b, i) => (b.type === 'list' && b.ordered ? (b.items ?? []).map((_, j) => `intro-${i}-${j}`) : [])),
        ...conclusion.flatMap((b, i) => (b.type === 'list' && b.ordered ? (b.items ?? []).map((_, j) => `end-${i}-${j}`) : [])),
      ]
    : [];
  const doneCount = checklistIds.filter((id) => checklist.checked.has(id)).length;

  return (
    <article
      className={cn(
        'grid gap-y-5 [--prose-size:17px] sm:[--prose-size:18px]',
        PROSE_COLUMN,
      )}
    >
      {checklistIds.length > 0 && (
        <div className={cn(COL, 'mb-2 flex items-center gap-3 rounded-xl border border-slate-800 bg-slate-900/40 px-4 py-3')}>
          <div className="h-1.5 flex-1 overflow-hidden rounded-full bg-slate-800">
            <div
              className="h-full rounded-full bg-emerald-500 transition-[width] duration-300"
              style={{ width: `${(doneCount / checklistIds.length) * 100}%` }}
            />
          </div>
          <span className="shrink-0 text-xs tabular-nums text-slate-400">
            {doneCount}/{checklistIds.length}
          </span>
          {doneCount > 0 && (
            <button
              type="button"
              onClick={checklist.reset}
              className="shrink-0 text-xs text-slate-500 underline-offset-2 hover:text-white hover:underline"
            >
              {t('articles.checklistReset')}
            </button>
          )}
        </div>
      )}
      {leadBlock && (
        <p className={cn(
          COL,
          'text-[length:calc(var(--prose-size)*var(--prose-scale)*1.25)] font-light leading-[1.55] text-slate-100',
          // Chữ cái đầu thả to. Dùng leading rất chặt + padding trên để dấu tiếng Việt (Ấ, Ồ)
          // không bị cắt mất phần trên.
          spec.dropCap && 'first-letter:float-left first-letter:mr-3 first-letter:pt-1 first-letter:text-[64px] first-letter:font-bold first-letter:leading-[0.8] first-letter:text-sky-400',
        )}>
          {leadBlock.text}
        </p>
      )}

      {renderBlocks(restIntro, 'intro', false)}
      {renderBlocks(body, 'body', spec.numberedHeadings)}

      {conclusion.length > 0 && (
        <>
          {/* Vạch ngắn giữa trang: dấu hết phần thân, nhẹ hơn một đường kẻ suốt chiều rộng */}
          <div className={cn(COL, 'mx-auto my-8 h-px w-16 bg-slate-700')} />
          {renderBlocks(conclusion, 'end', false)}
        </>
      )}
    </article>
  );
}

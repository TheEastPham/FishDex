import { Lightbulb, ImageOff } from 'lucide-react';
import type { ArticleAssetDto, ArticleBlock, ArticleContent } from '@fishlover/shared';

/**
 * Render content.json theo template. Phase này chỉ có template "standard":
 * mở bài → thân bài → kết bài, cùng một cách trình bày.
 *
 * Mọi text đều đi qua text node của React, KHÔNG dùng dangerouslySetInnerHTML — đó chính là
 * lý do nội dung lưu dạng block thay vì HTML: không có đường nào cho script chui vào bài.
 */
interface Props {
  content: ArticleContent;
  assets: ArticleAssetDto[];
}

function Block({ block, assetUrl }: { block: ArticleBlock; assetUrl: (id?: string) => string | null }) {
  switch (block.type) {
    case 'paragraph':
      return <p className="text-[15px] leading-7 text-slate-300">{block.text}</p>;

    case 'heading':
      return block.level === 3
        ? <h3 className="text-base font-bold text-white mt-6 mb-1">{block.text}</h3>
        : <h2 className="text-lg font-bold text-white mt-8 mb-1">{block.text}</h2>;

    case 'image': {
      const url = assetUrl(block.assetId);
      // Ảnh bị admin xóa nhưng block còn sót: hiện ô trống thay vì làm vỡ cả bài.
      if (!url) {
        return (
          <div className="flex items-center gap-2 rounded-xl border border-slate-800 bg-slate-900/40 px-4 py-6 text-xs text-slate-600">
            <ImageOff className="w-4 h-4" /> {block.caption ?? block.alt ?? ''}
          </div>
        );
      }
      return (
        <figure className="my-2">
          <img
            src={url}
            alt={block.alt ?? block.caption ?? ''}
            loading="lazy"
            // Chặn chiều cao: ảnh dọc (hoặc ảnh vuông trên desktop) để nguyên tỉ lệ sẽ cao hơn
            // cả màn hình, đẩy phần chữ còn lại xuống dưới tầm nhìn.
            className="max-h-[70vh] w-full rounded-xl border border-slate-800 object-contain"
          />
          {block.caption && (
            <figcaption className="mt-2 text-center text-xs text-slate-500">{block.caption}</figcaption>
          )}
        </figure>
      );
    }

    case 'list': {
      const items = block.items ?? [];
      const cls = 'space-y-1.5 pl-5 text-[15px] leading-7 text-slate-300';
      return block.ordered
        ? <ol className={`${cls} list-decimal`}>{items.map((it, i) => <li key={i}>{it}</li>)}</ol>
        : <ul className={`${cls} list-disc`}>{items.map((it, i) => <li key={i}>{it}</li>)}</ul>;
    }

    case 'quote':
      return (
        <blockquote className="border-l-2 border-sky-500/50 pl-4 py-1 text-[15px] leading-7 italic text-slate-400">
          {block.text}
          {block.cite && <cite className="mt-1 block text-xs not-italic text-slate-500">— {block.cite}</cite>}
        </blockquote>
      );

    case 'tip':
      return (
        <div className="flex gap-3 rounded-xl border border-amber-500/20 bg-amber-500/5 p-4">
          <Lightbulb className="mt-0.5 h-4 w-4 shrink-0 text-amber-400" />
          <p className="text-sm leading-6 text-amber-100/90">{block.text}</p>
        </div>
      );

    default:
      // Block lạ (bài cũ, schema mới hơn FE) — bỏ qua chứ không dựng gì.
      return null;
  }
}

function Section({ blocks, assetUrl }: { blocks: ArticleBlock[]; assetUrl: (id?: string) => string | null }) {
  if (blocks.length === 0) return null;
  return (
    <div className="space-y-4">
      {blocks.map((block, i) => <Block key={i} block={block} assetUrl={assetUrl} />)}
    </div>
  );
}

export default function ArticleContentRenderer({ content, assets }: Props) {
  const urlById = new Map(assets.map((a) => [a.id, a.url]));
  const assetUrl = (id?: string) => (id ? urlById.get(id) ?? null : null);

  const intro = content.intro ?? [];
  const body = content.body ?? [];
  const conclusion = content.conclusion ?? [];

  return (
    <article className="space-y-6">
      {/* Mở bài — chữ to hơn thân bài một nhịp để vào bài có điểm nhấn */}
      {intro.length > 0 && (
        <div className="space-y-4 text-[16px] leading-7 text-slate-200">
          <Section blocks={intro} assetUrl={assetUrl} />
        </div>
      )}

      <Section blocks={body} assetUrl={assetUrl} />

      {conclusion.length > 0 && (
        <div className="mt-8 border-t border-slate-800 pt-6">
          <Section blocks={conclusion} assetUrl={assetUrl} />
        </div>
      )}
    </article>
  );
}

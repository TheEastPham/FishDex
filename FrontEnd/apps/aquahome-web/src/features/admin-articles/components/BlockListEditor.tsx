import { useState } from 'react';
import { useTranslation } from '@fishlover/shared';
import type { ArticleAssetDto, ArticleBlock, ArticleBlockType } from '@fishlover/shared';
import {
  ChevronUp, ChevronDown, Trash2, Type, Heading, Image as ImageIcon, List, Quote, Lightbulb, ClipboardPaste,
} from 'lucide-react';

/**
 * Sửa danh sách block của một mục (mở bài / thân bài / kết bài).
 *
 * Người viết không cần biết HTML: chọn loại block, gõ vào ô, kéo lên xuống bằng nút mũi tên.
 * Ô "dán cả bài" tách theo dòng trống thành từng đoạn — người quen soạn trong Word chỉ cần
 * copy toàn bộ, dán một lần, rồi chèn ảnh vào giữa.
 */
interface Props {
  label: string;
  hint?: string;
  blocks: ArticleBlock[];
  assets: ArticleAssetDto[];
  onChange: (blocks: ArticleBlock[]) => void;
  /** Chỉ mục thân bài mới cần ô dán hàng loạt — mở bài/kết bài thường vài dòng. */
  allowBulkPaste?: boolean;
}

const NEW_BLOCK: Record<ArticleBlockType, ArticleBlock> = {
  paragraph: { type: 'paragraph', text: '' },
  heading:   { type: 'heading', text: '', level: 2 },
  image:     { type: 'image', assetId: undefined, caption: '', alt: '' },
  list:      { type: 'list', ordered: false, items: [''] },
  quote:     { type: 'quote', text: '', cite: '' },
  tip:       { type: 'tip', text: '' },
};

const ADD_BUTTONS: { type: ArticleBlockType; icon: typeof Type; labelKey: string }[] = [
  { type: 'paragraph', icon: Type,       labelKey: 'adminArticles.blockParagraph' },
  { type: 'heading',   icon: Heading,    labelKey: 'adminArticles.blockHeading' },
  { type: 'image',     icon: ImageIcon,  labelKey: 'adminArticles.blockImage' },
  { type: 'list',      icon: List,       labelKey: 'adminArticles.blockList' },
  { type: 'quote',     icon: Quote,      labelKey: 'adminArticles.blockQuote' },
  { type: 'tip',       icon: Lightbulb,  labelKey: 'adminArticles.blockTip' },
];

const inputCls =
  'w-full rounded-lg border border-slate-700 bg-[#141518] px-3 py-2 text-base text-slate-200 placeholder:text-slate-600 focus:border-sky-500/50 focus:outline-none sm:text-sm';

export default function BlockListEditor({ label, hint, blocks, assets, onChange, allowBulkPaste }: Props) {
  const { t } = useTranslation();
  const [bulk, setBulk] = useState('');

  const update = (index: number, patch: Partial<ArticleBlock>) =>
    onChange(blocks.map((b, i) => (i === index ? { ...b, ...patch } : b)));

  const remove = (index: number) => onChange(blocks.filter((_, i) => i !== index));

  const move = (index: number, delta: number) => {
    const target = index + delta;
    if (target < 0 || target >= blocks.length) return;
    const next = [...blocks];
    [next[index], next[target]] = [next[target], next[index]];
    onChange(next);
  };

  const add = (type: ArticleBlockType) => onChange([...blocks, { ...NEW_BLOCK[type] }]);

  const applyBulk = () => {
    // Tách theo dòng trống: đúng thói quen xuống dòng đôi giữa các đoạn của Word/Google Docs.
    const paragraphs = bulk
      .split(/\n\s*\n/)
      .map((p) => p.trim().replace(/\s*\n\s*/g, ' '))
      .filter(Boolean)
      .map<ArticleBlock>((text) => ({ type: 'paragraph', text }));

    if (paragraphs.length === 0) return;
    onChange([...blocks, ...paragraphs]);
    setBulk('');
  };

  return (
    <section className="rounded-xl border border-slate-800 bg-[#202226] p-4">
      <div className="mb-3">
        <h3 className="text-sm font-semibold text-white">{label}</h3>
        {hint && <p className="mt-0.5 text-xs text-slate-500">{hint}</p>}
      </div>

      <div className="space-y-3">
        {blocks.map((block, i) => (
          <div key={i} className="rounded-lg border border-slate-700/60 bg-[#1A1B1F] p-3">
            <div className="mb-2 flex items-center gap-2">
              <span className="text-[11px] font-semibold uppercase tracking-wider text-slate-500">
                {t(`adminArticles.block${block.type.charAt(0).toUpperCase()}${block.type.slice(1)}`)}
              </span>
              <div className="ml-auto flex items-center gap-1">
                <button type="button" onClick={() => move(i, -1)} disabled={i === 0}
                  className="rounded p-1.5 text-slate-500 hover:text-white disabled:opacity-30">
                  <ChevronUp className="h-4 w-4" />
                </button>
                <button type="button" onClick={() => move(i, 1)} disabled={i === blocks.length - 1}
                  className="rounded p-1.5 text-slate-500 hover:text-white disabled:opacity-30">
                  <ChevronDown className="h-4 w-4" />
                </button>
                <button type="button" onClick={() => remove(i)}
                  className="rounded p-1.5 text-slate-500 hover:text-red-400">
                  <Trash2 className="h-4 w-4" />
                </button>
              </div>
            </div>

            {(block.type === 'paragraph' || block.type === 'tip') && (
              <textarea
                value={block.text ?? ''}
                onChange={(e) => update(i, { text: e.target.value })}
                rows={block.type === 'tip' ? 2 : 4}
                placeholder={t('adminArticles.textPlaceholder')}
                className={inputCls}
              />
            )}

            {block.type === 'heading' && (
              <div className="flex gap-2">
                <select
                  value={block.level ?? 2}
                  onChange={(e) => update(i, { level: Number(e.target.value) })}
                  className="rounded-lg border border-slate-700 bg-[#141518] px-2 py-2 text-sm text-slate-200"
                >
                  <option value={2}>H2</option>
                  <option value={3}>H3</option>
                </select>
                <input
                  value={block.text ?? ''}
                  onChange={(e) => update(i, { text: e.target.value })}
                  placeholder={t('adminArticles.headingPlaceholder')}
                  className={inputCls}
                />
              </div>
            )}

            {block.type === 'quote' && (
              <div className="space-y-2">
                <textarea
                  value={block.text ?? ''}
                  onChange={(e) => update(i, { text: e.target.value })}
                  rows={2}
                  className={inputCls}
                />
                <input
                  value={block.cite ?? ''}
                  onChange={(e) => update(i, { cite: e.target.value })}
                  placeholder={t('adminArticles.citePlaceholder')}
                  className={inputCls}
                />
              </div>
            )}

            {block.type === 'list' && (
              <div className="space-y-2">
                <label className="flex items-center gap-2 text-xs text-slate-400">
                  <input
                    type="checkbox"
                    checked={block.ordered ?? false}
                    onChange={(e) => update(i, { ordered: e.target.checked })}
                    className="h-4 w-4 accent-sky-500"
                  />
                  {t('adminArticles.orderedList')}
                </label>
                <textarea
                  value={(block.items ?? []).join('\n')}
                  onChange={(e) => update(i, { items: e.target.value.split('\n') })}
                  rows={4}
                  placeholder={t('adminArticles.listPlaceholder')}
                  className={inputCls}
                />
              </div>
            )}

            {block.type === 'image' && (
              <div className="space-y-2">
                {assets.length === 0 ? (
                  <p className="text-xs text-amber-400/80">{t('adminArticles.noAssetsHint')}</p>
                ) : (
                  <div className="flex gap-2">
                    <select
                      value={block.assetId ?? ''}
                      onChange={(e) => update(i, { assetId: e.target.value || undefined })}
                      className="flex-1 rounded-lg border border-slate-700 bg-[#141518] px-2 py-2 text-sm text-slate-200"
                    >
                      <option value="">{t('adminArticles.pickImage')}</option>
                      {assets.map((a) => (
                        <option key={a.id} value={a.id}>{a.fileName ?? a.id.slice(0, 8)}</option>
                      ))}
                    </select>
                    {block.assetId && assets.find((a) => a.id === block.assetId)?.url && (
                      <img
                        src={assets.find((a) => a.id === block.assetId)!.url!}
                        alt=""
                        className="h-10 w-10 rounded border border-slate-700 object-cover"
                      />
                    )}
                  </div>
                )}
                <input
                  value={block.caption ?? ''}
                  onChange={(e) => update(i, { caption: e.target.value })}
                  placeholder={t('adminArticles.captionPlaceholder')}
                  className={inputCls}
                />
                <input
                  value={block.alt ?? ''}
                  onChange={(e) => update(i, { alt: e.target.value })}
                  placeholder={t('adminArticles.altPlaceholder')}
                  className={inputCls}
                />
              </div>
            )}
          </div>
        ))}
      </div>

      {/* Thêm block */}
      <div className="mt-3 flex flex-wrap gap-1.5">
        {ADD_BUTTONS.map(({ type, icon: Icon, labelKey }) => (
          <button
            key={type}
            type="button"
            onClick={() => add(type)}
            className="flex items-center gap-1.5 rounded-lg border border-slate-700 bg-[#1A1B1F] px-2.5 py-1.5 text-xs text-slate-300 transition-colors hover:border-sky-500/40 hover:text-white"
          >
            <Icon className="h-3.5 w-3.5" /> {t(labelKey)}
          </button>
        ))}
      </div>

      {allowBulkPaste && (
        <div className="mt-4 rounded-lg border border-dashed border-slate-700 p-3">
          <label className="mb-1.5 flex items-center gap-1.5 text-xs font-semibold text-slate-400">
            <ClipboardPaste className="h-3.5 w-3.5" /> {t('adminArticles.bulkPasteTitle')}
          </label>
          <p className="mb-2 text-[11px] text-slate-500">{t('adminArticles.bulkPasteHint')}</p>
          <textarea
            value={bulk}
            onChange={(e) => setBulk(e.target.value)}
            rows={4}
            className={inputCls}
          />
          <button
            type="button"
            onClick={applyBulk}
            disabled={!bulk.trim()}
            className="mt-2 rounded-lg bg-sky-500/10 px-3 py-1.5 text-xs font-semibold text-sky-400 transition-colors hover:bg-sky-500/20 disabled:opacity-40"
          >
            {t('adminArticles.bulkPasteAction')}
          </button>
        </div>
      )}
    </section>
  );
}

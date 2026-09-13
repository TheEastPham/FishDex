/**
 * Kiểu trình bày bài viết. Người viết chọn một kiểu cho mỗi bài, `templateKey` lưu trong DB
 * nên bài cũ không đổi mặt khi mình thêm kiểu mới.
 *
 * Nền chung cho mọi kiểu (theo tài liệu typography cho văn bản dài):
 * - Cột chữ ~66 ký tự, body 17px mobile / 18px desktop, line-height 1.75
 * - Heading cách mục trên rộng gấp đôi khoảng cách xuống nội dung của chính nó
 *
 * Mỗi kiểu có một thứ CHỈ NÓ CÓ, gắn với việc người đọc thật sự làm với loại nội dung đó —
 * chứ không phải chỉ đổi khoảng cách với bề rộng.
 */
export interface ArticleTemplateSpec {
  key: string;
  labelKey: string;
  descKey: string;

  // ── Bố cục ──────────────────────────────────────────────
  /** inline = ảnh bìa dưới tiêu đề; overlay = tiêu đề đặt trên ảnh bìa; none = không dùng ảnh bìa */
  hero: 'inline' | 'overlay' | 'none';
  /** measure = ảnh bó trong cột chữ; wide = ảnh tràn rộng hơn cột chữ */
  imageWidth: 'measure' | 'wide';
  /** Đoạn đầu của mở bài phóng to thành đoạn dẫn */
  lead: boolean;

  // ── Đặc sắc riêng ───────────────────────────────────────
  /** Ảnh của bài gom vào một băng ảnh đặt đúng chỗ loạt ảnh trong thân bài. Ảnh là chính. */
  gallery: boolean;
  /** Tiêu đề mục tự đánh số. Hướng dẫn. */
  numberedHeadings: boolean;
  /** Mục lục dính bên phải trên desktop, tự sáng mục đang đọc. Hướng dẫn. */
  toc: boolean;
  /** List đánh số thành checkbox, tick được và nhớ theo bài. Hướng dẫn. */
  checklist: boolean;
  /** Thanh tiến độ đọc dính trên đỉnh. */
  progress: boolean;
  /** Chữ cái đầu của đoạn dẫn thả to. Tạp chí. */
  dropCap: boolean;
  /** Quote tràn rộng, chữ lớn như pull-quote báo giấy. Tạp chí. */
  pullQuote: boolean;
  /** Ảnh bìa trôi chậm hơn trang khi cuộn. Tạp chí. */
  parallax: boolean;
  /** Nút chỉnh cỡ chữ cho người đọc, nhớ trong localStorage. */
  readerControls: boolean;
}

const base = {
  gallery: false,
  numberedHeadings: false,
  toc: false,
  checklist: false,
  progress: false,
  dropCap: false,
  pullQuote: false,
  parallax: false,
  readerControls: false,
};

export const ARTICLE_TEMPLATES: ArticleTemplateSpec[] = [
  {
    ...base,
    key: 'standard',
    labelKey: 'articleTemplates.standardLabel',
    descKey: 'articleTemplates.standardDesc',
    hero: 'inline',
    imageWidth: 'measure',
    lead: true,
    progress: true,
    readerControls: true,
  },
  {
    ...base,
    key: 'magazine',
    labelKey: 'articleTemplates.magazineLabel',
    descKey: 'articleTemplates.magazineDesc',
    hero: 'overlay',
    imageWidth: 'wide',
    lead: true,
    progress: true,
    dropCap: true,
    pullQuote: true,
    parallax: true,
  },
  {
    ...base,
    key: 'guide',
    labelKey: 'articleTemplates.guideLabel',
    descKey: 'articleTemplates.guideDesc',
    hero: 'inline',
    imageWidth: 'measure',
    lead: false,
    numberedHeadings: true,
    toc: true,
    checklist: true,
    progress: true,
  },
  {
    ...base,
    key: 'photo',
    labelKey: 'articleTemplates.photoLabel',
    descKey: 'articleTemplates.photoDesc',
    hero: 'none',
    imageWidth: 'wide',
    lead: false,
    gallery: true,
  },
];

export const DEFAULT_TEMPLATE = ARTICLE_TEMPLATES[0];

/** Bài lưu key lạ (kiểu bị bỏ đi sau này) vẫn phải render được — lùi về standard. */
export function templateSpec(key: string | undefined): ArticleTemplateSpec {
  return ARTICLE_TEMPLATES.find((x) => x.key === key) ?? DEFAULT_TEMPLATE;
}

/** Id neo cho mục lục — phải khớp giữa renderer và danh sách mục lục. */
export function headingAnchor(index: number): string {
  return `sec-${index}`;
}

/**
 * Cột chữ của bài đọc: grid 3 cột, cột giữa rộng 38rem (≈608px, khoảng 66–68 ký tự ở cỡ 18px).
 *
 * Đo bằng `rem` chứ KHÔNG bằng `ch` đặt trên từng element: `ch` tính theo cỡ chữ của chính
 * element đó, nên cùng một class `max-w-[66ch]` sẽ cho h2 26px một cột rộng hơn đoạn văn 18px
 * và chữ lệch cột. Ảnh cần rộng hơn thì dùng `col-span-3` để phá ra hai bên.
 */
export const PROSE_COLUMN = 'grid-cols-[1fr_min(38rem,100%)_1fr]';

/** Dùng cho các khối ngoài renderer (header, hộp khóa bài) để thẳng cột với thân bài. */
export const PROSE_WIDTH = 'mx-auto w-full max-w-[38rem]';

/** Ba nấc cỡ chữ cho nút A− / A / A+. Nhân vào cỡ nền, không thay cỡ nền. */
export const FONT_SCALES = [0.92, 1, 1.15] as const;

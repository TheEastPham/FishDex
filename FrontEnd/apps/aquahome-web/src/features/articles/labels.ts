import { ArticleType, ReadingLevel } from '@fishlover/shared';

/** Khóa i18n cho từng loại bài — enum số bên BE, chữ bên FE. */
export const TYPE_KEYS: Record<ArticleType, string> = {
  [ArticleType.Setup]:   'articles.typeSetup',
  [ArticleType.Care]:    'articles.typeCare',
  [ArticleType.Species]: 'articles.typeSpecies',
  [ArticleType.Nature]:  'articles.typeNature',
};

export const LEVEL_KEYS: Record<ReadingLevel, string> = {
  [ReadingLevel.Beginner]:     'articles.levelBeginner',
  [ReadingLevel.Intermediate]: 'articles.levelIntermediate',
  [ReadingLevel.Advanced]:     'articles.levelAdvanced',
};

/** Mỗi loại một màu badge để lướt danh sách phân biệt được ngay, không phải đọc chữ. */
export const TYPE_BADGE: Record<ArticleType, string> = {
  [ArticleType.Setup]:   'bg-sky-500/10 text-sky-400 border-sky-500/20',
  [ArticleType.Care]:    'bg-amber-500/10 text-amber-400 border-amber-500/20',
  [ArticleType.Species]: 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20',
  [ArticleType.Nature]:  'bg-teal-500/10 text-teal-300 border-teal-500/20',
};

export const LEVEL_BADGE: Record<ReadingLevel, string> = {
  [ReadingLevel.Beginner]:     'bg-emerald-500/10 text-emerald-400 border-emerald-500/20',
  [ReadingLevel.Intermediate]: 'bg-amber-500/10 text-amber-400 border-amber-500/20',
  [ReadingLevel.Advanced]:     'bg-rose-500/10 text-rose-400 border-rose-500/20',
};

export const ARTICLE_TYPES: ArticleType[] = [
  ArticleType.Setup,
  ArticleType.Care,
  ArticleType.Species,
  ArticleType.Nature,
];

export const READING_LEVELS: ReadingLevel[] = [
  ReadingLevel.Beginner,
  ReadingLevel.Intermediate,
  ReadingLevel.Advanced,
];

/** Ngày đăng — chỉ cần ngày, giờ không có ý nghĩa với người đọc bài viết. */
export function formatArticleDate(iso: string | null, locale: string): string {
  if (!iso) return '';
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return '';
  return d.toLocaleDateString(locale === 'vi' ? 'vi-VN' : locale, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  });
}

/**
 * Tên ngôn ngữ của bản dịch. Không dùng `languageNames.*` có sẵn vì bộ đó đánh khóa theo mã
 * quốc gia (VN/US/CN) chứ không theo mã ngôn ngữ (vi/en/zh) mà API bài viết trả về.
 */
export const LANGUAGE_KEYS: Record<string, string> = {
  vi: 'articles.langVi',
  en: 'articles.langEn',
  de: 'articles.langDe',
  zh: 'articles.langZh',
};

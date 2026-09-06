/**
 * Bài viết — khớp DTO của AquaHome tại AquaHome.Domain/DTOs/ArticleDtos.cs.
 *
 * Điểm khác mọi feature còn lại: response của API chỉ có metadata, còn nội dung nằm ở
 * `contentUrl` (presigned GET tới content.json trên R2). FE tải file đó rồi render theo
 * `templateKey`; ảnh trong bài do block trỏ `assetId`, tra sang mảng `assets` để lấy URL.
 */

/**
 * Chia theo giai đoạn người đọc đang đứng, không theo giọng văn. Giữ ít nhánh vì phần chi tiết
 * đã có tag lo. Phải khớp đúng số với AquaHome.Domain/Enums/ArticleType.cs.
 */
export enum ArticleType {
  /** Dựng bể, thiết bị, cycling, nền, cây và bố cục */
  Setup = 0,
  /** Nước, cho ăn, bảo dưỡng, bệnh, rêu hại */
  Care = 1,
  /** Chuyên sâu loài, ghép bể, ép đẻ, ương cá con */
  Species = 2,
  /** Sinh cảnh tự nhiên, bảo tồn, chuyện của người chơi — nối người nuôi với thiên nhiên và với nhau */
  Nature = 3,
}

export enum ReadingLevel {
  Beginner = 0,
  Intermediate = 1,
  Advanced = 2,
}

export enum ArticleStatus {
  Draft = 0,
  Published = 1,
  Archived = 2,
}

/** BE chỉ chấp nhận đúng 6 loại này, gửi loại khác trả 422. */
export type ArticleBlockType = 'paragraph' | 'heading' | 'image' | 'list' | 'quote' | 'tip';

/**
 * Union phẳng: field không thuộc loại block thì vắng mặt (BE bỏ field null khi ghi file).
 * Render theo `type`, đừng đoán theo field nào có giá trị.
 */
export interface ArticleBlock {
  type: ArticleBlockType;
  /** paragraph | heading | quote | tip */
  text?: string;
  /** heading: 2 hoặc 3 */
  level?: number;
  /** image — id của ArticleAsset thuộc chính bài này */
  assetId?: string;
  caption?: string;
  alt?: string;
  /** list */
  ordered?: boolean;
  items?: string[];
  /** quote */
  cite?: string;
}

/** Chính là file content.json trên R2. */
export interface ArticleContent {
  schemaVersion: number;
  template: string;
  intro?: ArticleBlock[];
  body?: ArticleBlock[];
  conclusion?: ArticleBlock[];
}

export interface ArticleAssetDto {
  id: string;
  /** Presigned GET, hết hạn sau 60 phút — luôn dùng URL của lần gọi API gần nhất. */
  url: string | null;
  fileName: string | null;
  contentType: string;
  bytes: number;
}

export interface ArticleListItemDto {
  id: string;
  slug: string;
  type: ArticleType;
  readingLevel: ReadingLevel;
  templateKey: string;
  tags: string[];
  /** Ngôn ngữ thực của bản trả về — có thể khác ngôn ngữ yêu cầu do fallback en → vi. */
  language: string;
  title: string;
  summary: string | null;
  thumbnailUrl: string | null;
  authorName: string | null;
  readingMinutes: number;
  viewCount: number;
  isFeatured: boolean;
  /** Khách chưa đăng nhập chỉ đọc được bài Beginner — cờ này để gắn ổ khóa lên card */
  requiresAuth: boolean;
  publishedAt: string | null;
}

export interface ArticleDetailDto {
  id: string;
  slug: string;
  type: ArticleType;
  readingLevel: ReadingLevel;
  templateKey: string;
  tags: string[];
  language: string;
  requestedLanguage: string;
  availableLanguages: string[];
  title: string;
  summary: string | null;
  thumbnailUrl: string | null;
  authorName: string | null;
  readingMinutes: number;
  wordCount: number;
  viewCount: number;
  publishedAt: string | null;
  updatedAt: string;
  /** Null khi bài cần đăng nhập mà người đọc chưa đăng nhập — BE không phát URL nội dung. */
  contentUrl: string | null;
  /** True = phải đăng nhập mới đọc được nội dung (bài từ Intermediate trở lên). */
  requiresAuth: boolean;
  /** Rỗng khi bài đang bị khóa — BE không phát cả URL ảnh trong bài. */
  assets: ArticleAssetDto[];
}

export interface AdminArticleTranslationDto {
  language: string;
  title: string;
  summary: string | null;
  blockCount: number;
  wordCount: number;
  readingMinutes: number;
  contentUrl: string | null;
  updatedAt: string;
}

export interface AdminArticleDto {
  id: string;
  slug: string;
  type: ArticleType;
  readingLevel: ReadingLevel;
  status: ArticleStatus;
  templateKey: string;
  tags: string[];
  thumbnailUrl: string | null;
  authorName: string | null;
  isFeatured: boolean;
  viewCount: number;
  publishedAt: string | null;
  createdAt: string;
  updatedAt: string;
  translations: AdminArticleTranslationDto[];
  assets: ArticleAssetDto[];
}

export interface GetArticlesParams {
  lang?: string;
  type?: ArticleType;
  level?: ReadingLevel;
  tag?: string;
  q?: string;
  page?: number;
  pageSize?: number;
}

/** SchemaVersion/template do BE gán, FE chỉ gửi 3 mục nội dung. */
export interface ArticleContentInput {
  intro?: ArticleBlock[];
  body?: ArticleBlock[];
  conclusion?: ArticleBlock[];
}

export interface CreateArticlePayload {
  type: ArticleType;
  readingLevel: ReadingLevel;
  tags?: string[];
  /** Bỏ trống thì BE tự sinh slug từ tiêu đề. */
  slug?: string;
  language: string;
  title: string;
  summary?: string;
  content?: ArticleContentInput;
}

export interface UpdateArticlePayload {
  type: ArticleType;
  readingLevel: ReadingLevel;
  tags?: string[];
  slug?: string;
  isFeatured: boolean;
}

export interface UpsertTranslationPayload {
  title: string;
  summary?: string;
  content?: ArticleContentInput;
}

/** Lỗi 422 của BE: mỗi phần tử chỉ đúng vị trí sai, ví dụ "body[2]: thiếu text". */
export interface ArticleValidationError {
  error: string;
  errors: string[];
}

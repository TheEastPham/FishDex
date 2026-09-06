import { apiClient } from './client';
import type { PagedResult } from '../../types/common';
import type {
  AdminArticleDto,
  ArticleAssetDto,
  ArticleContent,
  ArticleDetailDto,
  ArticleListItemDto,
  ArticleStatus,
  CreateArticlePayload,
  GetArticlesParams,
  UpdateArticlePayload,
  UpsertTranslationPayload,
} from '../../types/articles';

const BASE = '/aquahome/v1/articles';
const ADMIN = '/aquahome/v1/admin/articles';

// ── Công khai ─────────────────────────────────────────────

export async function getArticles(params: GetArticlesParams = {}): Promise<PagedResult<ArticleListItemDto>> {
  const { data } = await apiClient.get<PagedResult<ArticleListItemDto>>(BASE, { params });
  return data;
}

export async function getArticleBySlug(slug: string, lang?: string): Promise<ArticleDetailDto> {
  const { data } = await apiClient.get<ArticleDetailDto>(`${BASE}/${slug}`, { params: { lang } });
  return data;
}

/** Đếm lượt xem — gọi một lần khi mở bài, lỗi thì kệ, không chặn việc đọc. */
export async function recordArticleView(slug: string): Promise<void> {
  await apiClient.post(`${BASE}/${slug}/view`);
}

/**
 * Tải content.json thẳng từ R2 bằng presigned URL.
 *
 * KHÔNG dùng apiClient: interceptor sẽ gắn header Authorization, mà R2 coi request vừa có
 * chữ ký trên query vừa có header auth là lỗi. Dùng fetch trần, không kèm credentials.
 */
export async function fetchArticleContent(contentUrl: string): Promise<ArticleContent> {
  const res = await fetch(contentUrl, { credentials: 'omit' });
  if (!res.ok) throw new Error(`Không tải được nội dung bài viết (HTTP ${res.status})`);
  return (await res.json()) as ArticleContent;
}

// ── Admin (ContentAdmin | SystemAdmin) ────────────────────

export async function getAdminArticles(params: {
  status?: ArticleStatus;
  q?: string;
  page?: number;
  pageSize?: number;
} = {}): Promise<PagedResult<AdminArticleDto>> {
  const { data } = await apiClient.get<PagedResult<AdminArticleDto>>(ADMIN, { params });
  return data;
}

export async function getAdminArticle(id: string): Promise<AdminArticleDto> {
  const { data } = await apiClient.get<AdminArticleDto>(`${ADMIN}/${id}`);
  return data;
}

export async function createArticle(payload: CreateArticlePayload): Promise<AdminArticleDto> {
  const { data } = await apiClient.post<AdminArticleDto>(ADMIN, payload);
  return data;
}

export async function updateArticle(id: string, payload: UpdateArticlePayload): Promise<AdminArticleDto> {
  const { data } = await apiClient.put<AdminArticleDto>(`${ADMIN}/${id}`, payload);
  return data;
}

/** Tạo mới hoặc ghi đè bản dịch — BE validate block rồi mới ghi content.json lên R2. */
export async function upsertArticleTranslation(
  id: string,
  language: string,
  payload: UpsertTranslationPayload,
): Promise<AdminArticleDto> {
  const { data } = await apiClient.put<AdminArticleDto>(`${ADMIN}/${id}/translations/${language}`, payload);
  return data;
}

export async function deleteArticleTranslation(id: string, language: string): Promise<AdminArticleDto> {
  const { data } = await apiClient.delete<AdminArticleDto>(`${ADMIN}/${id}/translations/${language}`);
  return data;
}

/** Upload ảnh dùng trong bài — trả assetId để chèn vào block image. Tối đa 5MB. */
export async function uploadArticleAsset(id: string, file: File): Promise<ArticleAssetDto> {
  const form = new FormData();
  form.append('file', file);
  const { data } = await apiClient.post<ArticleAssetDto>(`${ADMIN}/${id}/assets`, form);
  return data;
}

export async function deleteArticleAsset(id: string, assetId: string): Promise<void> {
  await apiClient.delete(`${ADMIN}/${id}/assets/${assetId}`);
}

/** Thumbnail của bài. Tối đa 3MB, ghi đè ảnh cũ. */
export async function setArticleCover(id: string, file: File): Promise<AdminArticleDto> {
  const form = new FormData();
  form.append('file', file);
  const { data } = await apiClient.post<AdminArticleDto>(`${ADMIN}/${id}/cover`, form);
  return data;
}

export async function publishArticle(id: string): Promise<AdminArticleDto> {
  const { data } = await apiClient.post<AdminArticleDto>(`${ADMIN}/${id}/publish`);
  return data;
}

export async function unpublishArticle(id: string): Promise<AdminArticleDto> {
  const { data } = await apiClient.post<AdminArticleDto>(`${ADMIN}/${id}/unpublish`);
  return data;
}

export async function archiveArticle(id: string): Promise<AdminArticleDto> {
  const { data } = await apiClient.post<AdminArticleDto>(`${ADMIN}/${id}/archive`);
  return data;
}

export async function deleteArticle(id: string): Promise<void> {
  await apiClient.delete(`${ADMIN}/${id}`);
}

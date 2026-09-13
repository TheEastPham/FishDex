/**
 * Bóc id video từ link YouTube. Giữ cùng luật với `ArticleContentBuilder.ExtractYoutubeId` bên BE.
 *
 * BE mới là nơi quyết định — nó bóc lại lúc lưu và từ chối link sai bằng 422. Bản FE này chỉ để
 * trình soạn hiện thumbnail xem trước ngay lúc dán, khỏi phải lưu mới biết link có dùng được không.
 */
export function extractYoutubeId(input?: string | null): string | null {
  const value = input?.trim();
  if (!value) return null;

  // Id YouTube luôn là 11 ký tự trong [A-Za-z0-9_-]
  if (/^[A-Za-z0-9_-]{11}$/.test(value)) return value;

  const match = value.match(/(?:youtu\.be\/|\/embed\/|\/shorts\/|\/live\/|[?&]v=)([A-Za-z0-9_-]{11})/i);
  return match ? match[1] : null;
}

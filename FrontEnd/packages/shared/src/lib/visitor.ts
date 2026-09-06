const STORAGE_KEY = 'fishlover.visitorId';

let cached: string | null = null;

/**
 * ID ẩn danh để BE đếm hạn mức xem loài của khách chưa đăng nhập.
 *
 * Gửi qua header chứ không phải cookie: FE ở Cloudflare còn API ở api.fishlover.org — khác origin
 * nên cookie sẽ phải SameSite=None + CORS credentials, phiền hơn mà không chắc chắn hơn.
 *
 * Xoá localStorage là reset được hạn mức — chấp nhận. Đây là phanh mềm để nhắc người xem đăng ký;
 * thứ chặn scraper thật sự là trần theo IP và rate limit ở gateway.
 */
export function getVisitorId(): string {
  if (cached) return cached;

  try {
    const stored = localStorage.getItem(STORAGE_KEY);
    if (stored) {
      cached = stored;
      return stored;
    }

    const fresh = newId();
    localStorage.setItem(STORAGE_KEY, fresh);
    cached = fresh;
    return fresh;
  } catch {
    // Safari private mode / storage bị chặn → ID sống theo tab, hạn mức tính theo IP là chính.
    cached ??= newId();
    return cached;
  }
}

function newId(): string {
  if (typeof crypto !== 'undefined' && 'randomUUID' in crypto) return crypto.randomUUID();
  return `v-${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 10)}`;
}

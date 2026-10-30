import axios from 'axios';

/**
 * Media-access cho ảnh bucket system-image (FishDex).
 *
 * BE trả object key bất biến thay vì presigned URL (URL cũ đổi mỗi lần gọi API và chết sau 60 phút).
 * FE xin 1 SAS dùng chung cho mọi key rồi tự ghép:
 *   {gateway}/fishdex/v1/public/media-access?path={encodeURIComponent(key)}&{sas}
 * → URL giữ nguyên suốt đời SAS nên browser cache được ảnh; BE redirect 302 sang R2.
 *
 * SAS được giữ ở module (dùng chung mọi component) và tự gia hạn trước khi hết hạn.
 */

const MEDIA_ACCESS_PATH = '/fishdex/v1/public/media-access';
/** Gia hạn khi còn ít hơn chừng này — đủ để ảnh đang tải dở không bị 403. */
const REFRESH_MARGIN_MS = 5 * 60 * 1000;
/** Xin lỗi thì đợi bấy nhiêu rồi mới thử lại — tránh spam khi BE chưa cấu hình (503). */
const RETRY_AFTER_FAILURE_MS = 60 * 1000;

export type MediaSasStatus = 'idle' | 'loading' | 'ready' | 'failed';

interface MediaSasState {
  status: MediaSasStatus;
  sas: string | null;
}

interface SharedMediaAccessSignatureDto {
  sas: string;
  expiresAt: string;
}

const client = axios.create({ baseURL: import.meta.env.VITE_GATEWAY_URL });

let state: MediaSasState = { status: 'idle', sas: null };
let expiresAtMs = 0;
let failedAtMs = 0;
let inflight: Promise<void> | null = null;
let refreshTimer: ReturnType<typeof setTimeout> | null = null;
const listeners = new Set<() => void>();

function setState(next: MediaSasState): void {
  state = next;
  listeners.forEach((l) => l());
}

export function subscribeMediaSas(listener: () => void): () => void {
  listeners.add(listener);
  return () => listeners.delete(listener);
}

export function getMediaSasState(): MediaSasState {
  return state;
}

/** Bảo đảm có SAS còn hạn. Gọi bao nhiêu lần cũng chỉ ra 1 request. */
export function ensureMediaSas(): Promise<void> {
  const now = Date.now();
  if (state.sas && expiresAtMs - now > REFRESH_MARGIN_MS) return Promise.resolve();
  if (state.status === 'failed' && now - failedAtMs < RETRY_AFTER_FAILURE_MS) return Promise.resolve();
  if (inflight) return inflight;

  // Đang có SAS cũ còn dùng được thì giữ 'ready' trong lúc gia hạn — ảnh không nháy.
  if (!state.sas) setState({ status: 'loading', sas: null });

  inflight = client
    .post<SharedMediaAccessSignatureDto>(`${MEDIA_ACCESS_PATH}/sas`)
    .then(({ data }) => {
      expiresAtMs = Date.parse(data.expiresAt);
      setState({ status: 'ready', sas: data.sas });
      scheduleRefresh();
    })
    .catch(() => {
      failedAtMs = Date.now();
      // SAS cũ vẫn còn hạn thì cứ dùng tiếp, chỉ đánh failed khi thật sự không còn gì.
      if (!state.sas || expiresAtMs <= Date.now()) setState({ status: 'failed', sas: null });
    })
    .finally(() => { inflight = null; });

  return inflight;
}

function scheduleRefresh(): void {
  if (refreshTimer) clearTimeout(refreshTimer);
  const delay = Math.max(0, expiresAtMs - Date.now() - REFRESH_MARGIN_MS);
  refreshTimer = setTimeout(() => { void ensureMediaSas(); }, delay);
}

export function buildMediaUrl(key: string, sas: string): string {
  return `${import.meta.env.VITE_GATEWAY_URL}${MEDIA_ACCESS_PATH}?path=${encodeURIComponent(key)}&${sas}`;
}

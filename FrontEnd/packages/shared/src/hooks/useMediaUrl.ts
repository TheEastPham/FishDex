import { useCallback, useEffect, useSyncExternalStore } from 'react';
import { buildMediaUrl, ensureMediaSas, getMediaSasState, subscribeMediaSas } from '../lib/mediaAccess';

/**
 * Trả hàm resolve ảnh bucket system-image: `mediaSrc(imageKey, legacyUrl?)`.
 *
 * - Có key + SAS sẵn sàng → URL media-access (ổn định, browser cache được).
 * - Có key nhưng SAS đang tải → null (placeholder thoáng qua, không tải 2 lần cùng 1 ảnh).
 * - Không có key, hoặc xin SAS thất bại → rơi về legacyUrl (presigned cũ, BE chưa lên bản mới vẫn hiện ảnh).
 */
export function useMediaUrl() {
  const { status, sas } = useSyncExternalStore(subscribeMediaSas, getMediaSasState);

  useEffect(() => { void ensureMediaSas(); }, []);

  return useCallback(
    (key?: string | null, legacyUrl?: string | null): string | null => {
      if (key && sas) return buildMediaUrl(key, sas);
      if (key && (status === 'idle' || status === 'loading')) return null;
      return legacyUrl ?? null;
    },
    [status, sas],
  );
}

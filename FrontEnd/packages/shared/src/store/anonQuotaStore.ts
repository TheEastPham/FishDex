import { create } from 'zustand';

interface AnonQuotaState {
  /** Chưa gọi API loài nào thì chưa biết hạn mức — đừng hiện meter với con số đoán. */
  known: boolean;
  limit: number;
  used: number;
  remaining: number;
  /** Số giây tới nửa đêm giờ VN, lúc hạn mức reset. */
  resetsInSeconds: number;
  exhausted: boolean;
  setFromHeaders: (limit: number, used: number, remaining: number, resetsInSeconds: number) => void;
  setExhausted: (limit: number, resetsInSeconds: number) => void;
  reset: () => void;
}

const EMPTY = {
  known: false,
  limit: 0,
  used: 0,
  remaining: 0,
  resetsInSeconds: 0,
  exhausted: false,
};

/**
 * Hạn mức xem loài của khách, đọc từ header response của FishDex (X-Anon-Views-*).
 * Không có endpoint riêng để hỏi số lượt — mỗi lần xem profile là một lần cập nhật.
 */
export const useAnonQuotaStore = create<AnonQuotaState>()((set) => ({
  ...EMPTY,
  setFromHeaders: (limit, used, remaining, resetsInSeconds) =>
    set({ known: true, limit, used, remaining, resetsInSeconds, exhausted: false }),
  setExhausted: (limit, resetsInSeconds) =>
    set({ known: true, limit, used: limit, remaining: 0, resetsInSeconds, exhausted: true }),
  reset: () => set(EMPTY),
}));

/**
 * Danh sách quốc gia có lớp market, mirror `MarketCountries.cs` bên FishDex.
 *
 * Chỉ giữ mã và cờ — **tên nước KHÔNG hardcode ở đây** mà tra qua i18n key `countries.<alpha2>`,
 * theo đúng rule i18n của FrontEnd/CLAUDE.md.
 *
 * Mã số C_Code của FishBase chỉ tồn tại trong DB; FE và URL chỉ dùng alpha-2.
 */
export interface MarketCountry {
  alpha2: string;
  /** Emoji cờ, dựng từ regional indicator symbols. */
  flag: string;
  /** C_Code của FishBase — giá trị lưu trong DB, gửi kèm khi đóng góp tên bản ngữ. */
  code: string;
  /**
   * Ngôn ngữ mặc định để prefill khi đặt tên bản ngữ. Phải viết ĐÚNG như FishBase —
   * lưu ý Indonesia là "Bahasa Indonesia" chứ không phải "Indonesian".
   */
  defaultLanguage: string;
}

/** Thứ tự khớp `DisplayOrder` bên BE: Việt Nam trước, còn lại theo quy mô thị trường. */
export const MARKET_COUNTRIES: readonly MarketCountry[] = [
  { alpha2: 'VN', flag: '🇻🇳', code: '704', defaultLanguage: 'Vietnamese' },
  { alpha2: 'US', flag: '🇺🇸', code: '840', defaultLanguage: 'English' },
  { alpha2: 'CN', flag: '🇨🇳', code: '156', defaultLanguage: 'Mandarin Chinese' },
  { alpha2: 'JP', flag: '🇯🇵', code: '392', defaultLanguage: 'Japanese' },
  { alpha2: 'NL', flag: '🇳🇱', code: '528', defaultLanguage: 'Dutch' },
  { alpha2: 'DE', flag: '🇩🇪', code: '276', defaultLanguage: 'German' },
  { alpha2: 'GB', flag: '🇬🇧', code: '826', defaultLanguage: 'English' },
  { alpha2: 'IN', flag: '🇮🇳', code: '356', defaultLanguage: 'Hindi' },
  { alpha2: 'MY', flag: '🇲🇾', code: '458', defaultLanguage: 'Malay' },
  { alpha2: 'SG', flag: '🇸🇬', code: '702', defaultLanguage: 'English' },
  { alpha2: 'TH', flag: '🇹🇭', code: '764', defaultLanguage: 'Thai' },
  { alpha2: 'ID', flag: '🇮🇩', code: '360', defaultLanguage: 'Bahasa Indonesia' },
] as const;

/** Tra nguyên bản ghi theo alpha-2. Trả undefined nếu không phải nước có market. */
export function findCountry(alpha2: string): MarketCountry | undefined {
  return MARKET_COUNTRIES.find((c) => c.alpha2 === alpha2);
}

/** Nước mặc định khi chưa chọn gì và localStorage còn trống. */
export const DEFAULT_MARKET_COUNTRY = 'VN';

const STORAGE_KEY = 'market_country';

export function getCountryFlag(alpha2: string): string {
  return MARKET_COUNTRIES.find((c) => c.alpha2 === alpha2)?.flag ?? '';
}

/**
 * Nước đang chọn, ưu tiên localStorage để lần sau vào đúng nước.
 * Chỉ chấp nhận mã nằm trong danh sách — giá trị lạ thì rơi về mặc định.
 */
export function getStoredCountry(): string {
  try {
    const saved = localStorage.getItem(STORAGE_KEY);
    if (saved && MARKET_COUNTRIES.some((c) => c.alpha2 === saved)) return saved;
  } catch {
    // localStorage có thể bị chặn (private mode) — không phải lỗi, cứ dùng mặc định.
  }
  return DEFAULT_MARKET_COUNTRY;
}

export function storeCountry(alpha2: string): void {
  try {
    localStorage.setItem(STORAGE_KEY, alpha2);
  } catch {
    // Không lưu được thì thôi, không chặn luồng chính.
  }
}

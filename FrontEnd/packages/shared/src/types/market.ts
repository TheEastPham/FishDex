/**
 * Market layer — danh sách cá đang được bán ở từng quốc gia.
 * Khớp DTO của FishDex tại FishDex.Domain/DTOs/Market/MarketDtos.cs.
 */

/** Mức phổ biến trên thị trường. Null nghĩa là chưa ai curate — phần lớn dòng suy từ bể cá đều null. */
export enum TradeStatus {
  Common = 0,
  Occasional = 1,
  Seasonal = 2,
  Rare = 3,
}

/** Trạng thái pháp lý theo từng quốc gia. Chỉ admin đặt, và phải kèm nguồn khi khác Legal. */
export enum LegalStatus {
  Legal = 0,
  Restricted = 1,
  Banned = 2,
}

/**
 * Khoảng kích thước khi trưởng thành, suy từ `Species.Length` lúc query.
 * KHÔNG phải cột trong DB — mỗi loài không lưu sẵn size band.
 */
export enum SizeBand {
  Under5 = 0,
  From5To10 = 1,
  From10To20 = 2,
  Over20 = 3,
}

export enum NameStatusFilter {
  All = 0,
  Has = 1,
  Missing = 2,
}

export interface MarketCountryDto {
  /** ISO alpha-2, dùng ở URL và i18n key. Mã số C_Code chỉ tồn tại trong DB. */
  alpha2: string;
  nameEn: string;
  /** Tên ngôn ngữ theo cách FishBase viết, vd "Bahasa Indonesia" chứ không phải "Indonesian". */
  languages: string[];
  /**
   * Nước đã bật trang market chưa. Dropdown hiện cả nước chưa bật để người dùng thấy lộ trình,
   * nhưng chọn vào thì FE không gọi endpoint dữ liệu mà hiện thông báo chưa khảo sát.
   */
  isEnabled: boolean;
}

export interface MarketSpeciesDto {
  specCode: number;
  scientificName: string;
  /** Null nghĩa là loài đang chờ được đặt tên — chỗ này thành nút mời đóng góp. */
  localName: string | null;
  imageUrl: string | null;
  lengthCm: number | null;
  tradeStatus: TradeStatus | null;
  legalStatus: LegalStatus;
  legalNote: string | null;
  /** Chỉ số dễ tổn thương của FishBase. FE tự quyết ngưỡng hiện badge. */
  vulnerability: number | null;
  /** Giá trị thô của FishBase: harmless, venomous, traumatogenic… */
  dangerous: string | null;
}

export interface MarketStatsDto {
  traded: number;
  withLocalName: number;
  /** Con số được nhấn trên UI — đọc thành lời mời thay vì lời thú nhận. */
  awaitingName: number;
}

export interface GetMarketSpeciesParams {
  page?: number;
  pageSize?: number;
  sizeBand?: SizeBand;
  nameStatus?: NameStatusFilter;
}

/** Kết quả tra index toàn bộ FishBase để phân luồng khi loài chưa có trong danh sách. */
export enum SpeciesLookupOutcome {
  /** Có trong FishDex — thêm thẳng vào danh sách quốc gia. */
  InFishDex = 0,
  /** Có trong FishBase nhưng chưa nạp — cần chạy ETL. */
  NeedsMigration = 1,
}

export interface SpeciesLookupDto {
  specCode: number;
  scientificName: string;
  genus: string | null;
  outcome: SpeciesLookupOutcome;
}

export interface AddTradedSpeciesRequest {
  specCode: number;
  tradeStatus?: TradeStatus | null;
  legalStatus?: LegalStatus;
  legalNote?: string | null;
  legalSourceUrl?: string | null;
}

/**
 * Cấu hình tile layer dùng chung cho mọi map Leaflet trong app.
 *
 * Gom về một chỗ vì CARTO đã đổi chính sách giữa đường: URL `basemaps.cartocdn.com`
 * trước đây dùng ẩn danh được, từ 2026 bắt buộc API key và trả về tile watermark
 * "API KEY REQUIRED" nếu thiếu. Lần sau provider lại đổi thì sửa đúng file này.
 *
 * Lấy key free tại https://carto.com/basemaps/apikey — 5 triệu tile request/tháng,
 * vượt hạn mức họ liên hệ chứ không cắt service. Free tier nhắm cho non-commercial;
 * nếu market layer sau này có doanh thu thì cần chuyển sang commercial agreement.
 *
 * TODO(mobile): đọc import.meta.env — khi làm app native cần truyền key qua config object.
 * TODO: CARTO đang retire raster basemap để đẩy sang vector tiles. Raster `dark_all`
 *       vẫn sống nhưng nằm trên đường deprecate — theo dõi để chuyển sang MapLibre vector.
 */

export interface TileLayerConfig {
  url: string;
  /** Bắt buộc hiển thị theo điều kiện free tier của CARTO — đừng bỏ. */
  attribution: string;
  /** Chỉ set khi URL có `{s}`. Leaflet default là 'abc'. */
  subdomains?: string;
  maxZoom: number;
  /**
   * Class gắn vào container tile. Chỉ dùng cho fallback OSM — OSM chỉ có nền sáng
   * nên phải invert bằng CSS mới khớp dark theme (xem `.map-tiles-inverted` trong index.css).
   */
  className?: string;
}

const CARTO_KEY = import.meta.env.VITE_CARTO_API_KEY;

const CARTO_ATTRIBUTION =
  '&copy; <a href="https://carto.com/attributions">CARTO</a> | ' +
  '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>';

const OSM_ATTRIBUTION = '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>';

/** CARTO Dark Matter — nền dark thật, khớp theme app. Cần key. */
const CARTO_DARK: TileLayerConfig = {
  url: `https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png?key=${CARTO_KEY}`,
  attribution: CARTO_ATTRIBUTION,
  subdomains: 'abcd',
  maxZoom: 20,
};

/**
 * Fallback khi chưa có key: OSM standard, không cần key, không giới hạn quota.
 * Nền sáng nên phải invert qua CSS. Không dùng `{s}` vì OSM đã deprecate subdomain.
 * maxZoom 19 — OSM không có tile z20.
 */
const OSM_DARK_FALLBACK: TileLayerConfig = {
  url: 'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
  attribution: OSM_ATTRIBUTION,
  maxZoom: 19,
  className: 'map-tiles-inverted',
};

/** Spread trực tiếp vào `<TileLayer {...MAP_TILE_LAYER} />`. */
export const MAP_TILE_LAYER: TileLayerConfig = CARTO_KEY ? CARTO_DARK : OSM_DARK_FALLBACK;

if (!CARTO_KEY && import.meta.env.DEV) {
  console.warn('[FishLover] Thiếu VITE_CARTO_API_KEY — map đang dùng fallback OSM (nền invert).');
}

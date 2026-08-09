import type { MarketCountryDto } from '@fishlover/shared';
import { useTranslation } from '@fishlover/shared';

interface Props {
  countries: MarketCountryDto[];
  value: string;
  onChange: (alpha2: string) => void;
  className?: string;
}

/**
 * Chọn quốc gia bằng `select` chứ không phải hàng chip: tránh scroll ngang trên mobile,
 * không vỡ khi danh sách lên 12 nước, và trên iOS mở native picker — cuộn bằng ngón tay
 * quen hơn hẳn.
 *
 * Tên nước tra qua i18n key `countries.<alpha2>`, không hardcode trong helper.
 *
 * <b>Không hiện emoji cờ.</b> Windows không có glyph cho regional indicator symbols nên
 * cờ render ra thành đúng hai chữ cái ("VN Việt Nam") — trông như thừa mã viết tắt.
 *
 * Hiện CẢ nước chưa bật, có ghi chú riêng, để người dùng thấy hệ thống hỗ trợ những đâu.
 */
export default function CountrySelect({ countries, value, onChange, className }: Props) {
  const { t } = useTranslation();

  return (
    <select
      value={value}
      onChange={(e) => onChange(e.target.value)}
      aria-label={t('market.countryLabel')}
      className={
        'h-11 rounded-xl bg-[#141518] border border-slate-700/60 px-3 pr-8 text-[15px] text-white ' +
        'focus:outline-none focus:border-primary/50 focus:ring-4 focus:ring-primary/20 transition-all ' +
        (className ?? '')
      }
    >
      {countries.map((c) => (
        <option key={c.alpha2} value={c.alpha2}>
          {t(`countries.${c.alpha2}`)}{c.isEnabled ? '' : ` — ${t('market.comingSoon')}`}
        </option>
      ))}
    </select>
  );
}

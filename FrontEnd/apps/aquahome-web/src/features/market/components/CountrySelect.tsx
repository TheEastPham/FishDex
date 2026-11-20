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
 * Hiện CẢ nước chưa bật, để người dùng thấy hệ thống hướng tới những đâu — nhưng gom vào
 * `optgroup` và `disabled` thay vì dán " — sắp có" sau từng tên. Cụm từ đó lặp 11 lần đọc rất
 * rối, mà bản chất nó là thuộc tính của cả NHÓM chứ không phải của từng nước.
 *
 * `disabled` để không chọn được: trang của nước chưa bật chỉ ra panel "chưa tới lượt", chọn vào
 * là ngõ cụt. Vẫn vào được bằng URL trực tiếp, và MarketPage đã chặn sẵn ở tầng trang.
 */
export default function CountrySelect({ countries, value, onChange, className }: Props) {
  const { t } = useTranslation();

  // Giữ nguyên thứ tự BE trả về (DisplayOrder — Việt Nam trước, rồi theo quy mô thị trường).
  const enabled = countries.filter((c) => c.isEnabled);
  const upcoming = countries.filter((c) => !c.isEnabled);

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
      {enabled.map((c) => (
        <option key={c.alpha2} value={c.alpha2}>{t(`countries.${c.alpha2}`)}</option>
      ))}

      {upcoming.length > 0 && (
        <optgroup label={t('market.comingSoon')}>
          {upcoming.map((c) => (
            <option key={c.alpha2} value={c.alpha2} disabled>
              {t(`countries.${c.alpha2}`)}
            </option>
          ))}
        </optgroup>
      )}
    </select>
  );
}

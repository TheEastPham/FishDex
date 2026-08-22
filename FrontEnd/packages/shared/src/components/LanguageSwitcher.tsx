import { useTranslation } from 'react-i18next';
import { setLanguage, type AppLanguage } from '../i18n';

interface Props {
  className?: string;
}

// Cùng thứ tự với dropdown trong AppShell. Nút này nằm ở trang Đăng nhập/Đăng ký nên
// phải gọn — XOAY VÒNG qua các locale thay vì đổ ra danh sách. Trước đây nó chỉ lật
// en↔vi, nên từ khi có `de`/`zh` người dùng bấm là không về lại được ngôn ngữ của mình.
const ORDER: { code: AppLanguage; short: string; label: string }[] = [
  { code: 'en', short: 'EN', label: 'English' },
  { code: 'vi', short: 'VI', label: 'Tiếng Việt' },
  { code: 'de', short: 'DE', label: 'Deutsch' },
  { code: 'zh', short: 'ZH', label: '中文' },
];

export function LanguageSwitcher({ className }: Props) {
  const { i18n } = useTranslation();

  // Không tìm thấy (vd locale lạ trong localStorage) → coi như đang ở phần tử đầu.
  const idx = Math.max(0, ORDER.findIndex((l) => l.code === i18n.language));
  const next = ORDER[(idx + 1) % ORDER.length];

  return (
    <button
      onClick={() => setLanguage(next.code)}
      title={`Switch to ${next.label}`}
      className={className}
    >
      {next.short}
    </button>
  );
}

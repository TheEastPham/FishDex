import { useTranslation } from 'react-i18next';
import { setLanguage } from '../i18n';

interface Props {
  className?: string;
}

export function LanguageSwitcher({ className }: Props) {
  const { i18n } = useTranslation();
  const isVI = i18n.language === 'vi';

  return (
    <button
      onClick={() => setLanguage(isVI ? 'en' : 'vi')}
      title={isVI ? 'Switch to English' : 'Chuyển sang Tiếng Việt'}
      className={className}
    >
      {isVI ? 'EN' : 'VI'}
    </button>
  );
}

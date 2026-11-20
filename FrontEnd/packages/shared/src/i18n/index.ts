import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import de from './locales/de';
import en from './locales/en';
import vi from './locales/vi';
import zh from './locales/zh';

const LANG_KEY = 'fishlover_lang';

/** Mã ngôn ngữ UI. `de`/`zh` thêm 20/08 khi bật market Đức và Trung Quốc. */
export type AppLanguage = 'en' | 'vi' | 'de' | 'zh';

const savedLang =
  typeof localStorage !== 'undefined' ? (localStorage.getItem(LANG_KEY) ?? 'en') : 'en';

i18n.use(initReactI18next).init({
  resources: {
    en: { translation: en },
    vi: { translation: vi },
    de: { translation: de },
    zh: { translation: zh },
  },
  lng: savedLang,
  fallbackLng: 'en',
  interpolation: { escapeValue: false },
});

export function setLanguage(lang: AppLanguage) {
  i18n.changeLanguage(lang);
  localStorage.setItem(LANG_KEY, lang);
}

export { i18n };
export { useTranslation } from 'react-i18next';

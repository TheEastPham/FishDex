import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import en from './locales/en';
import vi from './locales/vi';

const LANG_KEY = 'fishlover_lang';

const savedLang =
  typeof localStorage !== 'undefined' ? (localStorage.getItem(LANG_KEY) ?? 'en') : 'en';

i18n.use(initReactI18next).init({
  resources: {
    en: { translation: en },
    vi: { translation: vi },
  },
  lng: savedLang,
  fallbackLng: 'en',
  interpolation: { escapeValue: false },
});

export function setLanguage(lang: 'en' | 'vi') {
  i18n.changeLanguage(lang);
  localStorage.setItem(LANG_KEY, lang);
}

export { i18n };
export { useTranslation } from 'react-i18next';

import i18n from 'i18next'
import LanguageDetector from 'i18next-browser-languagedetector'
import { initReactI18next } from 'react-i18next'
import en from './en.json'

/**
 * The SPA's translations. It speaks the language the user last picked, or else the browser's first supported one,
 * and English when it has no translation for a text.
 */
void i18n
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    resources: { en: { translation: en } },
    supportedLngs: ['en'],
    fallbackLng: 'en',
    load: 'languageOnly',
    detection: { order: ['localStorage', 'navigator'] },
    interpolation: { escapeValue: false },
    initAsync: false,
  })

export { i18n }

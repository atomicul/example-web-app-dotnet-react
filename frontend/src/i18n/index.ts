import i18n from 'i18next'
import LanguageDetector from 'i18next-browser-languagedetector'
import { initReactI18next } from 'react-i18next'
import { z } from 'zod'
import { en as zodEn, ro as zodRo } from 'zod/locales'
import en from './en.json'
import ro from './ro.json'

/** The languages the SPA speaks, by code, each named in its own language. */
export const languages = { en: 'English', ro: 'Română' }

type Language = keyof typeof languages

const zodLocales: Record<Language, () => z.core.$ZodConfig> = {
  en: zodEn,
  ro: zodRo,
}

/** Words zod's own messages, for the checks the SPA leaves to it, in the SPA's language. */
i18n.on('languageChanged', () => {
  z.config(zodLocales[i18n.resolvedLanguage as Language]())
})

/**
 * The SPA's translations. It speaks the language the user last picked, or else the browser's first supported one,
 * and English when it has no translation for a text.
 */
void i18n
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    resources: { en: { translation: en }, ro: { translation: ro } },
    supportedLngs: Object.keys(languages),
    fallbackLng: 'en',
    load: 'languageOnly',
    detection: { order: ['localStorage', 'navigator'] },
    interpolation: { escapeValue: false },
    initAsync: false,
  })

export { i18n }

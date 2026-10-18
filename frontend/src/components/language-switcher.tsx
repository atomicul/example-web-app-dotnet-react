import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/button'
import { languages } from '@/i18n'

/** Switches between the languages the SPA speaks; the choice is remembered in this browser. */
export function LanguageSwitcher() {
  const { i18n } = useTranslation()

  return (
    <div className="flex gap-1">
      {Object.entries(languages).map(([code, name]) => (
        <Button
          key={code}
          variant={i18n.resolvedLanguage === code ? 'secondary' : 'ghost'}
          size="sm"
          lang={code}
          aria-label={name}
          aria-pressed={i18n.resolvedLanguage === code}
          onClick={() => void i18n.changeLanguage(code)}
        >
          {code.toUpperCase()}
        </Button>
      ))}
    </div>
  )
}

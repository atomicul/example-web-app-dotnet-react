import {
  CircleUserRoundIcon,
  LanguagesIcon,
  LogOutIcon,
  SunMoonIcon,
} from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { useAuth } from '@/auth/use-auth'
import {
  Menu,
  MenuContent,
  MenuGroup,
  MenuItem,
  MenuLabel,
  MenuRadioGroup,
  MenuRadioItem,
  MenuSeparator,
  MenuSub,
  MenuSubContent,
  MenuSubTrigger,
  MenuTrigger,
} from '@/components/menu'
import { Button } from '@/components/ui/button'
import { languages } from '@/i18n'

const themes = ['light', 'dark', 'system'] as const

/** Shows who is signed in, and lets them pick the language and theme or log out. */
export function AccountMenu({ email }: { email: string }) {
  const { t } = useTranslation()
  const { logout } = useAuth()

  return (
    <Menu>
      <MenuTrigger
        render={
          <Button
            variant="ghost"
            size="icon"
            aria-label={t('accountMenu.open')}
          />
        }
      >
        <CircleUserRoundIcon />
      </MenuTrigger>
      <MenuContent title={t('accountMenu.title')} align="end">
        <MenuGroup>
          <MenuLabel>
            {t('accountMenu.signedInAs')}
            <span className="text-foreground block truncate">{email}</span>
          </MenuLabel>
        </MenuGroup>
        <MenuSeparator />
        <LanguageSubmenu />
        <ThemeSubmenu />
        <MenuSeparator />
        <MenuItem onClick={() => void logout()}>
          <LogOutIcon />
          {t('accountMenu.logOut')}
        </MenuItem>
      </MenuContent>
    </Menu>
  )
}

/** Picks one of the languages the SPA speaks; the choice is remembered in this browser. */
function LanguageSubmenu() {
  const { t, i18n } = useTranslation()

  return (
    <MenuSub>
      <MenuSubTrigger>
        <LanguagesIcon />
        {t('accountMenu.language')}
      </MenuSubTrigger>
      <MenuSubContent title={t('accountMenu.language')}>
        <MenuRadioGroup
          value={i18n.resolvedLanguage ?? 'en'}
          onValueChange={(code) => void i18n.changeLanguage(code)}
        >
          {Object.entries(languages).map(([code, name]) => (
            <MenuRadioItem key={code} value={code}>
              <span lang={code}>{name}</span>
            </MenuRadioItem>
          ))}
        </MenuRadioGroup>
      </MenuSubContent>
    </MenuSub>
  )
}

/** Lists the themes, which can't be picked until the SPA supports them. */
function ThemeSubmenu() {
  const { t } = useTranslation()

  return (
    <MenuSub>
      <MenuSubTrigger>
        <SunMoonIcon />
        {t('accountMenu.theme')}
      </MenuSubTrigger>
      <MenuSubContent title={t('accountMenu.theme')}>
        <MenuRadioGroup value="system">
          {themes.map((theme) => (
            <MenuRadioItem key={theme} value={theme} disabled>
              {t(`accountMenu.themes.${theme}`)}
            </MenuRadioItem>
          ))}
        </MenuRadioGroup>
      </MenuSubContent>
    </MenuSub>
  )
}

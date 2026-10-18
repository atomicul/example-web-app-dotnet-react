import { useNavigate } from '@tanstack/react-router'
import {
  CircleUserRoundIcon,
  KeyRoundIcon,
  LanguagesIcon,
  LogOutIcon,
  SunMoonIcon,
} from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { useGetApiAuthManageInfo } from '@/api/generated'
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
import { type Theme, themes, useTheme } from '@/theme'

/** Shows who is signed in, and lets them change their email and password, pick the language and theme or log out. */
export function AccountMenu() {
  const { t } = useTranslation()
  const { data: user } = useGetApiAuthManageInfo()
  const navigate = useNavigate()
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
            <span className="text-foreground block truncate">
              {user?.email}
            </span>
          </MenuLabel>
        </MenuGroup>
        <MenuSeparator />
        <MenuItem onClick={() => void navigate({ to: '/account' })}>
          <KeyRoundIcon />
          {t('accountMenu.emailAndPassword')}
        </MenuItem>
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

/** Picks a light or dark theme, or the device's; the choice is remembered in this browser. */
function ThemeSubmenu() {
  const { t } = useTranslation()
  const { theme, setTheme } = useTheme()

  return (
    <MenuSub>
      <MenuSubTrigger>
        <SunMoonIcon />
        {t('accountMenu.theme')}
      </MenuSubTrigger>
      <MenuSubContent title={t('accountMenu.theme')}>
        <MenuRadioGroup
          value={theme}
          onValueChange={(value) => setTheme(value as Theme)}
        >
          {themes.map((choice) => (
            <MenuRadioItem key={choice} value={choice}>
              {t(`accountMenu.themes.${choice}`)}
            </MenuRadioItem>
          ))}
        </MenuRadioGroup>
      </MenuSubContent>
    </MenuSub>
  )
}

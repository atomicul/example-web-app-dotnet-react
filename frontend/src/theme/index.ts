import { useSyncExternalStore } from 'react'

/** The themes the SPA can be shown in; `system` follows the device's light or dark setting. */
export const themes = ['light', 'dark', 'system'] as const

export type Theme = (typeof themes)[number]

const storageKey = 'theme'
const prefersDark = window.matchMedia('(prefers-color-scheme: dark)')
const listeners = new Set<() => void>()

function storedTheme(): Theme {
  const stored = localStorage.getItem(storageKey)
  return themes.find((theme) => theme === stored) ?? 'system'
}

/** Puts the `dark` class on the page when the theme, or the device for `system`, asks for it. */
function applyTheme() {
  const theme = storedTheme()
  const isDark = theme === 'dark' || (theme === 'system' && prefersDark.matches)
  document.documentElement.classList.toggle('dark', isDark)
}

function subscribe(onChange: () => void) {
  listeners.add(onChange)
  return () => listeners.delete(onChange)
}

function setTheme(theme: Theme) {
  localStorage.setItem(storageKey, theme)
  applyTheme()
  listeners.forEach((listener) => listener())
}

/** The theme the user last picked, remembered in this browser, and a way to pick another. */
export function useTheme() {
  return { theme: useSyncExternalStore(subscribe, storedTheme), setTheme }
}

prefersDark.addEventListener('change', applyTheme)
applyTheme()

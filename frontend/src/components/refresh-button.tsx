import { useTranslation } from 'react-i18next'
import { RefreshIcon } from '@/components/refresh-icon'
import { Button } from '@/components/ui/button'
import { useRefresh } from '@/lib/use-refresh'

/** Refetches what the page shows. Hidden on touch screens, which pull the page down instead. */
export function RefreshButton() {
  const { t } = useTranslation()
  const { status, refresh } = useRefresh()

  return (
    <Button
      variant="ghost"
      size="icon"
      aria-label={t('layout.refresh')}
      disabled={status === 'refreshing'}
      onClick={() => void refresh()}
      className="pointer-coarse:hidden"
    >
      <RefreshIcon status={status} />
    </Button>
  )
}

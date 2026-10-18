import { RefreshCwIcon } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/button'
import { useRefresh } from '@/lib/use-refresh'
import { cn } from '@/lib/utils'

/** Refetches what the page shows. Hidden on touch screens, which pull the page down instead. */
export function RefreshButton() {
  const { t } = useTranslation()
  const { refreshing, refresh } = useRefresh()

  return (
    <Button
      variant="ghost"
      size="icon"
      aria-label={t('layout.refresh')}
      disabled={refreshing}
      onClick={() => void refresh()}
      className="pointer-coarse:hidden"
    >
      <RefreshCwIcon className={cn(refreshing && 'animate-spin')} />
    </Button>
  )
}

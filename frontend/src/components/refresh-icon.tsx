import { CheckIcon, RefreshCwIcon } from 'lucide-react'
import type { CSSProperties } from 'react'
import type { RefreshStatus } from '@/lib/use-refresh'
import { cn } from '@/lib/utils'

type RefreshIconProps = {
  status: RefreshStatus
  className?: string
  style?: CSSProperties
}

/** The refresh arrow, spinning while the refresh is on its way, and a check mark popping up in its place once it is done. */
export function RefreshIcon({ status, className, style }: RefreshIconProps) {
  if (status === 'refreshed') {
    return (
      <CheckIcon
        className={cn('animate-in fade-in zoom-in-50 duration-300', className)}
      />
    )
  }
  return (
    <RefreshCwIcon
      className={cn(status === 'refreshing' && 'animate-spin', className)}
      style={style}
    />
  )
}

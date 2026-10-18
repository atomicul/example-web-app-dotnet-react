import { RefreshCwIcon } from 'lucide-react'
import { type ReactNode, useEffect, useEffectEvent, useState } from 'react'
import { useRefresh } from '@/lib/use-refresh'
import { cn } from '@/lib/utils'

const resistance = 0.5
const threshold = 64
const maxPull = 96
const indicatorSize = 36

type PullToRefreshProps = {
  className?: string
  children: ReactNode
}

/**
 * Tracks how far a touch drags `element` down from the top of its scroll, `onRelease` getting the distance once it
 * lets go. Dragging up, or starting below the top, scrolls as usual.
 */
function usePull(
  element: HTMLElement | null,
  enabled: boolean,
  onRelease: (distance: number) => void,
) {
  const [distance, setDistance] = useState(0)
  const canStart = useEffectEvent(() => enabled)
  const release = useEffectEvent(onRelease)

  useEffect(() => {
    if (!element) {
      return
    }
    const scroller = element
    let startY: number | null = null
    let pulled = 0

    function stop() {
      startY = null
      pulled = 0
      setDistance(0)
    }

    function start(event: TouchEvent) {
      const atTop = scroller.scrollTop <= 0 && event.touches.length === 1
      startY = atTop && canStart() ? event.touches[0].clientY : null
    }

    function move(event: TouchEvent) {
      if (startY === null) {
        return
      }
      const dragged = event.touches[0].clientY - startY
      if (dragged < 0) {
        stop()
        return
      }
      event.preventDefault()
      pulled = Math.min(dragged * resistance, maxPull)
      setDistance(pulled)
    }

    function end() {
      if (startY !== null) {
        release(pulled)
      }
      stop()
    }

    scroller.addEventListener('touchstart', start, { passive: true })
    scroller.addEventListener('touchmove', move, { passive: false })
    scroller.addEventListener('touchend', end)
    scroller.addEventListener('touchcancel', stop)
    return () => {
      scroller.removeEventListener('touchstart', start)
      scroller.removeEventListener('touchmove', move)
      scroller.removeEventListener('touchend', end)
      scroller.removeEventListener('touchcancel', stop)
    }
  }, [element])

  return distance
}

/**
 * Scrolls its children, and refetches what they show when a touch pulls them down from the top past a threshold,
 * as phone apps do. An arrow follows the pull, and spins until the refetch is done.
 */
export function PullToRefresh({ className, children }: PullToRefreshProps) {
  const [element, setElement] = useState<HTMLDivElement | null>(null)
  const { refreshing, refresh } = useRefresh()
  const distance = usePull(element, !refreshing, (pulled) => {
    if (pulled >= threshold) {
      void refresh()
    }
  })
  const offset = refreshing ? threshold : distance

  return (
    <div ref={setElement} className={cn('relative overflow-y-auto', className)}>
      <div
        aria-hidden
        className={cn(
          'pointer-events-none absolute inset-x-0 top-0 z-10 flex justify-center',
          distance === 0 && 'transition-transform',
        )}
        style={{ transform: `translateY(${offset - indicatorSize}px)` }}
      >
        <div
          className="bg-background flex items-center justify-center rounded-full border shadow-sm"
          style={{ width: indicatorSize, height: indicatorSize }}
        >
          <RefreshCwIcon
            className={cn('size-4', refreshing && 'animate-spin')}
            style={{
              rotate: `${(distance / threshold) * 270}deg`,
              opacity: refreshing ? 1 : Math.min(distance / threshold, 1),
            }}
          />
        </div>
      </div>
      {children}
    </div>
  )
}

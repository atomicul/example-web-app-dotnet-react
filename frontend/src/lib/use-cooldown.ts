import { useEffect, useState } from 'react'

/** Counts whole seconds down to zero; `start` begins a new count. */
export function useCooldown() {
  const [secondsLeft, setSecondsLeft] = useState(0)

  useEffect(() => {
    if (secondsLeft <= 0) {
      return
    }
    const tick = setTimeout(
      () => setSecondsLeft((seconds) => seconds - 1),
      1000,
    )
    return () => clearTimeout(tick)
  }, [secondsLeft])

  return { secondsLeft, isActive: secondsLeft > 0, start: setSecondsLeft }
}

import { useEffect, useState } from 'react'

/** Counts whole seconds down to zero, from `initialSeconds` at first; `start` begins a new count. */
export function useCooldown(initialSeconds = 0) {
  const [secondsLeft, setSecondsLeft] = useState(initialSeconds)

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

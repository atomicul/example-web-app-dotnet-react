import { useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'

/** How long Tailwind's `animate-spin` takes to turn once. */
const spinTurn = 1000

/** Waits for a spin that started at `start` to finish its turn, so the arrow stops upright rather than mid-turn. */
function finishTurn(start: number) {
  const elapsed = performance.now() - start
  return new Promise((resolve) =>
    setTimeout(resolve, spinTurn - (elapsed % spinTurn)),
  )
}

/**
 * Refetches the queries of the components on screen; `refreshing` holds until they are done, for at least one turn
 * of the spinning arrow, so a fast refetch still shows it happened. Queries of pages left behind are not refetched,
 * since they refetch anyway once their page shows again.
 */
export function useRefresh() {
  const queryClient = useQueryClient()
  const [refreshing, setRefreshing] = useState(false)

  async function refresh() {
    setRefreshing(true)
    const start = performance.now()
    try {
      await queryClient.refetchQueries({ type: 'active' })
    } finally {
      await finishTurn(start)
      setRefreshing(false)
    }
  }

  return { refreshing, refresh }
}

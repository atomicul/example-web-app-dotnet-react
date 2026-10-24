import { type QueryClient, useQueryClient } from '@tanstack/react-query'
import { useRef, useState } from 'react'

export type RefreshStatus = 'idle' | 'refreshing' | 'refreshed'

/** How long the check mark shows once a refresh is done. */
const refreshedFor = 1000

function wait(ms: number) {
  return new Promise((resolve) => setTimeout(resolve, ms))
}

/** Refetches the queries of the components on screen, telling whether every one of them succeeded. */
function refetchActive(queryClient: QueryClient) {
  return queryClient
    .refetchQueries({ type: 'active' }, { throwOnError: true })
    .then(
      () => true,
      () => false,
    )
}

/**
 * Refetches the queries of the components on screen. `status` is `refreshing` until they are done, then `refreshed`
 * for a moment when they all succeeded, so a fast refetch still shows it happened. A refresh started meanwhile takes
 * over the status. Queries of pages left behind are not refetched, since they refetch anyway once their page shows
 * again.
 */
export function useRefresh() {
  const queryClient = useQueryClient()
  const [status, setStatus] = useState<RefreshStatus>('idle')
  const latest = useRef(0)

  async function refresh() {
    const run = ++latest.current
    setStatus('refreshing')
    if (await refetchActive(queryClient)) {
      setStatus('refreshed')
      await wait(refreshedFor)
    }
    if (run === latest.current) {
      setStatus('idle')
    }
  }

  return { status, refresh }
}

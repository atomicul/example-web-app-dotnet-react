import {
  MutationCache,
  QueryCache,
  QueryClient,
  hashKey,
} from '@tanstack/react-query'
import { createRouter } from '@tanstack/react-router'
import { toast } from 'sonner'
import {
  ApiError,
  errorMessage,
  isTooManyRequests,
  isUnauthorized,
} from '@/api/fetcher'
import { sessionQueryKey } from '@/auth/session'
import { routeTree } from './routeTree.gen'

/** Every rate-limited request shares one toast, so a burst of them shows only once. */
function notifyTooManyRequests(error: unknown) {
  if (isTooManyRequests(error)) {
    toast.warning(errorMessage(error), { id: 'too-many-requests' })
  }
}

export const queryClient = new QueryClient({
  queryCache: new QueryCache({
    onError: (error, query) => {
      notifyTooManyRequests(error)
      if (
        isUnauthorized(error) &&
        query.queryHash !== hashKey(sessionQueryKey)
      ) {
        queryClient.removeQueries({ queryKey: sessionQueryKey })
        void router.invalidate()
      }
    },
  }),
  mutationCache: new MutationCache({ onError: notifyTooManyRequests }),
  defaultOptions: {
    queries: {
      retry: (failureCount, error) =>
        !(error instanceof ApiError && error.status < 500) && failureCount < 3,
    },
  },
})

export const router = createRouter({
  routeTree,
  context: { queryClient },
  defaultPreload: 'intent',
  scrollRestoration: true,
})

declare module '@tanstack/react-router' {
  interface Register {
    router: typeof router
  }
}

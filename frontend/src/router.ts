import { QueryCache, QueryClient, hashKey } from '@tanstack/react-query'
import { createRouter } from '@tanstack/react-router'
import { ApiError, isUnauthorized } from '@/api/fetcher'
import { sessionQueryKey } from '@/auth/session'
import { routeTree } from './routeTree.gen'

export const queryClient = new QueryClient({
  queryCache: new QueryCache({
    onError: (error, query) => {
      if (
        isUnauthorized(error) &&
        query.queryHash !== hashKey(sessionQueryKey)
      ) {
        queryClient.removeQueries({ queryKey: sessionQueryKey })
        void router.invalidate()
      }
    },
  }),
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

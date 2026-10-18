import {
  MutationCache,
  QueryCache,
  QueryClient,
  type QueryKey,
  hashKey,
  matchQuery,
} from '@tanstack/react-query'
import { createBrowserHistory, createRouter } from '@tanstack/react-router'
import { toast } from 'sonner'
import {
  ApiError,
  errorMessage,
  isTooManyRequests,
  isUnauthorized,
} from '@/api/fetcher'
import { sessionQueryKey } from '@/auth/session'
import { withTreeNavigation } from '@/lib/tree-history'
import { routeTree } from './routeTree.gen'

/** Every rate-limited request shares one toast, so a burst of them shows only once. */
function notifyTooManyRequests(error: ApiError) {
  toast.warning(errorMessage(error), { id: 'too-many-requests' })
}

function notifyMutationError(error: unknown) {
  if (isTooManyRequests(error)) {
    notifyTooManyRequests(error)
  } else {
    toast.error(errorMessage(error))
  }
}

/** A rate-limited query is retried once, other client errors never, and server or network errors up to three times. */
function shouldRetryQuery(failureCount: number, error: Error) {
  if (isTooManyRequests(error)) {
    return failureCount < 1
  }
  return !(error instanceof ApiError && error.status < 500) && failureCount < 3
}

/** Waits as long as a 429 asks, otherwise doubles from one second up to thirty, as React Query does by default. */
function queryRetryDelay(failureCount: number, error: Error) {
  if (isTooManyRequests(error) && error.retryAfter) {
    return error.retryAfter * 1000
  }
  return Math.min(1000 * 2 ** failureCount, 30_000)
}

export const queryClient: QueryClient = new QueryClient({
  queryCache: new QueryCache({
    onError: (error, query) => {
      if (isTooManyRequests(error)) {
        notifyTooManyRequests(error)
      }
      if (
        isUnauthorized(error) &&
        query.queryHash !== hashKey(sessionQueryKey)
      ) {
        queryClient.removeQueries({ queryKey: sessionQueryKey })
        void router.invalidate()
      }
    },
  }),
  mutationCache: new MutationCache({
    onError: (error, _variables, _onMutateResult, mutation) => {
      if (!mutation.meta?.handlesErrors) {
        notifyMutationError(error)
      }
    },
    onSettled: (_data, _error, _variables, _onMutateResult, mutation) =>
      queryClient.invalidateQueries({
        predicate: (query) =>
          mutation.meta?.invalidates?.some((queryKey) =>
            matchQuery({ queryKey }, query),
          ) ?? true,
      }),
  }),
  defaultOptions: {
    queries: {
      retry: shouldRetryQuery,
      retryDelay: queryRetryDelay,
    },
  },
})

/** Toasts a rate-limited query as soon as it fails, not only once its retry gives up. */
queryClient.getQueryCache().subscribe((event) => {
  if (
    event.type === 'updated' &&
    event.action.type === 'failed' &&
    isTooManyRequests(event.action.error)
  ) {
    notifyTooManyRequests(event.action.error)
  }
})

export const router = createRouter({
  routeTree,
  history: withTreeNavigation(createBrowserHistory()),
  context: { queryClient },
  defaultPreload: 'intent',
  scrollRestoration: true,
})

declare module '@tanstack/react-query' {
  interface Register {
    mutationMeta: {
      /** The caller shows the error itself, as a form does under its fields, so it is not toasted. */
      handlesErrors?: boolean
      /** Key prefixes of the queries the mutation changes, refetched once it settles; every query when left out. */
      invalidates?: QueryKey[]
    }
  }
}

declare module '@tanstack/react-router' {
  interface Register {
    router: typeof router
  }
}

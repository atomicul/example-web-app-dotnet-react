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
    onSettled: () => queryClient.invalidateQueries(),
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

declare module '@tanstack/react-query' {
  interface Register {
    mutationMeta: {
      /** The caller shows the error itself, as a form does under its fields, so it is not toasted. */
      handlesErrors?: boolean
    }
  }
}

declare module '@tanstack/react-router' {
  interface Register {
    router: typeof router
  }
}

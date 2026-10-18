import type { QueryClient } from '@tanstack/react-query'
import { ApiError, isUnauthorized } from '@/api/fetcher'
import {
  getGetApiAuthManageInfoQueryKey,
  getGetApiAuthManageInfoQueryOptions,
} from '@/api/generated'

export const sessionQueryKey = getGetApiAuthManageInfoQueryKey()

/** Identity answers 404 when the cookie outlives its user, as after the API restarts with an empty database. */
function isStaleSession(error: unknown) {
  return error instanceof ApiError && error.status === 404
}

/** Resolves the signed-in user, or null when the session cookie is missing, expired or stale. */
export async function fetchSession(queryClient: QueryClient) {
  try {
    return await queryClient.query({
      ...getGetApiAuthManageInfoQueryOptions(),
      staleTime: 'static',
    })
  } catch (error) {
    if (isUnauthorized(error) || isStaleSession(error)) {
      return null
    }
    throw error
  }
}
